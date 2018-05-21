using System;
using System.Collections.Generic;
using System.Linq;
using Main.Core.Entities.SubEntities;
using NHibernate.Linq;
using WB.Core.BoundedContexts.Headquarters.AssignmentImport.Parser;
using WB.Core.BoundedContexts.Headquarters.AssignmentImport.Verifier;
using WB.Core.BoundedContexts.Headquarters.Assignments;
using WB.Core.BoundedContexts.Headquarters.Services;
using WB.Core.BoundedContexts.Headquarters.Services.Preloading;
using WB.Core.BoundedContexts.Headquarters.UserPreloading.Dto;
using WB.Core.BoundedContexts.Headquarters.UserPreloading.Services;
using WB.Core.BoundedContexts.Headquarters.ValueObjects.PreloadedData;
using WB.Core.BoundedContexts.Headquarters.Views.User;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Events.Interview.Dtos;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.InterviewEntities.Answers;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Infrastructure.Native.Storage.Postgre.Implementation;

namespace WB.Core.BoundedContexts.Headquarters.AssignmentImport
{
    public class  AssignmentsImportService : IAssignmentsImportService
    {
        private readonly IUserViewFactory userViewFactory;
        private readonly IPreloadedDataVerifier verifier;
        private readonly IAuthorizedUser authorizedUser;
        private readonly IPlainSessionProvider sessionProvider;
        private readonly IPlainStorageAccessor<AssignmentsImportProcess> importAssignmentsProcessRepository;
        private readonly IPlainStorageAccessor<AssignmentToImport> importAssignmentsRepository;
        private readonly IInterviewCreatorFromAssignment interviewCreatorFromAssignment;
        private readonly IPlainStorageAccessor<Assignment> assignmentsStorage;
        private readonly IAssignmentsImportFileConverter assignmentsImportFileConverter;

        public AssignmentsImportService(IUserViewFactory userViewFactory,
            IPreloadedDataVerifier verifier,
            IAuthorizedUser authorizedUser,
            IPlainSessionProvider sessionProvider,
            IPlainStorageAccessor<AssignmentsImportProcess> importAssignmentsProcessRepository,
            IPlainStorageAccessor<AssignmentToImport> importAssignmentsRepository,
            IInterviewCreatorFromAssignment interviewCreatorFromAssignment,
            IPlainStorageAccessor<Assignment> assignmentsStorage,
            IAssignmentsImportFileConverter assignmentsImportFileConverter)
        {
            this.userViewFactory = userViewFactory;
            this.verifier = verifier;
            this.authorizedUser = authorizedUser;
            this.sessionProvider = sessionProvider;
            this.importAssignmentsProcessRepository = importAssignmentsProcessRepository;
            this.importAssignmentsRepository = importAssignmentsRepository;
            this.interviewCreatorFromAssignment = interviewCreatorFromAssignment;
            this.assignmentsStorage = assignmentsStorage;
            this.assignmentsImportFileConverter = assignmentsImportFileConverter;
        }

        public IEnumerable<PanelImportVerificationError> VerifySimple(PreloadedFile file, IQuestionnaire questionnaire)
        {
            bool hasErrors = false;

            var assignmentRows = new List<PreloadingAssignmentRow>();

            foreach (var assignmentRow in this.assignmentsImportFileConverter.GetAssignmentRows(file, questionnaire))
            {
                foreach (var answerError in this.verifier.VerifyAnswers(assignmentRow, questionnaire))
                {
                    hasErrors = true;
                    yield return answerError;
                }

                assignmentRows.Add(assignmentRow);
            }

            if (hasErrors) yield break;

            var questionnaireIdentity = new QuestionnaireIdentity(questionnaire.QuestionnaireId, questionnaire.Version);

            var assignmentToImports = ConcatRosters(assignmentRows, questionnaire);

            this.Save(file.FileInfo.FileName, questionnaireIdentity, assignmentToImports);
        }

        public IEnumerable<PanelImportVerificationError> VerifyPanel(string originalFileName,
            PreloadedFile[] allImportedFiles,
            IQuestionnaire questionnaire)
        {
            bool hasErrors = false;

            var assignmentRows = new List<PreloadingAssignmentRow>();

            foreach (var importedFile in allImportedFiles)
            {
                foreach (var assignmentRow in this.assignmentsImportFileConverter.GetAssignmentRows(importedFile, questionnaire))
                {
                    foreach (var answerError in this.verifier.VerifyAnswers(assignmentRow, questionnaire))
                    {
                        hasErrors = true;
                        yield return answerError;
                    }

                    assignmentRows.Add(assignmentRow);
                }
            }

            if (hasErrors) yield break;

            foreach (var rosterError in this.verifier.VerifyRosters(assignmentRows, questionnaire))
            {
                hasErrors = true;
                yield return rosterError;
            }

            if (hasErrors) yield break;

            var questionnaireIdentity = new QuestionnaireIdentity(questionnaire.QuestionnaireId, questionnaire.Version);

            var answersByAssignments = this.ConcatRosters(assignmentRows, questionnaire);

            var assignmentsToImport = FixRosterSizeAnswers(answersByAssignments, questionnaire).ToList();

            this.Save(originalFileName, questionnaireIdentity, assignmentsToImport);
        }

        public AssignmentToImport GetAssignmentById(int assignmentId)
            => this.importAssignmentsRepository.Query(x => x.FirstOrDefault(_ => _.Id == assignmentId));

        public int[] GetAllAssignmentIdsToVerify()
            => this.importAssignmentsRepository.Query(x => x.Where(_ => !_.Verified).Select(_ => _.Id).ToArray());

        public int[] GetAllAssignmentIdsToImport()
            => this.importAssignmentsRepository.Query(x => x.Where(_ => _.Verified && _.Error == null).Select(_ => _.Id).ToArray());

        public AssignmentsImportStatus GetImportStatus()
        {
            var process = this.importAssignmentsProcessRepository.Query(x => x.FirstOrDefault());
            if (process == null) return null;

            var status = new AssignmentsImportStatus
            {
                IsOwnerOfRunningProcess = process.Responsible == this.authorizedUser.UserName,
                TotalCount = process.TotalCount,
                FileName = process.FileName,
                StartedDate = process.StartedDate,
                ResponsibleName = process.Responsible,
                QuestionnaireIdentity = QuestionnaireIdentity.Parse(process.QuestionnaireId),
                ProcessStatus = process.Status
            };

            if (!this.importAssignmentsRepository.Query(x => x.Any())) return status;

            var statistics = this.importAssignmentsRepository.Query(x =>
                x.Select(_ =>
                        new
                        {
                            Total = 1,
                            Verified = _.Verified ? 1 : 0,
                            VerifiedWithoutError = _.Verified && _.Error == null ? 1 : 0,
                            HasError = _.Error != null ? 1 : 0,
                            AssignedToInterviewer = _.Interviewer != null ? 1 : 0,
                            AssignedToSupervisor = _.Interviewer == null && _.Supervisor != null ? 1 : 0
                        })
                    .GroupBy(_ => 1)
                    .Select(_ => new
                    {
                        Total = _.Sum(y => y.Total),
                        WithErrors = _.Sum(y => y.HasError),
                        Verified = _.Sum(y => y.Verified),
                        AssignedToInterviewers = _.Sum(y => y.AssignedToInterviewer),
                        AssignedToSupervisors = _.Sum(y => y.AssignedToSupervisor),
                    })
                    .FirstOrDefault());

            status.WithErrorsCount = statistics.WithErrors;
            status.VerifiedCount = statistics.Verified;
            status.AssignedToInterviewersCount = statistics.AssignedToInterviewers;
            status.AssignedToSupervisorsCount = statistics.AssignedToSupervisors;
            status.InQueueCount = statistics.Total;
            status.ProcessedCount = process.TotalCount - statistics.Total;

            return status;

        }

        public void RemoveAllAssignmentsToImport()
        {
            this.sessionProvider.GetSession().Query<AssignmentToImport>().Delete();
            this.sessionProvider.GetSession().Query<AssignmentsImportProcess>().Delete();
        }

        public void SetResponsibleToAllImportedAssignments(Guid responsibleId)
        {
            var responsible = this.userViewFactory.GetUser(new UserViewInputModel(responsibleId));

            this.sessionProvider.GetSession().Query<AssignmentToImport>()
                .UpdateBuilder()
                .Set(c => c.Interviewer, c => responsible.IsInterviewer() ? responsible.PublicKey : (Guid?) null)
                .Set(c => c.Supervisor, c => responsible.IsInterviewer() ? responsible.Supervisor.Id : responsible.PublicKey)
                .Update();
        }

        public IEnumerable<string> GetImportAssignmentsErrors()
            => this.importAssignmentsRepository.Query(x => x.Where(_ => _.Error != null).Select(_ => _.Error));

        public void ImportAssignment(int assignmentId, IQuestionnaire questionnaire)
        {
            var questionnaireIdentity = new QuestionnaireIdentity(questionnaire.QuestionnaireId, questionnaire.Version);
            var assignmentToImport = this.GetAssignmentById(assignmentId);

            var responsibleId = assignmentToImport.Interviewer ?? assignmentToImport.Supervisor.Value;
            var identifyingQuestionIds = questionnaire.GetPrefilledQuestions().ToHashSet();

            var assignment = new Assignment(questionnaireIdentity, responsibleId, assignmentToImport.Quantity);
            var identifyingAnswers = assignmentToImport.Answers
                .Where(x => identifyingQuestionIds.Contains(x.Identity.Id)).Select(a =>
                    IdentifyingAnswer.Create(assignment, questionnaire, a.Answer.ToString(), a.Identity))
                .ToList();

            assignment.SetIdentifyingData(identifyingAnswers);
            assignment.SetAnswers(assignmentToImport.Answers);

            this.assignmentsStorage.Store(assignment, null);

            this.interviewCreatorFromAssignment.CreateInterviewIfQuestionnaireIsOld(responsibleId,
                questionnaireIdentity, assignment.Id, assignmentToImport.Answers);
        }

        public void SetVerifiedToAssignment(int assignmentId, string errorMessage = null)
            => this.sessionProvider.GetSession().Query<AssignmentToImport>()
                .Where(c => c.Id == assignmentId)
                .UpdateBuilder()
                .Set(c => c.Verified, c => true)
                .Set(c => c.Error, c => errorMessage)
                .Update();

        public void RemoveAssignmentToImport(int assignmentId)
            => this.importAssignmentsRepository.Remove(assignmentId);

        public void SetImportProcessStatus(AssignmentsImportProcessStatus status)
            => this.sessionProvider.GetSession().Query<AssignmentsImportProcess>()
                .UpdateBuilder()
                .Set(c => c.Status, c => status)
                .Update();

        private void Save(string fileName, QuestionnaireIdentity questionnaireIdentity, IList<AssignmentToImport> assignments)
        {
            this.RemoveAllAssignmentsToImport();

            this.SaveProcess(fileName, questionnaireIdentity, assignments);
            this.SaveAssignments(assignments);
        }

        private void SaveProcess(string fileName, QuestionnaireIdentity questionnaireIdentity, IList<AssignmentToImport> assignments)
        {
            var process = this.importAssignmentsProcessRepository.Query(x => x.FirstOrDefault()) ??
                          new AssignmentsImportProcess();

            process.FileName = fileName;
            process.TotalCount = assignments.Count;
            process.Responsible = this.authorizedUser.UserName;
            process.StartedDate = DateTime.UtcNow;
            process.QuestionnaireId = questionnaireIdentity.ToString();
            process.Status = AssignmentsImportProcessStatus.Verification;

            this.importAssignmentsProcessRepository.Store(process, process.Id);
        }

        private void SaveAssignments(IList<AssignmentToImport> assignments)
        {
            this.importAssignmentsRepository.Store(assignments.Select(x =>
                new Tuple<AssignmentToImport, object>(x, x.Id)));
        }

        private List<AssignmentToImport> ConcatRosters(List<PreloadingAssignmentRow> assignmentRows,
            IQuestionnaire questionnaire)
            => assignmentRows
                .GroupBy(assignmentRow => assignmentRow.InterviewIdValue?.Value ??
                                          /*for single/anvanced preloading with main file only without interview ids*/
                                          Guid.NewGuid().ToString())
                .Select(x => ToAssignmentToImport(x, questionnaire))
                .ToList();

        private AssignmentToImport ToAssignmentToImport(IGrouping<string, PreloadingAssignmentRow> assignment, IQuestionnaire questionnaire)
        {
            var quantity = assignment.Select(_ => _.Quantity).FirstOrDefault(_ => _ != null)?.Quantity;
            var responsible = assignment.Select(_ => _.Responsible).FirstOrDefault(_ => _ != null)?.Responsible;
            var answers = assignment.SelectMany(_ => _.Answers.OfType<IAssignmentAnswer>().Select(y =>
                ToInterviewAnswer(y, ToRosterVector(_.RosterInstanceCodes), questionnaire)));

            return new AssignmentToImport
            {
                Quantity = quantity.HasValue ? (quantity > -1 ? quantity : null) : 1,
                Answers = answers.Where(y => y?.Answer != null).ToList(),
                Interviewer = responsible?.InterviewerId,
                Supervisor = responsible?.SupervisorId,
                Verified = false,
            };
        }

        private static RosterVector ToRosterVector(AssignmentRosterInstanceCode[] rosterInstanceCodes)
            => new RosterVector(rosterInstanceCodes.Select(x => x.Code.Value).ToArray());

        private static IEnumerable<AssignmentToImport> FixRosterSizeAnswers(IEnumerable<AssignmentToImport> assignments, IQuestionnaire questionnaire)
        {
            var allRosterSizeQuestions = questionnaire.GetAllRosterSizeQuestions();
            if (allRosterSizeQuestions.Count == 0)
                foreach (var assignmentToImport in assignments)
                    yield return assignmentToImport;

            var questionsInsideRosters = allRosterSizeQuestions
                .Select(x => (rosterSize: x, rosters: questionnaire.GetRosterGroupsByRosterSizeQuestion(x)))
                .Select(x => (rosterSize: x.rosterSize,
                    rosterQuestions: x.rosters.SelectMany(questionnaire.GetAllUnderlyingQuestions).ToArray()))
                .ToArray();

            foreach (var assignment in assignments)
            {
                if (assignment.Answers.Any(x => x.Identity.RosterVector.Length > 0))
                    BuildRosterSizeAnswersByRosterQuestionAnswers(assignment, allRosterSizeQuestions, questionsInsideRosters, questionnaire);

                yield return assignment;
            }
        }

        private static void BuildRosterSizeAnswersByRosterQuestionAnswers(AssignmentToImport assignment, IReadOnlyCollection<Guid> rosterSizeQuestions,
            (Guid rosterSize, Guid[] rosterQuestions)[] questionsInsideRosters, IQuestionnaire questionnaire)
        {
            // answers by text list roster size question from roster file
            var listRosterTitles = assignment.Answers
                .Where(x => rosterSizeQuestions.Contains(x.Identity.Id) && x.Answer is TextAnswer)
                .ToArray();

            // roster size answers from parent files
            var sourceRosterSizeAnswers = assignment.Answers
                .Where(x => rosterSizeQuestions.Contains(x.Identity.Id) && !(x.Answer is TextAnswer))
                .ToArray();

            // answers in rosters triggered by concrete rosters size question
            var rosterAnswersByRosterSizeQuestion = questionsInsideRosters.ToDictionary(x => x.rosterSize,
                x => new
                {
                    answers = assignment.Answers.Where(y => x.rosterQuestions.Contains(y.Identity.Id)).ToArray(),
                    rosterSizeType = questionnaire.GetQuestionType(x.rosterSize),
                    rosterSizeLevel = questionnaire.GetRosterLevelForQuestion(x.rosterSize)
                }).Where(x => x.Value.answers.Length > 0);

            var calculatedRosterSizeAnswers = new List<InterviewAnswer>();

            foreach (var rosterAnswers in rosterAnswersByRosterSizeQuestion.OrderBy(x => x.Value.rosterSizeLevel))
            {
                var answersByRosterLevels = rosterAnswers.Value.rosterSizeLevel == 0
                    ? rosterAnswers.Value.answers.GroupBy(x => RosterVector.Empty)
                    : rosterAnswers.Value.answers
                        .GroupBy(x => x.Identity.RosterVector.Take(rosterAnswers.Value.rosterSizeLevel)).ToArray();

                foreach (var answersByRosterLevel in answersByRosterLevels)
                {
                    var rosterSizeLevel = rosterAnswers.Value.rosterSizeLevel;
                    var rosterSizeType = rosterAnswers.Value.rosterSizeType;
                    var rosterSizeQuestionId = rosterAnswers.Key;
                    var rosterSizeQuestionRosterVector = answersByRosterLevel.Key;

                    var answersGroupedByRosterInstanceId = answersByRosterLevel
                        .GroupBy(x => x.Identity.RosterVector.ElementAt(rosterSizeLevel))
                        .OrderBy(x => x.Key);

                    // user defined roster instance ids
                    var rosterSizeAnsweredOptions = answersGroupedByRosterInstanceId.Select(x => x.Key).ToArray();

                    // difference between roster instance ids of user and what we can import to interview tree
                    var oldToNewRosterInstanceIds = rosterSizeAnsweredOptions
                        .Select((x, i) => (newId: i, oldId: x))
                        .ToDictionary(x => x.oldId, x => x.newId);

                    var rosterSizeAnswer = GetRosterSizeAnswerByRosterAnswers(questionnaire, rosterSizeType,
                        rosterSizeQuestionRosterVector, listRosterTitles, rosterSizeQuestionId,
                        rosterSizeAnsweredOptions, oldToNewRosterInstanceIds);
                    
                    if (new []{ QuestionType.TextList, QuestionType.Numeric }.Contains(rosterSizeType))
                        FixRosterVectors(answersGroupedByRosterInstanceId, oldToNewRosterInstanceIds, rosterSizeLevel);

                    calculatedRosterSizeAnswers.Add(rosterSizeAnswer);
                }
            }

            // remove roster size answer from parent file if we have calulated by roster files roster size answer
            foreach (var sourceRosterSizeAnswer in sourceRosterSizeAnswers.Where(x => calculatedRosterSizeAnswers.Any(y => y.Identity == x.Identity)))
                assignment.Answers.Remove(sourceRosterSizeAnswer);

            // remove text list roster size answers from roster files
            foreach (var listRosterTitle in listRosterTitles)
                assignment.Answers.Remove(listRosterTitle);

            assignment.Answers.AddRange(calculatedRosterSizeAnswers);
        }

        private static InterviewAnswer GetRosterSizeAnswerByRosterAnswers(IQuestionnaire questionnaire,
            QuestionType rosterSizeType, RosterVector rosterSizeRosterVector, InterviewAnswer[] listRosterTitles,
            Guid rosterSizeQuestionId, int[] rosterSizeAnsweredOptions, Dictionary<int, int> oldToNewRosterInstanceIds)
        {
            var rosterSizeAnswer = new InterviewAnswer
            {
                Identity = Identity.Create(rosterSizeQuestionId, rosterSizeRosterVector)
            };

            switch (rosterSizeType)
            {
                case QuestionType.MultyOption:
                    rosterSizeAnswer.Answer = ToRosterSizeCategoricalAnswer(questionnaire, rosterSizeAnswer, rosterSizeAnsweredOptions);
                    break;
                case QuestionType.Numeric:
                    rosterSizeAnswer.Answer = NumericIntegerAnswer.FromInt(rosterSizeAnsweredOptions.Length);
                    break;
                case QuestionType.TextList:
                    rosterSizeAnswer.Answer = ToRosterSizeListAnswer(oldToNewRosterInstanceIds, listRosterTitles, rosterSizeAnswer);
                    break;
            }

            return rosterSizeAnswer;
        }

        private static void FixRosterVectors(
            IOrderedEnumerable<IGrouping<int, InterviewAnswer>> answersGroupedByRosterInstanceId,
            Dictionary<int, int> oldToNewRosterInstanceIds, int rosterSizeLevel)
        {
            foreach (var answersByRosterInstanceId in answersGroupedByRosterInstanceId)
            {
                // this means that roster instance id in roster file as we expected in our system
                if (oldToNewRosterInstanceIds[answersByRosterInstanceId.Key] == answersByRosterInstanceId.Key) continue;

                foreach (var interviewAnswer in answersByRosterInstanceId)
                {
                      var newRosterVector = interviewAnswer.Identity.RosterVector.Replace(
                        rosterSizeLevel, oldToNewRosterInstanceIds[answersByRosterInstanceId.Key]);

                    interviewAnswer.Identity = Identity.Create(interviewAnswer.Identity.Id, newRosterVector);
                }
            }
        }

        private static TextListAnswer ToRosterSizeListAnswer(Dictionary<int, int> oldToNewRosterInstanceIds,
            InterviewAnswer[] listRosterTitles, InterviewAnswer rosterSizeAnswer)
        {
            var rosterSizeQuestionId = rosterSizeAnswer.Identity.Id;
            var rosterVector = rosterSizeAnswer.Identity.RosterVector;

            var rosterSizeListItems = oldToNewRosterInstanceIds.Keys
                .Select(rosterInstanceCode => ToRosterSizeListItem(rosterSizeQuestionId, rosterVector,
                    rosterInstanceCode, oldToNewRosterInstanceIds[rosterInstanceCode], listRosterTitles))
                .ToArray();

            return TextListAnswer.FromTupleArray(rosterSizeListItems);
        }

        private static Tuple<int, string> ToRosterSizeListItem(Guid rosterSizeQuestionId, RosterVector rosterVertor,
            int oldRosterInstanceCode, int newRosterInstanceCode, InterviewAnswer[] listRosterTitles)
        {
            var oldRosterInstanceId = Identity.Create(rosterSizeQuestionId,
                rosterVertor.ExtendWithOneCoordinate(oldRosterInstanceCode));

            var rosterInstanceTitle = (TextAnswer) listRosterTitles.FirstOrDefault(x => x.Identity == oldRosterInstanceId)?.Answer;

            return new Tuple<int, string>(newRosterInstanceCode, rosterInstanceTitle?.Value ?? "");
        }

        private static AbstractAnswer ToRosterSizeCategoricalAnswer(IQuestionnaire questionnaire, InterviewAnswer rosterSizeAnswer, int[] rosterSizeAnsweredOptions) 
            => questionnaire.IsQuestionYesNo(rosterSizeAnswer.Identity.Id)
            ? YesNoAnswer.FromAnsweredYesNoOptions(rosterSizeAnsweredOptions.Select(x => new AnsweredYesNoOption(x, true)).ToArray())
            : (AbstractAnswer) CategoricalFixedMultiOptionAnswer.FromIntArray(rosterSizeAnsweredOptions);

        private InterviewAnswer ToInterviewAnswer(IAssignmentAnswer value, RosterVector rosterVector, IQuestionnaire questionnaire)
        {
            var questionId = questionnaire.GetQuestionIdByVariable(value.VariableName);
            if (!questionId.HasValue) return null;

            var questionType = questionnaire.GetQuestionType(questionId.Value);
            
            var answer = new InterviewAnswer { Identity = Identity.Create(questionId.Value, rosterVector) };

            var isLinkedToQuestion = questionnaire.IsQuestionLinked(questionId.Value);
            var isLinkedToRoster = questionnaire.IsQuestionLinkedToRoster(questionId.Value);

            var isRosterSizeQuestion = questionnaire.IsRosterSizeQuestion(questionId.Value);
            // magic for text list question only
            if (isRosterSizeQuestion && value is AssignmentTextAnswer)
            {
                answer.Answer = TextAnswer.FromString(((AssignmentTextAnswer) value)?.Value);
                return answer;
            }
            //------------------------------------------------------------------------------

            switch (questionType)
            {
                case QuestionType.SingleOption:
                    if (!isLinkedToQuestion && !isLinkedToRoster)
                    {
                        var assignmentInt = ((AssignmentCategoricalSingleAnswer)value).OptionCode;
                        if (assignmentInt.HasValue)
                            answer.Answer = CategoricalFixedSingleOptionAnswer.FromInt(assignmentInt.Value);
                    }
                    break;
                case QuestionType.MultyOption:
                    {
                        var assignmentCategoricalMulti = ((AssignmentMultiAnswer)value)?.Values
                            ?.OfType<AssignmentIntegerAnswer>()
                            .Where(x => x.Answer.HasValue)
                            ?.Select(x => new { code = Convert.ToInt32(x.VariableName), answer = Convert.ToInt32(x.Answer) })
                            .ToArray();

                        if (assignmentCategoricalMulti?.Length > 0)
                        {
                            if (questionnaire.IsQuestionYesNo(questionId.Value))
                            {
                                var orderedAnswers = assignmentCategoricalMulti
                                    .Where(x => x.answer > -1)
                                    .OrderBy(x => x.answer)
                                    .Select(x => new AnsweredYesNoOption(x.code, x.answer != 0))
                                    .ToArray();

                                answer.Answer = YesNoAnswer.FromAnsweredYesNoOptions(orderedAnswers);
                            }
                            else if (!isLinkedToQuestion && !isLinkedToRoster)
                            {
                                var orderedAnswers = assignmentCategoricalMulti
                                    .Where(x => x.answer > 0)
                                    .OrderBy(x => x.answer)
                                    .Select(x => x.code)
                                    .Distinct()
                                    .ToArray();

                                answer.Answer = CategoricalFixedMultiOptionAnswer.FromIntArray(orderedAnswers);
                            }
                        }
                    }
                    break;
                case QuestionType.DateTime:
                    var assignmentDateTime = ((AssignmentDateTimeAnswer)value).Answer;
                    if (assignmentDateTime.HasValue)
                        answer.Answer = DateTimeAnswer.FromDateTime(assignmentDateTime.Value);
                    break;
                case QuestionType.GpsCoordinates:
                    var assignmentGpsValues = ((AssignmentGpsAnswer)value)?.Values;
                    if (assignmentGpsValues != null)
                    {
                        var doubleAnswers = assignmentGpsValues.OfType<AssignmentDoubleAnswer>();
                        var longitude = doubleAnswers.FirstOrDefault(x => x.VariableName == nameof(GeoPosition.Longitude).ToLower())?.Answer;
                        var latitude = doubleAnswers.FirstOrDefault(x => x.VariableName == nameof(GeoPosition.Latitude).ToLower())?.Answer;
                        var altitude = doubleAnswers.FirstOrDefault(x => x.VariableName == nameof(GeoPosition.Altitude).ToLower())?.Answer;
                        var accuracy = doubleAnswers.FirstOrDefault(x => x.VariableName == nameof(GeoPosition.Accuracy).ToLower())?.Answer;
                        var timestamp = assignmentGpsValues.OfType<AssignmentDateTimeAnswer>().FirstOrDefault(x => x.VariableName == nameof(GeoPosition.Timestamp).ToLower())?.Answer;

                        answer.Answer = GpsAnswer.FromGeoPosition(new GeoPosition(latitude ?? 0, longitude ?? 0,
                            accuracy ?? 0, altitude ?? 0, timestamp ?? DateTimeOffset.MinValue));
                    }
                    break;
                case QuestionType.Numeric:
                    if (questionnaire.IsQuestionInteger(questionId.Value))
                    {
                        var assignmentInt = ((AssignmentIntegerAnswer)value).Answer;
                        if (assignmentInt.HasValue)
                            answer.Answer = NumericIntegerAnswer.FromInt(assignmentInt.Value);
                    }
                    else
                    {
                        var assignmentDouble = ((AssignmentDoubleAnswer)value).Answer;
                        if (assignmentDouble.HasValue)
                            answer.Answer = NumericRealAnswer.FromDouble(assignmentDouble.Value);
                    }
                    break;
                case QuestionType.QRBarcode:
                    answer.Answer = QRBarcodeAnswer.FromString(((AssignmentTextAnswer)value)?.Value);
                    break;
                case QuestionType.Text:
                    answer.Answer = TextAnswer.FromString(((AssignmentTextAnswer)value)?.Value);
                    break;
                case QuestionType.TextList:
                    {
                        var textListAnswers = ((AssignmentMultiAnswer)value)?.Values
                            ?.OfType<AssignmentTextAnswer>()
                            ?.Where(x => !string.IsNullOrWhiteSpace(x.Value))
                            ?.Select(x => new Tuple<decimal, string>(Convert.ToDecimal(x.VariableName), x.Value))
                            ?.OrderBy(x => x.Item1)
                            ?.ToArray();

                        answer.Answer = TextListAnswer.FromTupleArray(textListAnswers);

                    }
                    break;
                default: return null;
            }

            return answer;
        }
    }
}
