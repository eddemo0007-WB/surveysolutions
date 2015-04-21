﻿using System;
using System.Collections.Generic;
using System.Linq;

using Main.Core.Documents;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using Main.Core.Entities.SubEntities.Question;
using Main.Core.Events.Questionnaire;
using Microsoft.Practices.ServiceLocation;

using Ncqrs.Eventing.Sourcing;

using WB.Core.GenericSubdomains.Utils;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.DataCollection.Utils;

namespace WB.Core.SharedKernels.DataCollection.Implementation.Aggregates
{
    internal class StatefullQuestionnaire : Questionnaire
    {
        private static IPlainQuestionnaireRepository QuestionnaireRepository
        {
            get { return ServiceLocator.Current.GetInstance<IPlainQuestionnaireRepository>(); }
        }

        private static IPlainRepository<QuestionnaireModel> QuestionnaireModelRepository
        {
            get { return ServiceLocator.Current.GetInstance<IPlainRepository<QuestionnaireModel>>(); }
        }

        public StatefullQuestionnaire() { }

        new protected internal void Apply(TemplateImported e)
        {
            var questionnaireDocument = e.Source;
            questionnaireDocument.ConnectChildrenWithParent();

            QuestionnaireRepository.StoreQuestionnaire(questionnaireDocument.PublicKey, 1, questionnaireDocument);

            var questionnaireModel = new QuestionnaireModel();

            var groups = questionnaireDocument.GetAllGroups().ToList();
            var questions = questionnaireDocument.GetAllQuestions().ToList();

            questionnaireModel.Id = questionnaireDocument.PublicKey;
            questionnaireModel.Title = questionnaireDocument.Title;
            questionnaireModel.ListOfRostersId = groups.Where(x => x.IsRoster).Select(x => x.PublicKey).ToList();
            questionnaireModel.PrefilledQuestionsIds = questions.Where(x => x.Featured).Select(x => x.PublicKey).ToList();
            questionnaireModel.Questions = questions.ToDictionary(x => x.PublicKey, CreateQuestionModel);
            questionnaireModel.GroupsWithoutNestedChildren = groups.ToDictionary(x => x.PublicKey, CreateGroupModelWithoutNestedChildren);
            questionnaireModel.GroupParents = groups.ToDictionary(x => x.PublicKey, BuildParentsList);
            questionnaireModel.GroupsHierarchy = questionnaireDocument.Children.Cast<Group>().Select(this.BuildGroupsHierarchy).ToList();

            QuestionnaireModelRepository.Store(questionnaireModel, questionnaireDocument.PublicKey.FormatGuid());
        }

        public GroupsHierarchyModel BuildGroupsHierarchy(Group currentGroup)
        {
            var childrenHierarchy = currentGroup.Children.OfType<Group>()
                .Select(this.BuildGroupsHierarchy)
                .ToList();

            return new GroupsHierarchyModel
                   {
                       Id = currentGroup.PublicKey,
                       Title = currentGroup.Title,
                       IsRoster = currentGroup.IsRoster,
                       Children = childrenHierarchy
                   };
        }

        private List<GroupPlaceholderModel> BuildParentsList(Group group)
        {
            var parents = new List<GroupPlaceholderModel>();

            var parent = group.GetParent() as Group;
            while (parent != null && parent.PublicKey != EventSourceId )
            {
                var parentPlaceholder = parent.IsRoster ? new RosterPlaceholderModel() : new GroupPlaceholderModel();
                parentPlaceholder.Id = parent.PublicKey;
                parentPlaceholder.Title = parent.Title;
                parents.Add(parentPlaceholder);

                parent = parent.GetParent() as Group;
            }
            return parents;
        }

        private static GroupModel CreateGroupModelWithoutNestedChildren(Group group)
        {
            var groupModel = group.IsRoster ? new RosterModel() : new GroupModel();

            groupModel.Id = group.PublicKey;
            groupModel.Title = group.Title;

            foreach (var child in group.Children)
            {
                var question = child as AbstractQuestion;
                if (question != null)
                {
                    if (question.QuestionScope != QuestionScope.Interviewer || question.Featured)
                        continue;

                    var questionModelPlaceholder = new QuestionPlaceholderModel { Id = question.PublicKey, Title = question.QuestionText };
                    groupModel.Placeholders.Add(questionModelPlaceholder);
                    continue;
                }

                var text = child as StaticText;
                if (text != null)
                {
                    var staticTextModel = new StaticTextModel { Id = text.PublicKey, Title = text.Text };
                    groupModel.Placeholders.Add(staticTextModel);
                    continue;
                }

                var subGroup = child as Group;
                if (subGroup != null)
                {
                    if (subGroup.IsRoster)
                    {
                        var rosterModelPlaceholder = new RosterPlaceholderModel { Id = subGroup.PublicKey, Title = subGroup.Title };
                        groupModel.Placeholders.Add(rosterModelPlaceholder);
                    }
                    else
                    {
                        var groupModelPlaceholder = new GroupPlaceholderModel { Id = subGroup.PublicKey, Title = subGroup.Title };
                        groupModel.Placeholders.Add(groupModelPlaceholder);
                    }
                }
            }
            return groupModel;
        }

        private static BaseQuestionModel CreateQuestionModel(IQuestion question)
        {
            BaseQuestionModel questionModel;
            switch (question.QuestionType)
            {
                case QuestionType.SingleOption:
                    var singleQuestion = question as SingleQuestion;
                    if (singleQuestion.LinkedToQuestionId.HasValue)
                    {
                        questionModel = new LinkedSingleOptionQuestionModel
                                        {
                                            LinkedToQuestionId = singleQuestion.LinkedToQuestionId.Value
                                        };
                    }
                    else
                    {
                        questionModel = new SingleOptionQuestionModel
                                        {
                                            CascadeFromQuestionId = singleQuestion.CascadeFromQuestionId,
                                            IsFilteredCombobox = singleQuestion.IsFilteredCombobox,
                                            Options = singleQuestion.Answers.Select(x => new OptionModel { Id = decimal.Parse(x.AnswerValue), Title = x.AnswerText }).ToList()
                                        };
                    }
                    break;
                case QuestionType.MultyOption:
                    var multiQuestion = question as MultyOptionsQuestion;
                    if (multiQuestion.LinkedToQuestionId.HasValue)
                    {
                        questionModel = new LinkedMultiOptionQuestionModel
                                        {
                                            LinkedToQuestionId = multiQuestion.LinkedToQuestionId.Value
                                        };
                    }
                    else
                    {
                        questionModel = new MultiOptionQuestionModel
                                        {
                                            AreAnswersOrdered = multiQuestion.AreAnswersOrdered,
                                            MaxAllowedAnswers = multiQuestion.MaxAllowedAnswers,
                                            Options = question.Answers.Select(x => new OptionModel { Id = decimal.Parse(x.AnswerValue), Title = x.AnswerText }).ToList()
                                        };
                    }
                    break;
                case QuestionType.Numeric:
                    var numericQuestion = question as INumericQuestion;
                    if (numericQuestion.IsInteger)
                    {
                        questionModel = new IntegerNumericQuestionModel { MaxValue = numericQuestion.MaxValue };
                    }
                    else
                    {
                        questionModel = new RealNumericQuestionModel { CountOfDecimalPlaces = numericQuestion.CountOfDecimalPlaces };
                    }
                    break;
                case QuestionType.DateTime:
                    questionModel = new DateTimeQuestionModel();
                    break;
                case QuestionType.GpsCoordinates:
                    questionModel = new GpsCoordinatesQuestionModel();
                    break;
                case QuestionType.Text:
                    questionModel = new MaskedTextQuestionModel { Mask = (question as TextQuestion).Mask };
                    break;
                case QuestionType.TextList:
                    questionModel = new TextListQuestionModel();
                    break;
                case QuestionType.QRBarcode:
                    questionModel = new QrBarcodeQuestionModel();
                    break;
                case QuestionType.Multimedia:
                    questionModel = new MultimediaQuestionModel();
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            questionModel.Id = question.PublicKey;
            questionModel.Title = question.QuestionText;
            questionModel.IsMandatory = question.Mandatory;
            questionModel.IsPrefilled = question.Featured;

            return questionModel;
        }
    }


}
