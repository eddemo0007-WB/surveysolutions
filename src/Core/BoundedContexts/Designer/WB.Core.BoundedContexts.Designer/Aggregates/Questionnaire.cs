﻿using Main.Core.Entities;
using Main.Core.Entities.SubEntities.Question;
using WB.Core.BoundedContexts.Designer.Exceptions;
using WB.Core.BoundedContexts.Designer.Resources;
using WB.Core.BoundedContexts.Designer.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using Ncqrs;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire.Question;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit;
using WB.Core.SharedKernels.SurveySolutions.Documents;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire.Macros;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire.LookupTables;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire.Attachments;
using WB.Core.SharedKernels.QuestionnaireEntities;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire.StaticText;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire.Translations;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire.Variable;
using WB.Core.BoundedContexts.Designer.Translations;
using WB.Core.BoundedContexts.Designer.ValueObjects;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.SharedPersons;
using WB.Core.Infrastructure.Aggregates;
using WB.Core.SharedKernels.Questionnaire.Documents;
using Group = Main.Core.Entities.SubEntities.Group;


namespace WB.Core.BoundedContexts.Designer.Aggregates
{
    internal class Questionnaire : IPlainAggregateRoot
    {
        private const int MaxCountOfDecimalPlaces = 15;
        private const int MaxChapterItemsCount = 400;
        private const int MaxTitleLength = 500;
        private const int maxFilteredComboboxOptionsCount = 15000;
        private const int maxCascadingComboboxOptionsCount = 15000;
        private const int MaxGroupDepth = 10;
        private const int DefaultVariableLengthLimit = 32;
        private const int DefaultRestrictedVariableLengthLimit = 20;

        private static readonly QuestionType[] RestrictedVariableLengthQuestionTypes = 
            new QuestionType[]
            {
                QuestionType.GpsCoordinates,
                QuestionType.MultyOption,
                QuestionType.TextList
            };

        private static readonly HashSet<QuestionType> RosterSizeQuestionTypes = new HashSet<QuestionType>
        {
            QuestionType.Numeric,
            QuestionType.MultyOption,
            QuestionType.TextList,
        };

        #region State

        private QuestionnaireDocument innerDocument = new QuestionnaireDocument();
        private List<SharedPerson> sharedPersons = new List<SharedPerson>();
        private IEnumerable<Guid> ReadOnlyUsersIds => sharedPersons.Where(p => p.ShareType == ShareType.View).Select(p => p.UserId);
        private IEnumerable<Guid> SharedUsersIds => sharedPersons.Select(p => p.UserId);

        public QuestionnaireDocument QuestionnaireDocument => this.innerDocument;
        public IEnumerable<SharedPerson> SharedPersons => this.sharedPersons;

        internal void Initialize(Guid aggregateId, QuestionnaireDocument document, IEnumerable<SharedPerson> sharedPersons)
        {
            this.innerDocument = document ?? new QuestionnaireDocument() { PublicKey = aggregateId };
            this.sharedPersons = sharedPersons?.ToList() ?? new List<SharedPerson>();

            // Migrate single validation conditions to multiple
            foreach (var question in this.innerDocument.Children.TreeToEnumerable(x => x.Children).OfType<IQuestion>())
            {
                question.ValidationConditions = question.ValidationConditions;
                question.ValidationExpression = null;
                question.ValidationMessage = null;
            }
        }
        
        public Guid Id => this.innerDocument.PublicKey;
        
        private void AddGroup(IGroup newGroup, Guid? parentId)
        {
            if (parentId.HasValue)
            {
                var parentGroup = this.innerDocument.Find<Group>(parentId.Value);
                if (parentGroup != null)
                {
                    newGroup.SetParent(parentGroup);
                }
                else
                {
                    string errorMessage = string.Format("Fail attempt to add group {0} into group {1}. But group {1} doesnt exist in document {2}",
                        newGroup.PublicKey,
                        parentId,
                        this.innerDocument.PublicKey);

                    logger.Error(errorMessage);
                }
            }

            this.innerDocument.Add(newGroup, parentId);
        }

        private void RemoveRosterFlagFromGroup(Guid groupId)
        {
            this.innerDocument.UpdateGroup(groupId, group =>
            {
                group.IsRoster = false;
                group.RosterSizeSource = RosterSizeSourceType.Question;
                group.RosterSizeQuestionId = null;
                group.RosterTitleQuestionId = null;
                group.FixedRosterTitles = new FixedRosterTitle[0];
            });
        }
        
        #endregion

        #region Dependencies

        private readonly ILogger logger;
        private readonly IClock clock;
        private readonly IExpressionProcessor expressionProcessor;
        private readonly ISubstitutionService substitutionService;
        private readonly IKeywordsProvider variableNameValidator;
        private readonly ILookupTableService lookupTableService;
        private readonly IAttachmentService attachmentService;
        private readonly ITranslationsService translationService;
        private readonly IQuestionnireHistotyVersionsService questionnireHistotyVersionsService;
        private int affectedByReplaceEntries;

        #endregion

        public Questionnaire(
            ILogger logger, 
            IClock clock, 
            IExpressionProcessor expressionProcessor, 
            ISubstitutionService substitutionService, 
            IKeywordsProvider variableNameValidator, 
            ILookupTableService lookupTableService, 
            IAttachmentService attachmentService,
            ITranslationsService translationService,
            IQuestionnireHistotyVersionsService questionnireHistotyVersionsService)
        {
            this.logger = logger;
            this.clock = clock;
            this.expressionProcessor = expressionProcessor;
            this.substitutionService = substitutionService;
            this.variableNameValidator = variableNameValidator;
            this.lookupTableService = lookupTableService;
            this.attachmentService = attachmentService;
            this.translationService = translationService;
            this.questionnireHistotyVersionsService = questionnireHistotyVersionsService;
        }

        #region Questionnaire command handlers

        public void CreateQuestionnaire(Guid publicKey, string title, Guid? createdBy, bool isPublic)
        {
            this.ThrowDomainExceptionIfQuestionnaireTitleIsEmptyOrWhitespacesOrTooLong(title);

            this.innerDocument = new QuestionnaireDocument
            {
                IsPublic = isPublic,
                Title = System.Web.HttpUtility.HtmlDecode(title),
                PublicKey = publicKey,
                CreationDate = this.clock.UtcNow(),
                LastEntryDate = this.clock.UtcNow(),
                CreatedBy = createdBy
            };

            this.AddGroup(CreateGroup(Guid.NewGuid(), "New Section", null, null, null,false), null);
        }

        public void CloneQuestionnaire(string title, bool isPublic, Guid createdBy, Guid publicKey, IQuestionnaireDocument source)
        {
            this.ThrowDomainExceptionIfQuestionnaireTitleIsEmptyOrWhitespacesOrTooLong(title);

            var document = source as QuestionnaireDocument;
            if (document == null)
                throw new QuestionnaireException(DomainExceptionType.TemplateIsInvalid, "only QuestionnaireDocuments are supported for now");

            var clonedDocument = (QuestionnaireDocument)document.Clone();
            clonedDocument.PublicKey = this.Id;
            clonedDocument.CreatedBy = createdBy;
            clonedDocument.CreationDate = this.clock.UtcNow();
            clonedDocument.Title = title;
            clonedDocument.IsPublic = isPublic;

            foreach (var lookupTable in clonedDocument.LookupTables)
            {
                var lookupTableName = lookupTable.Value.TableName;

                if (!this.lookupTableService.IsLookupTableEmpty(document.PublicKey, lookupTable.Key, lookupTableName))
                {
                    lookupTableService.CloneLookupTable(document.PublicKey, lookupTable.Key, lookupTableName, this.Id, lookupTable.Key);
                }
            }

            foreach (var attachment in clonedDocument.Attachments)
            {
                var newAttachmentId = Guid.NewGuid();
                this.attachmentService.CloneMeta(attachment.AttachmentId, newAttachmentId, clonedDocument.PublicKey);
                attachment.AttachmentId = newAttachmentId;
            }

            foreach (var translation in clonedDocument.Translations)
            {
                var newTranslationId = Guid.NewGuid();
                this.translationService.CloneTranslation(document.PublicKey, translation.Id, clonedDocument.PublicKey, newTranslationId);
                translation.Id = newTranslationId;
            }

            this.innerDocument = clonedDocument;
        }

        public void ImportQuestionnaire(Guid createdBy, IQuestionnaireDocument source)
        {
            var document = source as QuestionnaireDocument;
            if (document == null)
                throw new QuestionnaireException(DomainExceptionType.TemplateIsInvalid, "Only QuestionnaireDocuments are supported for now");
            if (document.IsDeleted)
                throw new QuestionnaireException(DomainExceptionType.TemplateIsInvalid, "Trying to import template of deleted questionnaire");

            document.CreatedBy = createdBy;
            
            this.innerDocument = document;
        }

        public void UpdateQuestionnaire(UpdateQuestionnaire command)
#warning CRUD
        {
            if (!command.IsResponsibleAdmin) 
                this.ThrowDomainExceptionIfViewerDoesNotHavePermissionsForEditQuestionnaire(command.ResponsibleId);

            this.ThrowDomainExceptionIfQuestionnaireTitleIsEmptyOrWhitespacesOrTooLong(command.Title);

            this.innerDocument.Title = System.Web.HttpUtility.HtmlDecode(command.Title);
            this.innerDocument.IsPublic = command.IsPublic;
        }

        public void DeleteQuestionnaire()
        {
            this.innerDocument.IsDeleted = true;
        }

        public IEnumerable<QuestionnaireNodeReference> FindAllTexts(string searchFor, bool matchCase, bool matchWholeWord, bool useRegex)
        {
            Regex searchRegex = BuildSearchRegex(searchFor, matchCase, matchWholeWord, useRegex);
            IEnumerable<IComposite> allEntries = this.innerDocument.Children.TreeToEnumerableDepthFirst(x => x.Children);
            foreach (var questionnaireItem in allEntries)
            {
                var title = questionnaireItem.GetTitle();
                var variable = questionnaireItem.GetVariable();

                if (MatchesSearchTerm(variable, searchRegex))
                {
                    yield return QuestionnaireNodeReference.CreateFrom(questionnaireItem, QuestionnaireVerificationReferenceProperty.VariableName);
                }
                if (MatchesSearchTerm(title, searchRegex))
                {
                    yield return QuestionnaireNodeReference.CreateFrom(questionnaireItem, QuestionnaireVerificationReferenceProperty.Title);
                }
                
                var question = questionnaireItem as IQuestion;
                if (question != null)
                {
                    if (question.Answers != null && !(question.IsFilteredCombobox.GetValueOrDefault() || question.CascadeFromQuestionId.HasValue))
                    {
                        for (int i = 0; i < question.Answers.Count; i++)
                        {
                            if (MatchesSearchTerm(question.Answers[i].AnswerText, searchRegex))
                            {
                                yield return QuestionnaireNodeReference.CreateFrom(questionnaireItem, QuestionnaireVerificationReferenceProperty.Option, i);
                            }
                        }
                    }

                    if (MatchesSearchTerm(question.Properties.OptionsFilterExpression, searchRegex) || MatchesSearchTerm(question.LinkedFilterExpression, searchRegex))
                    {
                        yield return QuestionnaireNodeReference.CreateFrom(questionnaireItem, QuestionnaireVerificationReferenceProperty.OptionsFilter);
                    }
                }

                var group = questionnaireItem as IGroup;
                if (group != null)
                {
                    if (group.IsRoster && group.RosterSizeSource == RosterSizeSourceType.FixedTitles)
                    {
                        for (int i = 0; i < group.FixedRosterTitles.Length; i++)
                        {
                            if (MatchesSearchTerm(group.FixedRosterTitles[i].Title, searchRegex))
                            {
                                yield return QuestionnaireNodeReference.CreateFrom(questionnaireItem, QuestionnaireVerificationReferenceProperty.FixedRosterItem, i);
                            }
                        }
                    }
                }

                var conditional = questionnaireItem as IConditional;
                if (MatchesSearchTerm(conditional?.ConditionExpression, searchRegex))
                {
                    yield return QuestionnaireNodeReference.CreateFrom(questionnaireItem, QuestionnaireVerificationReferenceProperty.EnablingCondition);
                }

                var validatable = questionnaireItem as IValidatable;
                if (validatable != null)
                {
                    for (int i = 0; i < validatable.ValidationConditions.Count; i++)
                    {
                        if(MatchesSearchTerm(validatable.ValidationConditions[i].Expression, searchRegex))
                        {
                            yield return QuestionnaireNodeReference.CreateFrom(questionnaireItem, QuestionnaireVerificationReferenceProperty.ValidationExpression, i);
                        }
                        if (MatchesSearchTerm(validatable.ValidationConditions[i].Message, searchRegex))
                        {
                            yield return QuestionnaireNodeReference.CreateFrom(questionnaireItem, QuestionnaireVerificationReferenceProperty.ValidationMessage, i);
                        }
                    }
                }

                var questionnaireVariable = questionnaireItem as IVariable;
                if (questionnaireVariable != null && MatchesSearchTerm(questionnaireVariable.Expression, searchRegex))
                {
                    yield return QuestionnaireNodeReference.CreateFrom(questionnaireVariable, QuestionnaireVerificationReferenceProperty.VariableContent);
                }

                var staticText = questionnaireItem as IStaticText;
                if (staticText != null && MatchesSearchTerm(staticText.AttachmentName, searchRegex))
                {
                    yield return QuestionnaireNodeReference.CreateFrom(staticText, QuestionnaireVerificationReferenceProperty.AttachmentName);
                }
            }

            foreach (var macro in this.innerDocument.Macros.OrderBy(x => x.Value.Name))
            {
                if (MatchesSearchTerm(macro.Value.Content, searchRegex))
                {
                    yield return QuestionnaireNodeReference.CreateForMacro(macro.Key);
                }
            }
        }

        private static Regex BuildSearchRegex(string searchFor, bool matchCase, bool matchWholeWord, bool useRegex)
        {
            RegexOptions options = RegexOptions.Compiled | RegexOptions.CultureInvariant;
            if (!matchCase)
            {
                options |= RegexOptions.IgnoreCase;
            }
            string encodedSearchPattern = useRegex ? searchFor : Regex.Escape(searchFor);
            string pattern = matchWholeWord ? $@"\b{encodedSearchPattern}\b" : encodedSearchPattern;

            Regex searchRegex = new Regex(pattern, options);
            return searchRegex;
        }

        private static bool MatchesSearchTerm(string target, Regex searchRegex)
        {
            if (target.IsNullOrEmpty()) return false;

            return searchRegex.IsMatch(target);
        }

        private static string ReplaceUsingSearchTerm(string target, Regex searchFor, string replaceWith)
        {
            return searchFor.Replace(target, replaceWith);
        }

        public void ReplaceTexts(ReplaceTextsCommand command)
        {
            ThrowDomainExceptionIfViewerDoesNotHavePermissionsForEditQuestionnaire(command.ResponsibleId);

            var allEntries = this.innerDocument.Children.TreeToEnumerable(x => x.Children);
            this.affectedByReplaceEntries = 0;
            var searchRegex = BuildSearchRegex(command.SearchFor, command.MatchCase, command.MatchWholeWord, command.UseRegex);
            foreach (var questionnaireItem in allEntries)
            {
                bool replacedAny = false;
                var title = questionnaireItem.GetTitle();
                if (MatchesSearchTerm(title, searchRegex))
                {
                    replacedAny = true;
                    questionnaireItem.SetTitle(ReplaceUsingSearchTerm(title, searchRegex, command.ReplaceWith));
                }

                var variableName = questionnaireItem.GetVariable();
                if (MatchesSearchTerm(variableName, searchRegex))
                {
                    replacedAny = true;
                    questionnaireItem.SetVariable(ReplaceUsingSearchTerm(variableName, searchRegex, command.ReplaceWith));
                }

                var conditional = questionnaireItem as IConditional;
                if (MatchesSearchTerm(conditional?.ConditionExpression, searchRegex))
                {
                    replacedAny = true;
                    string newCondition = ReplaceUsingSearchTerm(conditional.ConditionExpression, searchRegex, command.ReplaceWith);
                    conditional.ConditionExpression = newCondition;
                }

                var validatable = questionnaireItem as IValidatable;
                if (validatable != null)
                {
                    foreach (var validationCondition in validatable.ValidationConditions)
                    {
                        if (MatchesSearchTerm(validationCondition.Expression, searchRegex))
                        {
                            replacedAny = true;
                            string newValidationCondition = ReplaceUsingSearchTerm(validationCondition.Expression, searchRegex, command.ReplaceWith);
                            validationCondition.Expression = newValidationCondition;
                        }
                        if (MatchesSearchTerm(validationCondition.Message, searchRegex))
                        {
                            replacedAny = true;
                            string newMessage = ReplaceUsingSearchTerm(validationCondition.Message, searchRegex, command.ReplaceWith);
                            validationCondition.Message = newMessage;
                        }
                    }
                }

                var questionnaireVariable = questionnaireItem as IVariable;
                if (questionnaireVariable != null)
                {
                    if (MatchesSearchTerm(questionnaireVariable.Expression, searchRegex))
                    {
                        replacedAny = true;
                        questionnaireVariable.Expression = ReplaceUsingSearchTerm(questionnaireVariable.Expression, searchRegex, command.ReplaceWith);
                    }
                }

                var question = questionnaireItem as IQuestion;
                if (question != null)
                {
                    if (question.Answers != null && !(question.IsFilteredCombobox.GetValueOrDefault() || question.CascadeFromQuestionId.HasValue))
                    {
                        foreach (var questionAnswer in question.Answers)
                        {
                            if (MatchesSearchTerm(questionAnswer.AnswerText, searchRegex))
                            {
                                replacedAny = true;
                                questionAnswer.AnswerText = ReplaceUsingSearchTerm(questionAnswer.AnswerText, searchRegex, command.ReplaceWith);
                            }
                        }
                    }

                    if (MatchesSearchTerm(question.Properties.OptionsFilterExpression, searchRegex))
                    {
                        replacedAny = true;
                        question.Properties.OptionsFilterExpression = ReplaceUsingSearchTerm(question.Properties.OptionsFilterExpression, searchRegex, command.ReplaceWith);
                    }

                    if (MatchesSearchTerm(question.LinkedFilterExpression, searchRegex))
                    {
                        replacedAny = true;
                        question.LinkedFilterExpression = ReplaceUsingSearchTerm(question.LinkedFilterExpression, searchRegex, command.ReplaceWith);
                    }
                }

                var group = questionnaireItem as IGroup;
                if (group != null)
                {
                    if (group.IsRoster && group.RosterSizeSource == RosterSizeSourceType.FixedTitles)
                    {
                        foreach (var fixedRosterTitle in group.FixedRosterTitles)
                        {
                            if (MatchesSearchTerm(fixedRosterTitle.Title, searchRegex))
                            {
                                replacedAny = true;
                                fixedRosterTitle.Title = ReplaceUsingSearchTerm(fixedRosterTitle.Title, searchRegex, command.ReplaceWith);
                            }
                        }
                    }
                }

                var staticText = questionnaireItem as IStaticText;
                if (staticText != null)
                {
                    if (MatchesSearchTerm(staticText.AttachmentName, searchRegex))
                    {
                        replacedAny = true;
                        staticText.AttachmentName = ReplaceUsingSearchTerm(staticText.AttachmentName, searchRegex, command.ReplaceWith);
                    }
                }

                if (replacedAny)
                {
                    this.affectedByReplaceEntries++;
                }
            }

            foreach (var macro in this.innerDocument.Macros.Values)
            {
                if (MatchesSearchTerm(macro.Content, searchRegex))
                {
                    this.affectedByReplaceEntries++;
                    macro.Content = ReplaceUsingSearchTerm(macro.Content, searchRegex, command.ReplaceWith);
                }
            }
        }

        public int GetLastReplacedEntriesCount()
        {
            return this.affectedByReplaceEntries;
        }

        #endregion

        #region Macro command handlers

        public void AddMacro(AddMacro command)
        {
            this.ThrowDomainExceptionIfViewerDoesNotHavePermissionsForEditQuestionnaire(command.ResponsibleId);
            this.ThrowDomainExceptionIfMacroAlreadyExist(command.MacroId);

            this.innerDocument.Macros[command.MacroId] = new Macro();
        }

        public void UpdateMacro(UpdateMacro command)
        {
            this.ThrowDomainExceptionIfViewerDoesNotHavePermissionsForEditQuestionnaire(command.ResponsibleId);
            this.ThrowDomainExceptionIfMacroIsAbsent(command.MacroId);
            this.ThrowDomainExceptionIfMacroContentIsEmpty(command.Content);
            
            if (!innerDocument.Macros.ContainsKey(command.MacroId))
                return;

            var macro = this.innerDocument.Macros[command.MacroId];
            macro.Name = command.Name;
            macro.Content = command.Content;
            macro.Description = command.Description;
        }

        public void DeleteMacro(DeleteMacro command)
        {
            this.ThrowDomainExceptionIfViewerDoesNotHavePermissionsForEditQuestionnaire(command.ResponsibleId);
            this.ThrowDomainExceptionIfMacroIsAbsent(command.MacroId);

            innerDocument.Macros.Remove(command.MacroId);
        }

        #endregion

        #region Attachment command handlers
                
        public void AddOrUpdateAttachment(AddOrUpdateAttachment command)
        {
            this.ThrowDomainExceptionIfViewerDoesNotHavePermissionsForEditQuestionnaire(command.ResponsibleId);

            innerDocument.Attachments.RemoveAll(x => x.AttachmentId == command.AttachmentId);

            if(command.OldAttachmentId.HasValue)
                innerDocument.Attachments.RemoveAll(x => x.AttachmentId == command.OldAttachmentId.Value);

            innerDocument.Attachments.Add(new Attachment
            {
                AttachmentId = command.AttachmentId,
                Name = command.AttachmentName,
                ContentId = command.AttachmentContentId
            });
        }

        public void DeleteAttachment(DeleteAttachment command)
        {
            this.ThrowDomainExceptionIfViewerDoesNotHavePermissionsForEditQuestionnaire(command.ResponsibleId);
            this.innerDocument.Attachments.RemoveAll(x => x.AttachmentId == command.AttachmentId);
        }

        #endregion

        #region Translation command handlers
                
        public void AddOrUpdateTranslation(AddOrUpdateTranslation command)
        {
            this.ThrowDomainExceptionIfViewerDoesNotHavePermissionsForEditQuestionnaire(command.ResponsibleId);

            var translation = new Translation
            {
                Id = command.TranslationId,
                Name = command.Name,
            };
            innerDocument.Translations.RemoveAll(x => x.Id == command.TranslationId);

            if(command.OldTranslationId.HasValue)
                innerDocument.Translations.RemoveAll(x => x.Id == command.OldTranslationId.Value);

            innerDocument.Translations.Add(translation);
        }

        public void DeleteTranslation(DeleteTranslation command)
        {
            this.ThrowDomainExceptionIfViewerDoesNotHavePermissionsForEditQuestionnaire(command.ResponsibleId);
            this.innerDocument.Translations.RemoveAll(x => x.Id == command.TranslationId);
        }

        #endregion

        #region Lookup table command handlers

        public void AddLookupTable(AddLookupTable command)
        {
            this.ThrowDomainExceptionIfViewerDoesNotHavePermissionsForEditQuestionnaire(command.ResponsibleId);

            this.ThrowDomainExceptionIfVariableNameIsInvalid(command.LookupTableId, command.LookupTableName, DefaultVariableLengthLimit);

            if (this.innerDocument.LookupTables.ContainsKey(command.LookupTableId))
            {
                throw new QuestionnaireException(DomainExceptionType.LookupTableAlreadyExist, ExceptionMessages.LookupTableAlreadyExist);
            }

            innerDocument.LookupTables[command.LookupTableId] = new LookupTable()
            {
                TableName = command.LookupTableName,
                FileName = command.LookupTableFileName
            };
        }

        public void UpdateLookupTable(UpdateLookupTable command)
        {
            this.ThrowDomainExceptionIfViewerDoesNotHavePermissionsForEditQuestionnaire(command.ResponsibleId);
            this.ThrowDomainExceptionIfVariableNameIsInvalid(command.LookupTableId, command.LookupTableName, DefaultVariableLengthLimit);

            if (command.OldLookupTableId.HasValue)
            {
                if (innerDocument.LookupTables.ContainsKey(command.OldLookupTableId.Value))
                    innerDocument.LookupTables.Remove(command.OldLookupTableId.Value);
            }

            innerDocument.LookupTables[command.LookupTableId] = new LookupTable
            {
                TableName = command.LookupTableName,
                FileName = command.LookupTableFileName
            };
        }

        public void DeleteLookupTable(DeleteLookupTable command)
        {
            this.ThrowDomainExceptionIfViewerDoesNotHavePermissionsForEditQuestionnaire(command.ResponsibleId);

            if (!this.innerDocument.LookupTables.ContainsKey(command.LookupTableId))
            {
                throw new QuestionnaireException(DomainExceptionType.LookupTableIsAbsent, ExceptionMessages.LookupTableIsAbsent);
            }
            innerDocument.LookupTables.Remove(command.LookupTableId);
        }

        #endregion

        #region Group command handlers

        public void AddGroupAndMoveIfNeeded(Guid groupId, Guid responsibleId, string title, 
            string variableName, Guid? rosterSizeQuestionId, string description, string condition, 
            bool hideIfDisabled, Guid? parentGroupId, bool isRoster, RosterSizeSourceType rosterSizeSource,
            FixedRosterTitleItem[] rosterFixedTitles, Guid? rosterTitleQuestionId, int? index = null)
        {
            PrepareGeneralProperties(ref title, ref variableName);

            this.ThrowDomainExceptionIfViewerDoesNotHavePermissionsForEditQuestionnaire(responsibleId);
            this.ThrowDomainExceptionIfGroupAlreadyExists(groupId);

            this.ThrowDomainExceptionIfGroupTitleIsEmptyOrWhitespacesOrTooLong(title);

            this.ThrowDomainExceptionIfVariableNameIsInvalid(groupId, variableName, DefaultVariableLengthLimit);

            var fixedTitles = GetRosterFixedTitlesOrThrow(rosterFixedTitles);

            this.ThrowIfRosterInformationIsIncorrect(groupId: groupId, isRoster: isRoster, rosterSizeSource: rosterSizeSource,
                rosterSizeQuestionId: rosterSizeQuestionId, rosterFixedTitles: fixedTitles,
                rosterTitleQuestionId: rosterTitleQuestionId, rosterDepthFunc: () => GetQuestionnaireItemDepthAsVector(parentGroupId));

            if (parentGroupId.HasValue)
            {
                this.ThrowIfChapterHasMoreThanAllowedLimit(parentGroupId.Value);
                this.ThrowIfTargetGroupHasReachedAllowedDepthLimit(parentGroupId.Value);
            }

            this.ThrowDomainExceptionIfTextContainsIncorrectSubstitution(
                text: title,
                entityId: groupId,
                variableName: variableName,
                parentGroup: parentGroupId.HasValue ? this.innerDocument.Find<IGroup>(parentGroupId.Value) : this.innerDocument);

            this.AddGroup(CreateGroup(groupId, title, variableName, description, condition, hideIfDisabled),
                parentGroupId);

            if (isRoster)
            {
                this.innerDocument.UpdateGroup(groupId, group =>
                {
                    group.RosterSizeQuestionId = rosterSizeQuestionId;
                    group.RosterSizeSource = rosterSizeSource;
                    group.FixedRosterTitles = fixedTitles;
                    group.RosterTitleQuestionId = rosterTitleQuestionId;
                    group.IsRoster = true;
                });
            }
            else
            {
                this.RemoveRosterFlagFromGroup( groupId);
            }

            if (index.HasValue)
            {
                this.innerDocument.MoveItem(groupId, parentGroupId, index.Value);
            }
        }

        private static Guid? GetIdOrReturnSameId(Dictionary<Guid, Guid> replacementIdDictionary, Guid? id)
        {
            if (!id.HasValue)
                return null;

            return replacementIdDictionary.ContainsKey(id.Value) ? replacementIdDictionary[id.Value] : id;
        }


        public void UpdateGroup(Guid groupId, Guid responsibleId,
            string title,string variableName, Guid? rosterSizeQuestionId, string description, string condition, bool hideIfDisabled, 
            bool isRoster, RosterSizeSourceType rosterSizeSource, FixedRosterTitleItem[] rosterFixedTitles, Guid? rosterTitleQuestionId)
        {
            PrepareGeneralProperties(ref title, ref variableName);

            this.ThrowDomainExceptionIfViewerDoesNotHavePermissionsForEditQuestionnaire(responsibleId);

            this.ThrowDomainExceptionIfGroupDoesNotExist(groupId);

            this.ThrowDomainExceptionIfMoreThanOneGroupExists(groupId);

            this.ThrowDomainExceptionIfGroupTitleIsEmptyOrWhitespacesOrTooLong(title);

            this.ThrowDomainExceptionIfVariableNameIsInvalid(groupId, variableName, DefaultVariableLengthLimit);

            var fixedTitles = GetRosterFixedTitlesOrThrow(rosterFixedTitles);

            this.ThrowIfRosterInformationIsIncorrect(groupId: groupId, isRoster: isRoster, rosterSizeSource: rosterSizeSource,
                rosterSizeQuestionId: rosterSizeQuestionId, rosterFixedTitles: fixedTitles,
                rosterTitleQuestionId: rosterTitleQuestionId, rosterDepthFunc: () => GetQuestionnaireItemDepthAsVector(groupId));
            
            var group = this.GetGroupById(groupId);

            var wasGroupAndBecomeARoster = !@group.IsRoster && isRoster;
            var wasRosterAndBecomeAGroup = @group.IsRoster && !isRoster;

            if (wasGroupAndBecomeARoster)
            {
                this.ThrowIfGroupCantBecomeARosterBecauseOfPrefilledQuestions(group);
            }
            if (wasRosterAndBecomeAGroup)
            {
                this.ThrowIfRosterHaveAQuestionThatUsedAsRosterTitleQuestionOfOtherGroups(group);
                this.ThrowIfRosterCantBecomeAGroupBecauseContainsLinkedSourceQuestions(group);
                this.ThrowIfRosterCantBecomeAGroupBecauseOfReferencesOnRosterTitleInSubstitutions(group, wasRosterAndBecomeAGroup: true);
            }

            this.ThrowDomainExceptionIfTextContainsIncorrectSubstitution(
                text: title,
                entityId: groupId,
                variableName: variableName,
                parentGroup: @group.GetParent() as IGroup ?? this.innerDocument);

            this.innerDocument.UpdateGroup(groupId,
                title,
                variableName,
                description,
                condition,
                hideIfDisabled);

            if (isRoster)
            {
                this.innerDocument.UpdateGroup(groupId, groupToUpdate =>
                {
                    groupToUpdate.RosterSizeQuestionId = rosterSizeQuestionId;
                    groupToUpdate.RosterSizeSource = rosterSizeSource;
                    groupToUpdate.FixedRosterTitles = fixedTitles;
                    groupToUpdate.RosterTitleQuestionId = rosterTitleQuestionId;
                    groupToUpdate.IsRoster = true;
                });
            }
            else
            {
                this.RemoveRosterFlagFromGroup(groupId);
            }
        }

        public void DeleteGroup(Guid groupId, Guid responsibleId)
        {
            this.ThrowDomainExceptionIfViewerDoesNotHavePermissionsForEditQuestionnaire(responsibleId);
            this.ThrowDomainExceptionIfGroupDoesNotExist(groupId);
            this.ThrowDomainExceptionIfMoreThanOneGroupExists(groupId);
            this.ThrowDomainExceptionIfGroupQuestionsUsedAsRosterTitleQuestionOfOtherGroups(groupId);

            if (this.QuestionnaireDocument.Children.Count == 1 &&
                this.QuestionnaireDocument.Children[0].PublicKey == groupId)
            {
                throw new QuestionnaireException(DomainExceptionType.Undefined, "Last existing section can not be removed from questionnaire");
            }

            var group = this.GetGroupById(groupId);

            this.ThrowDomainExceptionIfRosterQuestionsUsedAsLinkedSourceQuestions(group);

            this.innerDocument.RemoveEntity(groupId);
        }

        public void MoveGroup(Guid groupId, Guid? targetGroupId, int targetIndex, Guid responsibleId)
        {
            this.ThrowDomainExceptionIfViewerDoesNotHavePermissionsForEditQuestionnaire(responsibleId);
            this.ThrowDomainExceptionIfGroupDoesNotExist(groupId);

            this.ThrowDomainExceptionIfMoreThanOneGroupExists(groupId);

            if (targetGroupId.HasValue)
            {
                this.ThrowDomainExceptionIfGroupDoesNotExist(targetGroupId.Value);
            }

            var sourceGroup = this.GetGroupById(groupId);

            if (targetGroupId.HasValue)
            {
                var sourceChapter = this.innerDocument.GetChapterOfItemById(groupId);
                var targetChapter = this.innerDocument.GetChapterOfItemById(targetGroupId.Value);

                if (sourceChapter.PublicKey != targetChapter.PublicKey)
                {
                    var numberOfMovedItems = sourceGroup.Children
                        .TreeToEnumerable(x => x.Children)
                        .Count();

                    var numberOfItemsInChapter = targetChapter.Children
                        .TreeToEnumerable(x => x.Children)
                        .Count();

                    if ((numberOfMovedItems + numberOfItemsInChapter) >= MaxChapterItemsCount)
                    {
                        throw new QuestionnaireException(string.Format("Section cannot have more than {0} elements", MaxChapterItemsCount));
                    }
                }

                var targetGroupDepthLevel = this.GetAllParentGroups(this.GetGroupById(targetGroupId.Value)).Count();
                var sourceGroupMaxChildNestingDepth = GetMaxChildGroupNestingDepth(sourceGroup);

                if ((targetGroupDepthLevel + sourceGroupMaxChildNestingDepth) > MaxGroupDepth)
                {
                    throw new QuestionnaireException($"Sub-section or roster depth cannot be higher than {MaxGroupDepth}");
                }
                
            }
            
            // if we don't have a target group we would like to move source group into root of questionnaire
            var targetGroup = targetGroupId.HasValue ? this.GetGroupById(targetGroupId.Value) : this.innerDocument;

            this.ThrowIfTargetIndexIsNotAcceptable(targetIndex, targetGroup, sourceGroup.GetParent() as IGroup);

            this.ThrowIfGroupFromRosterThatContainsRosterTitleQuestionMovedToAnotherGroup(sourceGroup, targetGroup);
            this.ThrowIfSourceGroupContainsInvalidRosterSizeQuestions(sourceGroup, targetGroup);
            this.ThrowIfGroupFromRosterThatContainsLinkedSourceQuestionsMovedToGroup(sourceGroup, targetGroup);
            this.ThrowIfGroupMovedFromRosterToGroupAndContainsRosterTitleInSubstitution(sourceGroup, targetGroup);

            this.ThrowIfRosterInformationIsIncorrect(groupId: groupId, isRoster: sourceGroup.IsRoster,
                rosterSizeSource: sourceGroup.RosterSizeSource,
                rosterSizeQuestionId: sourceGroup.RosterSizeQuestionId, rosterFixedTitles: sourceGroup.FixedRosterTitles,
                rosterTitleQuestionId: sourceGroup.RosterTitleQuestionId,
                rosterDepthFunc: () => GetQuestionnaireItemDepthAsVector(targetGroup.PublicKey));

            this.ThrowDomainExceptionIfTextContainsIncorrectSubstitution(
                    text: sourceGroup.Title,
                    entityId: sourceGroup.PublicKey,
                    variableName: sourceGroup.VariableName,
                    parentGroup: targetGroup);

            this.innerDocument.MoveItem(groupId, targetGroupId, targetIndex);
            
        }

        #endregion

        public void AddDefaultTypeQuestionAdnMoveIfNeeded(AddDefaultTypeQuestion command)
        {
            this.ThrowDomainExceptionIfQuestionAlreadyExists(command.QuestionId);
            var parentGroup = this.GetGroupById(command.ParentGroupId);
            this.ThrowIfChapterHasMoreThanAllowedLimit(command.ParentGroupId);

            this.ThrowDomainExceptionIfViewerDoesNotHavePermissionsForEditQuestionnaire(command.ResponsibleId);
            
            if (parentGroup != null)
            {
                this.ThrowIfChapterHasMoreThanAllowedLimit(parentGroup.PublicKey);
            }

            IQuestion question = CreateQuestion(command.QuestionId,
                questionText: command.Title,
                questionType: QuestionType.Text,
                stataExportCaption: null,
                variableLabel: null,
                featured: false,
                questionScope: QuestionScope.Interviewer,
                conditionExpression: null,
                hideIfDisabled: false,
                validationConditions: new List<ValidationCondition>(),
                instructions: null,
                questionProperties: new QuestionProperties(false, false),
                linkedToQuestionId: null,
                areAnswersOrdered: null,
                maxAllowedAnswers: null,
                mask: null,
                isFilteredCombobox: false,
                cascadeFromQuestionId: null,
                capital: false,
                answerOrder: null,
                answers: null,
                isInteger: null,
                yesNoView: null,
                linkedToRosterId: null,
                countOfDecimalPlaces: null,
                maxAnswerCount: null,
                linkedFilterExpression: null,
                isTimestamp: false);

            this.innerDocument.Add(question, command.ParentGroupId);
            
            if (command.Index.HasValue)
            {
                this.innerDocument.MoveItem(command.QuestionId, command.ParentGroupId, command.Index.Value);
                this.innerDocument.CheckIsQuestionHeadAndUpdateRosterProperties(command.QuestionId, command.ParentGroupId);
            }
        }

        public void DeleteQuestion(Guid questionId, Guid responsibleId)
        {
            this.ThrowDomainExceptionIfViewerDoesNotHavePermissionsForEditQuestionnaire(responsibleId);
            this.ThrowDomainExceptionIfQuestionDoesNotExist(questionId);
            this.ThrowDomainExceptionIfMoreThanOneQuestionExists(questionId);
            this.ThrowDomainExceptionIfQuestionUsedInConditionOrValidationOfOtherQuestionsAndGroups(questionId);
            this.ThrowIfQuestionIsUsedAsRosterSize(questionId);
            this.ThrowIfQuestionIsUsedAsRosterTitle(questionId);
            this.ThrowIfQuestionIsUsedAsCascadingParent(questionId);

            this.innerDocument.RemoveEntity(questionId);
            this.innerDocument.RemoveHeadPropertiesFromRosters(questionId);
        }

        public void MoveQuestion(Guid questionId, Guid targetGroupId, int targetIndex, Guid responsibleId)
        {
            this.ThrowDomainExceptionIfViewerDoesNotHavePermissionsForEditQuestionnaire(responsibleId);
            this.ThrowDomainExceptionIfQuestionDoesNotExist(questionId);
            this.ThrowDomainExceptionIfMoreThanOneQuestionExists(questionId);
            this.ThrowDomainExceptionIfGroupDoesNotExist(targetGroupId);

            this.ThrowIfChapterHasMoreThanAllowedLimit(targetGroupId);

            var question = this.innerDocument.Find<AbstractQuestion>(questionId);
            var targetGroup = this.innerDocument.Find<IGroup>(targetGroupId);

            this.ThrowIfTargetIndexIsNotAcceptable(targetIndex, targetGroup, question.GetParent() as IGroup);

            this.ThrowDomainExceptionIfQuestionTitleContainsIncorrectSubstitution(question.QuestionText, question.StataExportCaption,
                questionId, question.Featured, targetGroup);

            foreach (var validationCondition in question.ValidationConditions)
            {
                this.ThrowDomainExceptionIfTextContainsIncorrectSubstitution(
                    text: validationCondition.Message,
                    entityId: questionId,
                    variableName: question.StataExportCaption,
                    parentGroup: targetGroup);
            }
            
            this.ThrowDomainExceptionIfQuestionIsPrefilledAndParentGroupIsRoster(question.Featured, targetGroup);
            this.ThrowDomainExceptionIfQuestionIsRosterTitleAndItsMovedToIncorrectGroup(question, targetGroup);

            this.ThrowDomainExceptionIfQuestionIsRosterSizeAndItsMovedToIncorrectGroup(question, targetGroup);

            this.innerDocument.MoveItem(questionId, targetGroupId, targetIndex);
            this.innerDocument.CheckIsQuestionHeadAndUpdateRosterProperties(questionId, targetGroupId);
        }

        public void UpdateTextQuestion(UpdateTextQuestion command)
        {
            var title = command.Title;
            var variableName = command.VariableName;

            PrepareGeneralProperties(ref title, ref variableName);

            IGroup parentGroup = this.innerDocument.GetParentById(command.QuestionId);

            this.ThrowDomainExceptionIfQuestionDoesNotExist(command.QuestionId);
            this.ThrowDomainExceptionIfMoreThanOneQuestionExists(command.QuestionId);
            this.ThrowDomainExceptionIfGeneralQuestionSettingsAreInvalid(command.QuestionId, parentGroup, title, variableName, command.IsPreFilled,
               QuestionType.Text, command.ResponsibleId, command.ValidationConditions);

            var question = this.innerDocument.Find<AbstractQuestion>(command.QuestionId);
            IQuestion newQuestion = CreateQuestion(
                        command.QuestionId,
                        QuestionType.Text,
                        command.Scope,
                        title,
                        variableName,
                        command.VariableLabel,
                        command.EnablementCondition,
                        command.HideIfDisabled,
                        null,
                        command.IsPreFilled,
                        false,
                        command.Instructions,
                        command.Properties,
                        command.Mask,
                        null, null, null, null,
                        null, null, null, null,
                        null, null, null,
                        command.ValidationConditions,
                        null,
                        false);

            this.innerDocument.ReplaceEntity(question, newQuestion);
        }

        public void UpdateGpsCoordinatesQuestion(UpdateGpsCoordinatesQuestion command)
        {
            var title = command.Title;
            var variableName = command.VariableName;

            PrepareGeneralProperties(ref title, ref variableName);

            IGroup parentGroup = this.innerDocument.GetParentById(command.QuestionId);

            this.ThrowDomainExceptionIfQuestionDoesNotExist(command.QuestionId);
            this.ThrowDomainExceptionIfMoreThanOneQuestionExists(command.QuestionId);
            this.ThrowDomainExceptionIfGeneralQuestionSettingsAreInvalid(command.QuestionId, parentGroup, title, variableName, command.IsPreFilled, QuestionType.GpsCoordinates, 
                command.ResponsibleId, command.ValidationConditions);

            var question = this.innerDocument.Find<AbstractQuestion>(command.QuestionId);
            IQuestion newQuestion = CreateQuestion(
                        question.PublicKey,
                        QuestionType.GpsCoordinates,
                        command.Scope,
                        title,
                        variableName,
                        command.VariableLabel,
                        command.EnablementCondition,
                        command.HideIfDisabled,
                        Order.AZ,
                        command.IsPreFilled,
                        false,
                        command.Instructions,
                        command.Properties,
                        null, null, null, null,
                        null, null, null, null,
                        null, null, null, null,
                        command.ValidationConditions,
                        null,
                        false);

            this.innerDocument.ReplaceEntity(question, newQuestion);
        }

        public void UpdateDateTimeQuestion(UpdateDateTimeQuestion command)
        {
            var title = command.Title;
            var variableName = command.VariableName;

            PrepareGeneralProperties(ref title, ref variableName);

            IGroup parentGroup = this.innerDocument.GetParentById(command.QuestionId);
            
            this.ThrowDomainExceptionIfQuestionDoesNotExist(command.QuestionId);
            this.ThrowDomainExceptionIfMoreThanOneQuestionExists(command.QuestionId);
            this.ThrowDomainExceptionIfGeneralQuestionSettingsAreInvalid(command.QuestionId, parentGroup, title, variableName, command.IsPreFilled,
                QuestionType.DateTime, command.ResponsibleId, command.ValidationConditions);


            var question = this.innerDocument.Find<AbstractQuestion>(command.QuestionId);
            IQuestion newQuestion = CreateQuestion(
                question.PublicKey,
                QuestionType.DateTime,
                command.Scope,
                command.Title,
                command.VariableName,
                command.VariableLabel,
                command.EnablementCondition,
                command.HideIfDisabled,
                null,
                command.IsPreFilled,
                false,
                command.Instructions,
                command.Properties,
                null, null, null, null, null,null, null, null, null, null, null,null,
                command.ValidationConditions,
                null,
                command.IsTimestamp);

            this.innerDocument.ReplaceEntity(question, newQuestion);
        }

        public void UpdateMultiOptionQuestion(Guid questionId, string title, string variableName, string variableLabel, QuestionScope scope, string enablementCondition,
            bool hideIfDisabled, string instructions, Guid responsibleId, Option[] options, Guid? linkedToEntityId, bool areAnswersOrdered, int? maxAllowedAnswers, 
            bool yesNoView, IList<ValidationCondition> validationConditions, string linkedFilterExpression, QuestionProperties properties)
        {
            PrepareGeneralProperties(ref title, ref variableName);
            IGroup parentGroup = this.innerDocument.GetParentById(questionId);

            this.ThrowDomainExceptionIfQuestionDoesNotExist(questionId);
            this.ThrowDomainExceptionIfMoreThanOneQuestionExists(questionId);
            this.ThrowDomainExceptionIfGeneralQuestionSettingsAreInvalid(questionId, parentGroup, title, variableName, false, QuestionType.MultyOption, responsibleId, validationConditions);
            this.ThrowIfQuestionIsRosterTitleLinkedCategoricalQuestion(questionId, linkedToEntityId);
            this.ThrowIfCategoricalQuestionIsInvalid(questionId, options, linkedToEntityId, false, null, scope, null);
            this.ThrowIfMaxAllowedAnswersInvalid(QuestionType.MultyOption, linkedToEntityId, maxAllowedAnswers, options);
            this.ThrowIfCategoricalQuestionHasMoreThan200Options(options, linkedToEntityId.HasValue);

            Guid? linkedRosterId;
            Guid? linkedQuestionId;

            this.ExtractLinkedQuestionValues(linkedToEntityId, out linkedQuestionId, out linkedRosterId);


            var question = this.innerDocument.Find<AbstractQuestion>(questionId);
            IQuestion newQuestion = CreateQuestion(questionId,
                QuestionType.MultyOption,
                scope,
                title,
                variableName,
                variableLabel,
                enablementCondition,
                hideIfDisabled,
                null, false, false,
                instructions,
                properties,
                null,
                ConvertOptionsToAnswers(options),
                linkedQuestionId,
                linkedRosterId,
                null,
                null,
                areAnswersOrdered,
                maxAllowedAnswers,
                null, null, null,
                yesNoView,
                validationConditions,
                linkedFilterExpression,
                false);

            this.innerDocument.ReplaceEntity(question, newQuestion);
            
        }

        #region Question: SingleOption command handlers

        public void UpdateSingleOptionQuestion(Guid questionId, string title, string variableName, string variableLabel, bool isPreFilled, QuestionScope scope,
            string enablementCondition, bool hideIfDisabled, string instructions, Guid responsibleId, Option[] options, Guid? linkedToEntityId, bool isFilteredCombobox, 
            Guid? cascadeFromQuestionId, IList<ValidationCondition> validationConditions, string linkedFilterExpression, QuestionProperties properties)
        {
            Answer[] answers;

            if (options == null && (isFilteredCombobox || cascadeFromQuestionId.HasValue))
            {
                IQuestion originalQuestion = this.GetQuestion(questionId);
                answers = originalQuestion.Answers.ToArray();                
            }
            else
            {
                answers = ConvertOptionsToAnswers(options);                
            }

            PrepareGeneralProperties(ref title, ref variableName);
            IGroup parentGroup = this.innerDocument.GetParentById(questionId);
            
            this.ThrowDomainExceptionIfQuestionDoesNotExist(questionId);
            this.ThrowDomainExceptionIfMoreThanOneQuestionExists(questionId);
            this.ThrowDomainExceptionIfGeneralQuestionSettingsAreInvalid(questionId, parentGroup, title, variableName, isPreFilled, QuestionType.SingleOption, responsibleId, validationConditions);

            if (isFilteredCombobox || cascadeFromQuestionId.HasValue)
            {
                var categoricalOneAnswerQuestion = this.innerDocument.Find<SingleQuestion>(questionId);
                answers = categoricalOneAnswerQuestion?.Answers.ToArray();
            }

            this.ThrowIfQuestionIsRosterTitleLinkedCategoricalQuestion(questionId, linkedToEntityId);
            this.ThrowIfCategoricalQuestionIsInvalid(questionId, options, linkedToEntityId, isPreFilled, isFilteredCombobox, scope, cascadeFromQuestionId);
            this.ThrowIfCascadingQuestionHasConditionOrValidation(questionId, cascadeFromQuestionId, validationConditions, enablementCondition);

            if (!isFilteredCombobox && !cascadeFromQuestionId.HasValue)
            {
                this.ThrowIfCategoricalQuestionHasMoreThan200Options(options, linkedToEntityId.HasValue);
            }
            Guid? linkedRosterId;
            Guid? linkedQuestionId;

            this.ExtractLinkedQuestionValues(linkedToEntityId, out linkedQuestionId, out linkedRosterId);

            var question = this.innerDocument.Find<AbstractQuestion>(questionId);
            IQuestion newQuestion = CreateQuestion(questionId,
                QuestionType.SingleOption,
                scope,
                title,
                variableName,
                variableLabel,
                enablementCondition,
                hideIfDisabled,
                null,
                isPreFilled,
                false,
                instructions,
                properties,
                null,
                answers,
                linkedQuestionId,
                linkedRosterId,
                null, null, null, null, null,
                isFilteredCombobox,
                cascadeFromQuestionId,
                null,
                validationConditions,
                linkedFilterExpression,
                false);

            this.innerDocument.ReplaceEntity(question, newQuestion);
        }

        private void  ExtractLinkedQuestionValues(Guid? linkedToEntityId, out Guid? linkedQuestionId, out Guid? linkedRosterId)
        {
            linkedQuestionId = linkedToEntityId.HasValue
                ? (this.innerDocument.FirstOrDefault<IQuestion>(q => q.PublicKey == linkedToEntityId.Value) == null
                    ? (Guid?) null
                    : linkedToEntityId.Value)
                : null;

            linkedRosterId = linkedToEntityId.HasValue && !linkedQuestionId.HasValue
                ? linkedToEntityId.Value
                : (Guid?) null;
        }

        public void UpdateFilteredComboboxOptions(Guid questionId, Guid responsibleId, Option[] options)
        {
            this.ThrowDomainExceptionIfViewerDoesNotHavePermissionsForEditQuestionnaire(responsibleId);
            this.ThrowDomainExceptionIfFilteredComboboxIsInvalid(questionId, options);
            ThrowIfNotLinkedCategoricalQuestionIsInvalid(options);

            var categoricalOneAnswerQuestion = this.innerDocument.Find<SingleQuestion>(questionId);
            IQuestion newQuestion = CreateQuestion(questionId,
                categoricalOneAnswerQuestion.QuestionType,
                categoricalOneAnswerQuestion.QuestionScope,
                categoricalOneAnswerQuestion.QuestionText,
                categoricalOneAnswerQuestion.StataExportCaption,
                categoricalOneAnswerQuestion.VariableLabel,
                categoricalOneAnswerQuestion.ConditionExpression,
                categoricalOneAnswerQuestion.HideIfDisabled,
                null,
                categoricalOneAnswerQuestion.Featured,
                false,
                categoricalOneAnswerQuestion.Instructions,
                categoricalOneAnswerQuestion.Properties,
                null,
                ConvertOptionsToAnswers(options),
                categoricalOneAnswerQuestion.LinkedToQuestionId,
                categoricalOneAnswerQuestion.LinkedToRosterId,
                null, null, null, null, null,
                categoricalOneAnswerQuestion.IsFilteredCombobox,
                categoricalOneAnswerQuestion.CascadeFromQuestionId,
                null,
                categoricalOneAnswerQuestion.ValidationConditions,
                null,
                false);

            this.innerDocument.ReplaceEntity(categoricalOneAnswerQuestion, newQuestion);
        }

        public void UpdateCascadingComboboxOptions(Guid questionId, Guid responsibleId, Option[] options)
        {
            ThrowDomainExceptionIfViewerDoesNotHavePermissionsForEditQuestionnaire(responsibleId);
            ThrowDomainExceptionIfCascadingComboboxIsInvalid(questionId, options);
            ThrowIfNotLinkedCategoricalQuestionIsInvalid(options, isCascade: true);

            ThrowDomainExceptionIfOptionsHasEmptyParentValue(options);

            ThrowDomainExceptionIfOptionsHasNotDecimalParentValue(options);

            ThrowDomainExceptionIfOptionsHasNotUniqueTitleAndParentValuePair(options);

            var categoricalOneAnswerQuestion = this.innerDocument.Find<SingleQuestion>(questionId);

            var question = this.innerDocument.Find<AbstractQuestion>(questionId);
            IQuestion newQuestion = CreateQuestion(
                questionId,
                categoricalOneAnswerQuestion.QuestionType,
                categoricalOneAnswerQuestion.QuestionScope,
                categoricalOneAnswerQuestion.QuestionText,
                categoricalOneAnswerQuestion.StataExportCaption,
                categoricalOneAnswerQuestion.VariableLabel,
                categoricalOneAnswerQuestion.ConditionExpression,
                categoricalOneAnswerQuestion.HideIfDisabled,
                null,
                categoricalOneAnswerQuestion.Featured,
                false,
                categoricalOneAnswerQuestion.Instructions,
                categoricalOneAnswerQuestion.Properties,
                null,
                ConvertOptionsToAnswers(options),
                categoricalOneAnswerQuestion.LinkedToQuestionId,
                categoricalOneAnswerQuestion.LinkedToRosterId,
                null, null, null, null, null,
                categoricalOneAnswerQuestion.IsFilteredCombobox,
                categoricalOneAnswerQuestion.CascadeFromQuestionId,
                null,
                categoricalOneAnswerQuestion.ValidationConditions,
                null,
                false);

            this.innerDocument.ReplaceEntity(question, newQuestion);
        }
        #endregion


        public void UpdateNumericQuestion(UpdateNumericQuestion command)
        {
            var title = command.Title;
            var variableName = command.VariableName;

            PrepareGeneralProperties(ref title, ref variableName);

            this.ThrowDomainExceptionIfQuestionDoesNotExist(command.QuestionId);
            this.ThrowDomainExceptionIfMoreThanOneQuestionExists(command.QuestionId);

            IGroup parentGroup = this.innerDocument.GetParentById(command.QuestionId);

            this.ThrowDomainExceptionIfGeneralQuestionSettingsAreInvalid(command.QuestionId, parentGroup, title, variableName, command.IsPreFilled, QuestionType.Numeric, command.ResponsibleId, command.ValidationConditions);

            this.ThrowIfPrecisionSettingsAreInConflictWithDecimalPlaces(command.IsInteger, command.CountOfDecimalPlaces);
            this.ThrowIfDecimalPlacesValueIsIncorrect(command.CountOfDecimalPlaces);

            var question = this.innerDocument.Find<AbstractQuestion>(command.QuestionId);
            IQuestion newQuestion = CreateQuestion(
                question.PublicKey,
                QuestionType.Numeric,
                command.Scope,
                title,
                variableName,
                command.VariableLabel,
                command.EnablementCondition,
                command.HideIfDisabled,
                Order.AZ,
                command.IsPreFilled,
                false,
                command.Instructions,
                command.Properties,
                null,
                null,
                null,
                null,
                command.IsInteger,
                command.CountOfDecimalPlaces,
                null,
                null,
                null,
                null,
                null,
                null,
                command.ValidationConditions,
                null,
                false);

            this.innerDocument.ReplaceEntity(question, newQuestion);
        }

        public void UpdateTextListQuestion(UpdateTextListQuestion command)
        {
            var title = command.Title;
            var variableName = command.VariableName;

            PrepareGeneralProperties(ref title, ref variableName);

            this.ThrowDomainExceptionIfQuestionDoesNotExist(command.QuestionId);
            this.ThrowDomainExceptionIfMoreThanOneQuestionExists(command.QuestionId);

            var isPrefilled = false;

            IGroup parentGroup = this.innerDocument.GetParentById(command.QuestionId);

            this.ThrowDomainExceptionIfGeneralQuestionSettingsAreInvalid(command.QuestionId, parentGroup, title, variableName, isPrefilled, 
                QuestionType.TextList, command.ResponsibleId, command.ValidationConditions);
            
            var question = this.innerDocument.Find<AbstractQuestion>(command.QuestionId);
            IQuestion newQuestion = CreateQuestion(
                    command.QuestionId,
                    QuestionType.TextList,
                    command.Scope,
                    command.Title,
                    command.VariableName,
                    command.VariableLabel,
                    command.EnablementCondition,
                    command.HideIfDisabled,
                    Order.AZ,
                    false,
                    false,
                    command.Instructions,
                    command.Properties,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    command.MaxAnswerCount,
                    null,
                    null,
                    null,
                    command.ValidationConditions,
                    null,
                    false);

            if (question != null)
            {
                this.innerDocument.ReplaceEntity(question, newQuestion);
            }
        }

        public void UpdateMultimediaQuestion(Guid questionId, string title, string variableName, string variableLabel, string enablementCondition, bool hideIfDisabled, 
            string instructions, Guid responsibleId, QuestionScope scope, QuestionProperties properties)
        {
            PrepareGeneralProperties(ref title, ref variableName);

            this.ThrowDomainExceptionIfQuestionDoesNotExist(questionId);
            this.ThrowDomainExceptionIfMoreThanOneQuestionExists(questionId);

            var isPrefilled = false;
            IGroup parentGroup = this.innerDocument.GetParentById(questionId);

            this.ThrowDomainExceptionIfGeneralQuestionSettingsAreInvalid(questionId, parentGroup, title, variableName, isPrefilled, QuestionType.Multimedia, responsibleId, null);

            var question = this.innerDocument.Find<AbstractQuestion>(questionId);
            IQuestion newQuestion = CreateQuestion(
                    questionId,
                    QuestionType.Multimedia,
                    scope,
                    title,
                    variableName,
                    variableLabel,
                    enablementCondition,
                    hideIfDisabled,
                    Order.AZ,
                    false,
                    false,
                    instructions,
                    properties,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    new List<ValidationCondition>(),
                    null,
                    false);

            if (question != null)
            {
                this.innerDocument.ReplaceEntity(question, newQuestion);
            }
        }

        public void UpdateQRBarcodeQuestion(UpdateQRBarcodeQuestion command)
        {
            var title = command.Title;
            var variableName = command.VariableName;
            PrepareGeneralProperties(ref title, ref variableName);

            this.ThrowDomainExceptionIfQuestionDoesNotExist(command.QuestionId);
            this.ThrowDomainExceptionIfMoreThanOneQuestionExists(command.QuestionId);

            var isPrefilled = false;
            IGroup parentGroup = this.innerDocument.GetParentById(command.QuestionId);

            this.ThrowDomainExceptionIfGeneralQuestionSettingsAreInvalid(command.QuestionId, parentGroup, title, variableName, isPrefilled, QuestionType.QRBarcode, 
                command.ResponsibleId, command.ValidationConditions);
            
            var question = this.innerDocument.Find<AbstractQuestion>(command.QuestionId);
            IQuestion newQuestion = CreateQuestion(
                    command.QuestionId,
                    QuestionType.QRBarcode,
                    command.Scope,
                    command.Title,
                    command.VariableName,
                    command.VariableLabel,
                    command.EnablementCondition,
                    command.HideIfDisabled,
                    Order.AZ,
                    false,
                    false,
                    command.Instructions,
                    command.Properties,
                    null, null, null, null, null, null, null, null, null,
                    null,null,null,
                    command.ValidationConditions,
                    null,
                    false);

            if (question != null)
            {
                this.innerDocument.ReplaceEntity(question, newQuestion);
            }
        }

        #region Static text command handlers
        public void AddStaticTextAndMoveIfNeeded(AddStaticText command)
        {
            this.ThrowDomainExceptionIfViewerDoesNotHavePermissionsForEditQuestionnaire(command.ResponsibleId);

            this.ThrowDomainExceptionIfEntityAlreadyExists(command.EntityId);
            this.ThrowDomainExceptionIfGroupDoesNotExist(command.ParentId);
            this.ThrowDomainExceptionIfStaticTextIsEmpty(command.Text);
            
            this.ThrowIfChapterHasMoreThanAllowedLimit(command.ParentId);

            var staticText = new StaticText(publicKey: command.EntityId,
                text: System.Web.HttpUtility.HtmlDecode(command.Text),
                enablementCondition: null,
                hideIfDisabled: false,
                validationConditions: null,
                attachmentName: null);

            this.innerDocument.Add(staticText, command.ParentId);

            if (command.Index.HasValue)
            {
                this.innerDocument.MoveItem(command.EntityId, command.ParentId, command.Index.Value);
            }
        }

        public void UpdateStaticText(UpdateStaticText command)
        {
            this.ThrowDomainExceptionIfViewerDoesNotHavePermissionsForEditQuestionnaire(command.ResponsibleId);
            
            this.ThrowDomainExceptionIfEntityDoesNotExists(command.EntityId);
            this.ThrowDomainExceptionIfStaticTextIsEmpty(command.Text);

            var oldStaticText = this.innerDocument.Find<IStaticText>(command.EntityId);
            var newStaticText = new StaticText(publicKey: command.EntityId,
                text: System.Web.HttpUtility.HtmlDecode(command.Text),
                enablementCondition: command.EnablementCondition,
                hideIfDisabled: command.HideIfDisabled,
                validationConditions: command.ValidationConditions,
                attachmentName: command.AttachmentName);

            this.innerDocument.ReplaceEntity(oldStaticText, newStaticText);
        }

        public void DeleteStaticText(Guid entityId, Guid responsibleId)
        {
            this.ThrowDomainExceptionIfViewerDoesNotHavePermissionsForEditQuestionnaire(responsibleId);
            this.ThrowDomainExceptionIfEntityDoesNotExists(entityId);

            this.innerDocument.RemoveEntity(entityId);
        }

        public void MoveStaticText(Guid entityId, Guid targetEntityId, int targetIndex, Guid responsibleId)
        {
            this.ThrowDomainExceptionIfViewerDoesNotHavePermissionsForEditQuestionnaire(responsibleId);
            this.ThrowDomainExceptionIfEntityDoesNotExists(entityId);
            this.ThrowDomainExceptionIfGroupDoesNotExist(targetEntityId);
            this.ThrowIfChapterHasMoreThanAllowedLimit(targetEntityId);

            // if we don't have a target group we would like to move source group into root of questionnaire
            var targetGroup = this.GetGroupById(targetEntityId);
            var sourceStaticText = this.innerDocument.Find<IStaticText>(entityId);
            this.ThrowIfTargetIndexIsNotAcceptable(targetIndex, targetGroup, sourceStaticText.GetParent() as IGroup);

            this.innerDocument.MoveItem(entityId, targetEntityId, targetIndex);
        }
        #endregion


        #region Variable command handlers
        public void AddVariableAndMoveIfNeeded(AddVariable command)
        {
            this.ThrowDomainExceptionIfViewerDoesNotHavePermissionsForEditQuestionnaire(command.ResponsibleId);

            this.ThrowDomainExceptionIfEntityAlreadyExists(command.EntityId);
            this.ThrowDomainExceptionIfGroupDoesNotExist(command.ParentId);
            this.ThrowDomainExceptionIfVariableNameIsInvalid(command.EntityId, command.VariableData.Name, DefaultVariableLengthLimit);
            this.ThrowDomainExceptionIfVariableLabelContainsSubstitution(command.VariableData.Label);

            this.ThrowIfChapterHasMoreThanAllowedLimit(command.ParentId);

            var variable = new Variable(command.EntityId, command.VariableData);

            this.innerDocument.Add(variable, command.ParentId);
            
            if (command.Index.HasValue)
            {
                this.innerDocument.MoveItem(command.EntityId, command.ParentId, command.Index.Value);
            }
        }

        public void UpdateVariable(UpdateVariable command)
        {
            this.ThrowDomainExceptionIfViewerDoesNotHavePermissionsForEditQuestionnaire(command.ResponsibleId);
            
            this.ThrowDomainExceptionIfEntityDoesNotExists(command.EntityId);
            this.ThrowDomainExceptionIfVariableNameIsInvalid(command.EntityId, command.VariableData.Name, DefaultVariableLengthLimit);
            this.ThrowDomainExceptionIfVariableLabelContainsSubstitution(command.VariableData.Label);

            var oldVariable = this.innerDocument.Find<IVariable>(command.EntityId);

            var newVariable = new Variable(command.EntityId, command.VariableData);
            this.innerDocument.ReplaceEntity(oldVariable, newVariable);
        }

        public void DeleteVariable(Guid entityId, Guid responsibleId)
        {
            this.ThrowDomainExceptionIfViewerDoesNotHavePermissionsForEditQuestionnaire(responsibleId);
            this.ThrowDomainExceptionIfEntityDoesNotExists(entityId);

            this.innerDocument.RemoveEntity(entityId);
        }

        public void MoveVariable(Guid entityId, Guid targetEntityId, int targetIndex, Guid responsibleId)
        {
            this.ThrowDomainExceptionIfViewerDoesNotHavePermissionsForEditQuestionnaire(responsibleId);
            this.ThrowDomainExceptionIfEntityDoesNotExists(entityId);
            this.ThrowDomainExceptionIfGroupDoesNotExist(targetEntityId);
            this.ThrowIfChapterHasMoreThanAllowedLimit(targetEntityId);

            // if we don't have a target group we would like to move source group into root of questionnaire
            var targetGroup = this.GetGroupById(targetEntityId);
            var sourceVariable = this.innerDocument.Find<IVariable>(entityId);
            this.ThrowIfTargetIndexIsNotAcceptable(targetIndex, targetGroup, sourceVariable.GetParent() as IGroup);

            this.innerDocument.MoveItem(entityId, targetEntityId, targetIndex);
        }
        #endregion

        #region Shared Person command handlers

        public void AddSharedPerson(Guid personId, string email, ShareType shareType, Guid responsibleId)
        {
            this.ThrowDomainExceptionIfViewerDoesNotHavePermissionsForEditQuestionnaire(responsibleId);

            if (this.innerDocument.CreatedBy == personId)
            {
                throw new QuestionnaireException(
                    DomainExceptionType.OwnerCannotBeInShareList,
                    string.Format("User {0} is an owner of this questionnaire. Please, input another email.", email));
            }

            if (this.SharedUsersIds.Contains(personId))
            {
                throw new QuestionnaireException(
                    DomainExceptionType.UserExistInShareList,
                    string.Format("User {0} already exist in share list.", email));
            }

            this.sharedPersons.Add(new SharedPerson()
            {
                UserId = personId,
                ShareType = shareType,
                Email = email,
            });
        }

        public void RemoveSharedPerson(Guid personId, string email, Guid responsibleId)
        {
            this.ThrowDomainExceptionIfViewerDoesNotHavePermissionsForEditQuestionnaire(responsibleId);

            if (!this.SharedUsersIds.Contains(personId))
            {
                throw new QuestionnaireException(
                    DomainExceptionType.UserDoesNotExistInShareList,
                    "Couldn't remove user, because it doesn't exist in share list");
            }

            this.sharedPersons.RemoveAll(sp => sp.UserId == personId);

        }

        #endregion

        #region CopyPaste command handler
        
        public void PasteAfter(PasteAfter pasteAfter)
        {
            this.ThrowDomainExceptionIfViewerDoesNotHavePermissionsForEditQuestionnaire(pasteAfter.ResponsibleId);
            this.ThrowDomainExceptionIfEntityDoesNotExists(pasteAfter.ItemToPasteAfterId);
            this.ThrowDomainExceptionIfEntityAlreadyExists(pasteAfter.EntityId);
            ThrowDomainExceptionIfEntityDoesNotExists(pasteAfter.SourceDocument, pasteAfter.SourceItemId);
            
            var itemToInsertAfter = this.innerDocument.Find<IComposite>(pasteAfter.ItemToPasteAfterId);
            var targetToPasteIn = itemToInsertAfter.GetParent();
            var entityToInsert = pasteAfter.SourceDocument.Find<IComposite>(pasteAfter.SourceItemId);
            var targetIndex = targetToPasteIn.Children.IndexOf(itemToInsertAfter) + 1;

            this.CheckDepthInvariants(targetToPasteIn, entityToInsert);

            this.CopyElementInTree(pasteAfter.EntityId, entityToInsert, targetToPasteIn, targetIndex);
        }

        public void PasteInto(PasteInto pasteInto)
        {
            this.ThrowDomainExceptionIfViewerDoesNotHavePermissionsForEditQuestionnaire(pasteInto.ResponsibleId);
            this.ThrowDomainExceptionIfEntityAlreadyExists(pasteInto.EntityId);
            ThrowDomainExceptionIfEntityDoesNotExists(pasteInto.SourceDocument, pasteInto.SourceItemId);
            
            this.ThrowDomainExceptionIfGroupDoesNotExist(pasteInto.ParentId);

            var entityToInsert = pasteInto.SourceDocument.Find<IComposite>(pasteInto.SourceItemId);
            var targetToPasteIn = this.GetGroupById(pasteInto.ParentId);
            var targetIndex = targetToPasteIn.Children.Count();

            this.CheckDepthInvariants(targetToPasteIn, entityToInsert);

            this.CopyElementInTree(pasteInto.EntityId, entityToInsert, targetToPasteIn, targetIndex);
        }

        private void CheckDepthInvariants(IComposite targetToPasteIn, IComposite entityToInsert)
        {
            if (targetToPasteIn.PublicKey != this.Id)
            {
                var targetChapter = this.innerDocument.GetChapterOfItemById(targetToPasteIn.PublicKey);

                var numberOfMovedItems = entityToInsert.Children
                    .TreeToEnumerable(x => x.Children)
                    .Count();

                var numberOfItemsInChapter = targetChapter.Children
                    .TreeToEnumerable(x => x.Children)
                    .Count();

                if ((numberOfMovedItems + numberOfItemsInChapter) >= MaxChapterItemsCount - 1)
                {
                    throw new QuestionnaireException(string.Format("Section cannot have more than {0} elements",
                        MaxChapterItemsCount));
                }

                var targetGroupDepthLevel = this.GetAllParentGroups(this.GetGroupById(targetToPasteIn.PublicKey)).Count();

                var entityToInsertAsGroup = entityToInsert as IGroup;
                if (entityToInsertAsGroup != null)
                {
                    var sourceGroupMaxChildNestingDepth = GetMaxChildGroupNestingDepth(entityToInsertAsGroup);

                    if ((targetGroupDepthLevel + sourceGroupMaxChildNestingDepth) > MaxGroupDepth)
                    {
                        throw new QuestionnaireException(string.Format("Sub-section or roster depth cannot be higher than {0}",
                            MaxGroupDepth));
                    }
                }
            }
        }

        internal void CopyElementInTree(Guid pasteItemId, IComposite entityToInsert, IComposite targetToPasteIn, int targetIndex)
        {
            var entityToInsertAsQuestion = entityToInsert as IQuestion;
            if (entityToInsertAsQuestion != null)
            {
                if (targetToPasteIn.PublicKey == this.Id)
                    throw new QuestionnaireException(string.Format("Question cannot be pasted here."));

                var question = (AbstractQuestion)entityToInsertAsQuestion.Clone();
                question.PublicKey = pasteItemId;
                this.innerDocument.Insert(targetIndex, question, targetToPasteIn.PublicKey);

                if (entityToInsertAsQuestion.Capital)
                    this.innerDocument.MoveHeadQuestionPropertiesToRoster(pasteItemId, targetToPasteIn.PublicKey);

                return;
            }

            var entityToInsertAsStaticText = entityToInsert as IStaticText;
            if (entityToInsertAsStaticText != null)
            {
                if (targetToPasteIn.PublicKey == this.Id)
                    throw new QuestionnaireException(string.Format("Static Text cannot be pasted here."));

                var staticText = (StaticText)entityToInsertAsStaticText.Clone();
                staticText.PublicKey = pasteItemId;
                this.innerDocument.Insert(targetIndex, staticText, targetToPasteIn.PublicKey);
                return;
            }

            var entityToInsertAsGroup = entityToInsert as IGroup;
            if (entityToInsertAsGroup != null)
            {
                //roster as chapter is forbidden
                if (entityToInsertAsGroup.IsRoster && (targetToPasteIn.PublicKey == this.Id))
                    throw new QuestionnaireException(string.Format("Roster cannot be pasted here."));

                //roster, group, chapter
                Dictionary<Guid, Guid> replacementIdDictionary = (entityToInsert).TreeToEnumerable(x => x.Children).ToDictionary(y => y.PublicKey, y => Guid.NewGuid());
                replacementIdDictionary[entityToInsert.PublicKey] = pasteItemId;

                var clonedGroup = entityToInsertAsGroup.Clone();
                clonedGroup.TreeToEnumerable(x => x.Children).ForEach(c =>
                {
                    TypeSwitch.Do(c,
                        TypeSwitch.Case<Group>(g =>
                        {
                            g.PublicKey = replacementIdDictionary[g.PublicKey];
                            g.RosterSizeQuestionId = GetIdOrReturnSameId(replacementIdDictionary, g.RosterSizeQuestionId);
                            g.RosterTitleQuestionId = GetIdOrReturnSameId(replacementIdDictionary, g.RosterTitleQuestionId);
                        }),
                        TypeSwitch.Case<IQuestion>(q =>
                        {
                            ((AbstractQuestion)q).PublicKey = replacementIdDictionary[q.PublicKey];
                            q.CascadeFromQuestionId = GetIdOrReturnSameId(replacementIdDictionary, q.CascadeFromQuestionId);
                            q.LinkedToQuestionId = GetIdOrReturnSameId(replacementIdDictionary, q.LinkedToQuestionId);
                            q.LinkedToRosterId = GetIdOrReturnSameId(replacementIdDictionary, q.LinkedToRosterId);
                        }),
                        TypeSwitch.Case<Variable>(v => v.PublicKey = replacementIdDictionary[v.PublicKey]),
                        TypeSwitch.Case<StaticText>(st => st.PublicKey = replacementIdDictionary[st.PublicKey])
                    );
                });
                this.innerDocument.Insert(targetIndex, clonedGroup, targetToPasteIn.PublicKey);
                return;
            }

            var entityToInsertAsVariable = entityToInsert as IVariable;
            if (entityToInsertAsVariable != null)
            {
                if (targetToPasteIn.PublicKey == this.Id)
                    throw new QuestionnaireException(string.Format("Variable cannot be pasted here."));

                var variable = (Variable)entityToInsertAsVariable.Clone();
                variable.PublicKey = pasteItemId;
                this.innerDocument.Insert(targetIndex, variable, targetToPasteIn.PublicKey);

                return;
            }

            throw new QuestionnaireException(string.Format("Unknown item type. Paste failed."));
        }

        #endregion

        #region Questionnaire Invariants

        private void ThrowIfQuestionIsUsedAsCascadingParent(Guid questionId)
        {
            var usedInCascades = this.innerDocument.Find<SingleQuestion>(x => x.CascadeFromQuestionId == questionId).Any();
            if (usedInCascades)
            {
                throw new QuestionnaireException(ExceptionMessages.CantRemoveParentQuestionInCascading);
            }
        }

        private void ThrowDomainExceptionIfGeneralQuestionSettingsAreInvalid(Guid questionId, IGroup parentGroup, string title, string variableName, bool isPrefilled, QuestionType questionType, Guid responsibleId, IList<ValidationCondition> validationCoditions)
        {
            this.ThrowDomainExceptionIfViewerDoesNotHavePermissionsForEditQuestionnaire(responsibleId);
            this.ThrowDomainExceptionIfTitleIsEmptyOrTooLong(title);

            int variableLengthLimit = RestrictedVariableLengthQuestionTypes.Contains(questionType)
                ? DefaultRestrictedVariableLengthLimit
                : DefaultVariableLengthLimit;

            this.ThrowDomainExceptionIfVariableNameIsInvalid(questionId, variableName, variableLengthLimit);

            this.ThrowDomainExceptionIfQuestionTitleContainsIncorrectSubstitution(title, variableName, questionId, isPrefilled, parentGroup);
            
            this.ThrowDomainExceptionIfQuestionIsPrefilledAndParentGroupIsRoster(isPrefilled, parentGroup);

            if (parentGroup != null)
            {
                this.ThrowIfChapterHasMoreThanAllowedLimit(parentGroup.PublicKey);
            }

            if (validationCoditions != null)
            {
                foreach (var validationCondition in validationCoditions)
                {
                    this.ThrowDomainExceptionIfTextContainsIncorrectSubstitution(
                        text: validationCondition.Message,
                        entityId: questionId,
                        variableName: variableName,
                        parentGroup: parentGroup);
                }
            }
        }

        private void ThrowIfChapterHasMoreThanAllowedLimit(Guid itemId)
        {
            var chapter = this.innerDocument.GetChapterOfItemById(itemId);
            if (chapter.Children.TreeToEnumerable(x => x.Children).Count() >= MaxChapterItemsCount)
            {
                throw new QuestionnaireException(string.Format("Section cannot have more than {0} child items", MaxChapterItemsCount));
            }
        }

        private void ThrowIfTargetGroupHasReachedAllowedDepthLimit(Guid itemId)
        {
            
            var entity = innerDocument.Find<IComposite>(itemId);
            if (entity != null)
            {
                var targetGroupDepth = this.GetAllParentGroups(entity).Count();

                if ((targetGroupDepth) >= MaxGroupDepth)
                {
                    throw new QuestionnaireException(string.Format("Sub-section or roster  depth cannot be higher than {0}",
                        MaxGroupDepth));
                }
            }
        }

        private void ThrowIfGroupMovedFromRosterToGroupAndContainsRosterTitleInSubstitution(IGroup sourceGroup, IGroup targetGroup)
        {
            if (this.IsRosterOrInsideRoster(targetGroup) || sourceGroup.IsRoster)
                return;

            this.ThrowIfRosterCantBecomeAGroupBecauseOfReferencesOnRosterTitleInSubstitutions(sourceGroup);
        }

        private void ThrowIfTargetIndexIsNotAcceptable(int targetIndex, IGroup targetGroup, IGroup parentGroup)
        {
            var maxAcceptableIndex = targetGroup.Children.Count;
            if (parentGroup != null && targetGroup.PublicKey == parentGroup.PublicKey)
                maxAcceptableIndex--;

            if (targetIndex < 0 || maxAcceptableIndex < targetIndex)
                throw new QuestionnaireException(
                   string.Format(
                       "You can't move to sub-section {0} because it position {1} in not acceptable",
                       FormatGroupForException(targetGroup.PublicKey, this.innerDocument), targetIndex));
        }

        private void ThrowIfGroupFromRosterThatContainsLinkedSourceQuestionsMovedToGroup(IGroup sourceGroup, IGroup targetGroup)
        {
            if (this.IsRosterOrInsideRoster(targetGroup)) return;

            var allQuestionsIdsFromGroup = this.GetAllQuestionsInGroup(sourceGroup).Select(question => question.PublicKey);

            var linkedQuestionSourcesInGroup = this.GetAllLinkedSourceQuestions().Intersect(allQuestionsIdsFromGroup);

            if (linkedQuestionSourcesInGroup.Any())
            {
                throw new QuestionnaireException(
                    string.Format(
                        "You can't move sub-section {0} to another sub-section because it contains linked source question(s): {1}",
                        FormatGroupForException(sourceGroup.PublicKey, this.innerDocument),
                        string.Join(Environment.NewLine,
                            linkedQuestionSourcesInGroup.Select(
                                questionId => this.FormatQuestionForException(questionId, this.innerDocument)))));
            }
        }

        private void ThrowIfSourceGroupContainsInvalidRosterSizeQuestions(IGroup sourceGroup, IGroup targetGroup)
        {
            var allQuestionsIdsFromGroup = this.GetAllQuestionsInGroup(sourceGroup).Select(question => question.PublicKey);

            var rosterSizeQuestionsInGroup = this.GetAllRosterSizeQuestionIds().Intersect(allQuestionsIdsFromGroup);
            var rosterSizeQuestionsOfTargetGroupAndUpperRosters = GetRosterSizeQuestionsOfGroupAndUpperRosters(targetGroup);

            if (rosterSizeQuestionsOfTargetGroupAndUpperRosters.Intersect(rosterSizeQuestionsInGroup).Any())
            {
                throw new QuestionnaireException(
                    string.Format(
                        "You can't move sub-section {0} to roster {1} because it contains roster source question(s): {2}",
                        FormatGroupForException(sourceGroup.PublicKey, this.innerDocument),
                        FormatGroupForException(targetGroup.PublicKey, this.innerDocument),
                        string.Join(Environment.NewLine,
                            rosterSizeQuestionsInGroup.Select(questionId => this.FormatQuestionForException(questionId, this.innerDocument)))));
            }
        }

        private void ThrowIfGroupFromRosterThatContainsRosterTitleQuestionMovedToAnotherGroup(IGroup sourceGroup, IGroup targetGroup)
        {
            if (!IsRosterOrInsideRoster(sourceGroup) && !ContainsRoster(sourceGroup)) return;

            IEnumerable<IQuestion> groupQuestions = GetAllQuestionsInGroup(sourceGroup);

            var rosterTitleQuestionsWithDependentGroups =
                from question in groupQuestions
                let groupsWhereQuestionIsRosterTitle = this.GetGroupsByRosterTitleId(question.PublicKey, sourceGroup.PublicKey)
                where groupsWhereQuestionIsRosterTitle.Any()
                select new { RostertTitleQuestion = question, DependentGroups = groupsWhereQuestionIsRosterTitle };

            if (rosterTitleQuestionsWithDependentGroups.All(question => !question.DependentGroups.Any())) return;

            Func<Guid, IEnumerable<IGroup>, string> getWarningMessage = (rosterTitleQuestionId, invalidGroups) =>
            {
                return string.Format("Question {0} used as roster title question in sub-section(s):{1}{2}",
                    this.FormatQuestionForException(rosterTitleQuestionId, this.innerDocument), Environment.NewLine,
                    string.Join(Environment.NewLine,
                        invalidGroups.Select(group => FormatGroupForException(@group.PublicKey, this.innerDocument))));
            };

            if (IsRosterOrInsideRoster(targetGroup))
            {
                var rosterForTargetGroup = GetFirstRosterParentGroupOrNull(targetGroup);

                var rosterTitleQuestionsWithDependentGroupsByTargetRosterSizeQuestion =
                    rosterTitleQuestionsWithDependentGroups.Select(
                        question =>
                            new
                            {
                                RostertTitleQuestion = question.RostertTitleQuestion,
                                DependentGroups =
                                    question.DependentGroups.Where(
                                        group => group.RosterSizeQuestionId != rosterForTargetGroup.RosterSizeQuestionId)
                            }).Where(question => question.DependentGroups.Any());

                if (!rosterTitleQuestionsWithDependentGroupsByTargetRosterSizeQuestion.Any()) return;

                var warningsForRosterTitlesNotInRostersByRosterSize =
                    rosterTitleQuestionsWithDependentGroupsByTargetRosterSizeQuestion.Select(
                        x => getWarningMessage(x.RostertTitleQuestion.PublicKey, x.DependentGroups));

                throw new QuestionnaireException(
                    string.Join(
                        string.Format(
                            "Sub-section {0} could not be moved to sub-section {1} because contains some questions that used as roster title questions in groups which have roster size question not the same as have target {1} group: ",
                            FormatGroupForException(sourceGroup.PublicKey, this.innerDocument),
                            FormatGroupForException(targetGroup.PublicKey, this.innerDocument)), Environment.NewLine,
                        string.Join(Environment.NewLine, warningsForRosterTitlesNotInRostersByRosterSize)));
            }
            else
            {
                if (sourceGroup.IsRoster || ContainsRoster(sourceGroup)) return;

                var warningsForRosterTitlesNotInRostersByRosterSize =
                    rosterTitleQuestionsWithDependentGroups.Select(
                        x => getWarningMessage(x.RostertTitleQuestion.PublicKey, x.DependentGroups));

                throw new QuestionnaireException(
                    string.Join(
                        string.Format("Sub-section {0} could not be moved to sub-section {1} because: ",
                            FormatGroupForException(sourceGroup.PublicKey, this.innerDocument),
                            FormatGroupForException(targetGroup.PublicKey, this.innerDocument)),
                        Environment.NewLine,
                        string.Join(Environment.NewLine, warningsForRosterTitlesNotInRostersByRosterSize)));
            }
        }

        private void ThrowDomainExceptionIfQuestionIsPrefilledAndParentGroupIsRoster(bool isPrefilled, IGroup parentGroup)
        {
            if (isPrefilled && IsRosterOrInsideRoster(parentGroup))
                throw new QuestionnaireException("Question inside roster sub-section can not be pre-filled.");
        }

        private void ThrowDomainExceptionIfGroupTitleIsEmptyOrWhitespacesOrTooLong(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new QuestionnaireException(
                    DomainExceptionType.GroupTitleRequired,
                    "The titles of sections and sub-sections can not be empty or contain whitespace only");
            }

            if (title.Length > MaxTitleLength)
            {
                throw new QuestionnaireException(
                    DomainExceptionType.TitleIsTooLarge,
                    string.Format("The titles of sections and sub-sections can't have more than {0} symbols", MaxTitleLength));
            }
        }

        private void ThrowDomainExceptionIfQuestionnaireTitleIsEmptyOrWhitespacesOrTooLong(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new QuestionnaireException(
                    DomainExceptionType.QuestionnaireTitleRequired,
                    "Questionnaire's title can not be empty or contains whitespace only");
            }
            if (title.Length > MaxTitleLength)
            {
                throw new QuestionnaireException(
                    DomainExceptionType.TitleIsTooLarge, 
                    string.Format("Questionnaire's title can't have more than {0} symbols", MaxTitleLength));
            }
        }

        private static void ThrowDomainExceptionIfEntityDoesNotExists(QuestionnaireDocument doc, Guid entityId)
        {
            var entity = doc.Find<IComposite>(entityId);
            if (entity == null)
            {
                throw new QuestionnaireException(
                    DomainExceptionType.EntityNotFound,
                    string.Format("Questionnaire item with id {0} can't be found", entityId));
            }
        }

        private void ThrowDomainExceptionIfEntityDoesNotExists(Guid entityId)
        {
            ThrowDomainExceptionIfEntityDoesNotExists(this.innerDocument, entityId);
        }

        private void ThrowDomainExceptionIfQuestionDoesNotExist(Guid publicKey)
        {
            var question = this.innerDocument.Find<AbstractQuestion>(publicKey);
            if (question == null)
            {
                throw new QuestionnaireException(
                    DomainExceptionType.QuestionNotFound,
                    string.Format("Question with public key {0} can't be found", publicKey));
            }
        }

        private void ThrowDomainExceptionIfGroupDoesNotExist(Guid groupPublicKey)
        {
            var group = this.innerDocument.Find<Group>(groupPublicKey);
            if (group == null)
            {
                throw new QuestionnaireException(
                    DomainExceptionType.GroupNotFound,
                    string.Format("sub-section with public key {0} can't be found", groupPublicKey));
            }
        }

        private void ThrowDomainExceptionIfTitleIsEmptyOrTooLong(string title)
        {
            if (string.IsNullOrEmpty(title))
                throw new QuestionnaireException(DomainExceptionType.QuestionTitleRequired, "Question text can't be empty");

            if (title.Length > MaxTitleLength)
            {
                throw new QuestionnaireException(DomainExceptionType.TitleIsTooLarge,
                    string.Format("Question text can't have more than {0} symbols", MaxTitleLength));
            }
        }

        private void ThrowDomainExceptionIfStaticTextIsEmpty(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new QuestionnaireException(DomainExceptionType.StaticTextIsEmpty, "Static text is empty");
        }

        private void ThrowDomainExceptionIfVariableNameIsInvalid(Guid questionPublicKey, string stataCaption, int variableLengthLimit)
        {
            if (string.IsNullOrEmpty(stataCaption))
            {
                return;
            }
            
            bool isTooLong = stataCaption.Length > variableLengthLimit;
            if (isTooLong)
            {
                throw new QuestionnaireException(
                    DomainExceptionType.VariableNameMaxLength, string.Format("This element's name or ID shouldn't be longer than {0} characters.", variableLengthLimit));
            }

            bool containsInvalidCharacters = stataCaption.Any(c => !(c == '_' || Char.IsLetterOrDigit(c)));
            if (containsInvalidCharacters)
            {
                throw new QuestionnaireException(
                    DomainExceptionType.VariableNameSpecialCharacters,
                    "Valid variable or roster ID name should contain only letters, digits and underscore character");
            }

            bool startsWithDigitOrUnderscore = Char.IsDigit(stataCaption[0]) || stataCaption[0] == '_';
            if (startsWithDigitOrUnderscore)
            {
                throw new QuestionnaireException(
                    DomainExceptionType.VariableNameStartWithDigit, "Variable name or roster ID shouldn't start with digit or underscore");
            }

            bool endsWithUnderscore = stataCaption[stataCaption.Length-1] == '_';
            if (endsWithUnderscore)
            {
                throw new QuestionnaireException(
                    DomainExceptionType.VariableNameEndsWithUnderscore, "Variable name or roster ID shouldn't end with underscore");
            }

            bool hasConsecutiveUnderscore = stataCaption.Contains("__");
            if (hasConsecutiveUnderscore)
            {
                throw new QuestionnaireException(
                    DomainExceptionType.VariableNameHasConsecutiveUnderscores, "Variable name or roster ID shouldn't have two and more consecutive underscore characters.");
            }

            var captions = this.innerDocument.GetEntitiesByType<AbstractQuestion>()
                .Where(q => q.PublicKey != questionPublicKey)
                .Select(q => q.StataExportCaption);

            bool isNotUnique = captions.Contains(stataCaption);
            if (isNotUnique)
            {
                throw new QuestionnaireException(
                    DomainExceptionType.VarialbeNameNotUnique, "Variable name or roster ID should be unique in questionnaire's scope");
            }
            
            var keywords = this.variableNameValidator.GetAllReservedKeywords();

            foreach (var keyword in keywords.Where(keyword => stataCaption.ToLower() == keyword)) {
                throw new QuestionnaireException(
                    DomainExceptionType.VariableNameShouldNotMatchWithKeywords,
                    keyword + " is a keyword. Variable name or roster ID shouldn't match with keywords");
            }
        }

        private void ThrowIfCascadingQuestionHasConditionOrValidation(Guid questionId, Guid? cascadeFromQuestionId, IList<ValidationCondition> validationExpression, string enablementCondition)
        {
            if (!cascadeFromQuestionId.HasValue )
            {
                return;
            }

            if (validationExpression.Count > 0)
            {
                throw new QuestionnaireException(ExceptionMessages.CascadingCantHaveValidationExpression);
            }

            if (!string.IsNullOrWhiteSpace(enablementCondition))
            {
                throw new QuestionnaireException(ExceptionMessages.CascadingCantHaveConditionExpression);
            }
        }
        private void ThrowIfCategoricalQuestionIsInvalid(Guid questionId, Option[] options, Guid? linkedToEntityId, bool isFeatured, bool? isFilteredCombobox, QuestionScope scope, Guid? cascadeFromQuestionId)
        {
            bool entityIsLinked = linkedToEntityId.HasValue;
            bool questionHasOptions = options != null && options.Any();

            if (entityIsLinked && questionHasOptions)
            {
                throw new QuestionnaireException(
                    DomainExceptionType.ConflictBetweenLinkedQuestionAndOptions,
                    "Categorical question cannot be with answers and linked to another question in the same time");
            }

            if (cascadeFromQuestionId.HasValue && entityIsLinked)
            {
                throw new QuestionnaireException(ExceptionMessages.CantBeLinkedAndCascadingAtSameTime);
            }

            if (cascadeFromQuestionId.HasValue)
            {
                var cascadefrom = this.innerDocument.Find<IQuestion>(x => x.PublicKey == cascadeFromQuestionId).FirstOrDefault();
                if (cascadefrom == null)
                {
                    throw new QuestionnaireException(ExceptionMessages.ShouldCascadeFromExistingQuestion);
                }
            }

            if (entityIsLinked)
            {
                //this.ThrowIfQuestionIsRosterTitleLinkedCategoricalQuestion(questionId);
                this.ThrowIfLinkedEntityIsInvalid(linkedToEntityId, isFeatured);
                this.ThrowIfLinkedCategoricalQuestionIsNotFilledByInterviewer(scope);
            }
            else if (isFilteredCombobox != true && !(cascadeFromQuestionId.HasValue))
            {
                ThrowIfNotLinkedCategoricalQuestionIsInvalid(options);
            }
        }

        private void ThrowIfCategoricalQuestionHasMoreThan200Options(Option[] options, bool isLinkedQuestion)
        {
            if (!isLinkedQuestion && options.Count() > 200)
            {
                throw new QuestionnaireException(DomainExceptionType.CategoricalQuestionHasMoreThan200Options, ExceptionMessages.CategoricalQuestionHasMoreThan200Options);
            }
        }

        private void ThrowIfQuestionIsRosterTitleLinkedCategoricalQuestion(Guid questionId, Guid? linkedToQuestionId)
        {
            if (!linkedToQuestionId.HasValue) return;

            var rosterTitleQuestionGroups =
                this.innerDocument.Find<IGroup>(
                    group => group.RosterTitleQuestionId.HasValue && group.RosterTitleQuestionId.Value == questionId)
                    .Select(group => group.PublicKey);

            if (rosterTitleQuestionGroups.Any())
            {
                throw new QuestionnaireException(
                    string.Format("Linked categorical multi-select question could not be used as a roster title question in sub-section(s): {0}",
                        string.Join(Environment.NewLine,
                            rosterTitleQuestionGroups.Select(groupId => FormatGroupForException(groupId, this.innerDocument)))));
            }
        }

        private void ThrowIfMaxAllowedAnswersInvalid(QuestionType questionType, Guid? linkedToQuestionId, int? maxAllowedAnswers,
            Option[] options)
        {
            if (!maxAllowedAnswers.HasValue) return;

            if (maxAllowedAnswers.Value < 2)
            {
                throw new QuestionnaireException(
                    DomainExceptionType.MaxAllowedAnswersLessThan2,
                    "Maximum Allowed Answers for question should be more than one");
            }

            if (!linkedToQuestionId.HasValue && maxAllowedAnswers.Value > options.Length)
            {
                throw new QuestionnaireException(
                    DomainExceptionType.MaxAllowedAnswersMoreThanOptions,
                    "Maximum Allowed Answers more than question's options");
            }
        }

        private void ThrowIfLinkedEntityIsInvalid(Guid? linkedToEntityId, bool isPrefilled)
        {
            var linkedToQuestion =
                this.innerDocument.Find<IQuestion>(x => x.PublicKey == linkedToEntityId).FirstOrDefault();

            if (linkedToQuestion == null)
            {
                var linkedToRoster =
                    this.innerDocument.Find<IGroup>(x => x.PublicKey == linkedToEntityId).FirstOrDefault();

                if (linkedToRoster == null)
                    throw new QuestionnaireException(
                        DomainExceptionType.LinkedEntityDoesNotExist,
                        "Entity that you are linked to does not exist");

                if(!linkedToRoster.IsRoster)
                    throw new QuestionnaireException(
                       DomainExceptionType.GroupYouAreLinkedToIsNotRoster,
                       "Group that you are linked to is not a roster");

                return;
            }

            bool typeOfLinkedQuestionIsNotSupported = !(
                linkedToQuestion.QuestionType == QuestionType.DateTime ||
                    linkedToQuestion.QuestionType == QuestionType.Numeric ||
                    linkedToQuestion.QuestionType == QuestionType.TextList ||
                    linkedToQuestion.QuestionType == QuestionType.Text);

            if (typeOfLinkedQuestionIsNotSupported)
            {
                throw new QuestionnaireException(
                    DomainExceptionType.NotSupportedQuestionForLinkedQuestion,
                    "Linked question can be only type of number, text or date");
            }

            if (isPrefilled)
            {
                throw new QuestionnaireException(
                    DomainExceptionType.QuestionWithLinkedQuestionCanNotBeFeatured,
                    "Question that linked to another question can not be pre-filled");
            }
        }

        private void ThrowIfLinkedCategoricalQuestionIsNotFilledByInterviewer(QuestionScope scope)
        {
            if (scope == QuestionScope.Supervisor)
            {
                throw new QuestionnaireException(
                    DomainExceptionType.LinkedCategoricalQuestionCanNotBeFilledBySupervisor,
                    "Linked categorical questions cannot be filled by supervisor");
            }
        }

        private static void ThrowIfNotLinkedCategoricalQuestionIsInvalid(Option[] options, bool isCascade = false)
        {
            if ((options == null || !options.Any() || options.Count() < 2))
            {
                throw new QuestionnaireException(
                    DomainExceptionType.SelectorEmpty, "Question with options should have two options at least");
            }

            if (options.Any(x => string.IsNullOrEmpty(x.Value)))
            {
                throw new QuestionnaireException(
                    DomainExceptionType.SelectorValueRequired, "Answer option value is required");
            }

            var tooLongValues = options.Where(option => option.Value.Length > 16).Select(option => option.Value).ToList();
            if (tooLongValues.Any())
            {
                throw new QuestionnaireException(string.Format("Following option values are too long: {0}", string.Join(", ", tooLongValues)));
            }

            if (options.Any(x => !x.Value.IsDecimal()))
            {
                throw new QuestionnaireException(
                    DomainExceptionType.SelectorValueSpecialCharacters,
                    "Option value should have only number characters");
            }

            if (!AreElementsUnique(options.Select(x => x.Value)))
            {
                throw new QuestionnaireException(
                    DomainExceptionType.SelectorValueNotUnique,
                    "Option values must be unique for categorical question");
            }

            if (options.Any(x => string.IsNullOrEmpty(x.Title)))
            {
                throw new QuestionnaireException(DomainExceptionType.SelectorTextRequired, "Answer title can't be empty");
            }

            if (!isCascade && !AreElementsUnique(options.Select(x => x.Title)))
            {
                throw new QuestionnaireException(DomainExceptionType.SelectorTextNotUnique, "Answer title is not unique");
            }
        }

        private void ThrowDomainExceptionIfEntityAlreadyExists(Guid entityId)
        {
            this.ThrowDomainExceptionIfElementCountIsMoreThanExpected<IComposite>(
                elementId: entityId,
                expectedCount: 0,
                exceptionType: DomainExceptionType.EntityWithSuchIdAlreadyExists,
                getExceptionDescription:
                    elementsWithSameId => string.Format("One or more questionnaire item(s) with same ID {0} already exists",
                        entityId));
        }

        private void ThrowDomainExceptionIfQuestionAlreadyExists(Guid questionId)
        {
            this.ThrowDomainExceptionIfElementCountIsMoreThanExpected<IQuestion>(
                elementId: questionId,
                expectedCount: 0,
                exceptionType: DomainExceptionType.QuestionWithSuchIdAlreadyExists,
                getExceptionDescription:
                    elementsWithSameId => string.Format("One or more question(s) with same ID {0} already exist:{1}{2}",
                        questionId,
                        Environment.NewLine,
                        string.Join(Environment.NewLine, elementsWithSameId.Select(question => question.QuestionText ?? "<untitled>"))));
        }

        private void ThrowDomainExceptionIfGroupAlreadyExists(Guid groupId)
        {
            this.ThrowDomainExceptionIfElementCountIsMoreThanExpected<IGroup>(
                elementId: groupId,
                expectedCount: 0,
                exceptionType: DomainExceptionType.GroupWithSuchIdAlreadyExists,
                getExceptionDescription:
                    elementsWithSameId => string.Format("One or more sub-section(s) with same ID {0} already exist:{1}{2}",
                        groupId,
                        Environment.NewLine,
                        string.Join(Environment.NewLine, elementsWithSameId.Select(group => group.Title ?? "<untitled>"))));
        }

        private void ThrowDomainExceptionIfMoreThanOneEntityExists(Guid entityId)
        {
            this.ThrowDomainExceptionIfElementCountIsMoreThanExpected<IComposite>(
                elementId: entityId,
                expectedCount: 1,
                exceptionType: DomainExceptionType.MoreThanOneEntityWithSuchIdExists,
                getExceptionDescription:
                    elementsWithSameId => string.Format("One or more questionnaire item(s) with same ID {0} already exists",
                        entityId));
        }

        private void ThrowDomainExceptionIfMoreThanOneQuestionExists(Guid questionId)
        {
            this.ThrowDomainExceptionIfElementCountIsMoreThanExpected<IQuestion>(
                elementId: questionId,
                expectedCount: 1,
                exceptionType: DomainExceptionType.MoreThanOneQuestionsWithSuchIdExists,
                getExceptionDescription:
                    elementsWithSameId => string.Format("One or more question(s) with same ID {0} already exist:{1}{2}",
                        questionId,
                        Environment.NewLine,
                        string.Join(Environment.NewLine, elementsWithSameId.Select(question => question.QuestionText ?? "<untitled>"))));
        }

        private void ThrowIfPrecisionSettingsAreInConflictWithDecimalPlaces(bool isInteger, int? countOfDecimalPlaces)
        {
            if (isInteger && countOfDecimalPlaces.HasValue)
            {
                throw new QuestionnaireException(
                    DomainExceptionType.IntegerQuestionCantHaveDecimalPlacesSettings,
                    "Roster size question can't have decimal places settings");
            }
        }

        private void ThrowIfDecimalPlacesValueIsIncorrect(int? countOfDecimalPlaces)
        {
            if (!countOfDecimalPlaces.HasValue)
                return;

            if (countOfDecimalPlaces.Value > MaxCountOfDecimalPlaces)
            {
                throw new QuestionnaireException(
                    DomainExceptionType.CountOfDecimalPlacesValueIsIncorrect,
                    string.Format("Number of decimal places '{0}' exceeded maximum '{1}'.", countOfDecimalPlaces, MaxCountOfDecimalPlaces));
            }

            if (countOfDecimalPlaces.Value < 0)
            {
                throw new QuestionnaireException(
                    DomainExceptionType.CountOfDecimalPlacesValueIsIncorrect,
                    string.Format("Number of decimal places cant be negative."));
            }

            if (countOfDecimalPlaces.Value == 0)
            {
                throw new QuestionnaireException(
                    DomainExceptionType.CountOfDecimalPlacesValueIsIncorrect,
                    string.Format("If you want number of decimal places equals to 0 please select integer."));
            }
        }

        private void ThrowDomainExceptionIfMoreThanOneGroupExists(Guid groupId)
        {
            this.ThrowDomainExceptionIfElementCountIsMoreThanExpected<IGroup>(
                elementId: groupId,
                expectedCount: 1,
                exceptionType: DomainExceptionType.MoreThanOneGroupsWithSuchIdExists,
                getExceptionDescription:
                    elementsWithSameId => string.Format("One or more sub-section(s) with same ID {0} already exist:{1}{2}",
                        groupId,
                        Environment.NewLine,
                        string.Join(Environment.NewLine, elementsWithSameId.Select(group => group.Title ?? "<untitled>"))));
        }

        private void ThrowDomainExceptionIfElementCountIsMoreThanExpected<T>(Guid elementId, int expectedCount,
            DomainExceptionType exceptionType, Func<IEnumerable<T>, string> getExceptionDescription)
            where T : class, IComposite
        {
            this.ThrowDomainExceptionIfElementCountIsMoreThanExpected<T>(x => x.PublicKey == elementId, expectedCount,
                exceptionType, getExceptionDescription);
        }

        private void ThrowDomainExceptionIfElementCountIsMoreThanExpected<T>(Func<T, bool> condition, int expectedCount,
            DomainExceptionType exceptionType, Func<IEnumerable<T>, string> getExceptionDescription)
            where T : class, IComposite
        {
            List<T> elementsWithSameId = this.innerDocument.Find(condition).ToList();

            if (elementsWithSameId.Count > expectedCount)
            {
                throw new QuestionnaireException(exceptionType, getExceptionDescription(elementsWithSameId));
            }
        }

        private void ThrowDomainExceptionIfMacroAlreadyExist(Guid macroId)
        {
            if (this.innerDocument.Macros.ContainsKey(macroId))
            {
                throw new QuestionnaireException(DomainExceptionType.MacroAlreadyExist, ExceptionMessages.MacroAlreadyExist);
            }
        }

        private void ThrowDomainExceptionIfMacroContentIsEmpty(string macroContent)
        {
            if (string.IsNullOrWhiteSpace(macroContent))
            {
                throw new QuestionnaireException(DomainExceptionType.MacroContentIsEmpty, ExceptionMessages.MacroContentIsEmpty);
            }
        }

        private void ThrowDomainExceptionIfMacroIsAbsent(Guid macroId)
        {
            if (!this.innerDocument.Macros.ContainsKey(macroId))
            {
                throw new QuestionnaireException(DomainExceptionType.MacroIsAbsent, ExceptionMessages.MacroIsAbsent);
            }
        }

        private void ThrowDomainExceptionIfViewerDoesNotHavePermissionsForEditQuestionnaire(Guid viewerId) 
        {
            if (this.innerDocument.CreatedBy != viewerId && !this.SharedUsersIds.Contains(viewerId))
            {
                throw new QuestionnaireException(
                    DomainExceptionType.DoesNotHavePermissionsForEdit,
                    "You don't have permissions for changing this questionnaire");
            }
            if (this.ReadOnlyUsersIds.Contains(viewerId))
            {
                throw new QuestionnaireException(
                   DomainExceptionType.DoesNotHavePermissionsForEdit,
                   "You don't have permissions for changing this questionnaire");
            }
        }

        private void ThrowDomainExceptionIfOptionsHasEmptyParentValue(Option[] options)
        {
            if (options.Select(x => x.ParentValue).Any(string.IsNullOrWhiteSpace))
            {
                throw new QuestionnaireException(
                    DomainExceptionType.CategoricalCascadingOptionsCantContainsEmptyParentValueField,
                    ExceptionMessages.CategoricalCascadingOptionsCantContainsEmptyParentValueField);
            }
        }

        private void ThrowDomainExceptionIfOptionsHasNotDecimalParentValue(Option[] options)
        {
            decimal d;
            if (options.Select(x => x.ParentValue).Any(number => !Decimal.TryParse(number, out d)))
            {
                throw new QuestionnaireException(
                    DomainExceptionType.CategoricalCascadingOptionsCantContainsNotDecimalParentValueField,
                    ExceptionMessages.CategoricalCascadingOptionsCantContainsNotDecimalParentValueField);
            }
        }

        private void ThrowDomainExceptionIfOptionsHasNotUniqueTitleAndParentValuePair(Option[] options)
        {

            if (options.Select(x => x.ParentValue + "$" + x.Title).Distinct().Count() != options.Length)
            {
                throw new QuestionnaireException(
                    DomainExceptionType.CategoricalCascadingOptionsContainsNotUniqueTitleAndParentValuePair,
                    ExceptionMessages.CategoricalCascadingOptionsContainsNotUniqueTitleAndParentValuePair);
            }
        }

        private void ThrowDomainExceptionIfVariableLabelContainsSubstitution(string variableLabel)
        {
            if (this.substitutionService.GetAllSubstitutionVariableNames(variableLabel).Length > 0)
            {
                throw new QuestionnaireException(DomainExceptionType.VariableLabelContainsSubstitutionReference,
                    ExceptionMessages.VariableLabelContainsSubstitutionReference);
            }
        }

        private void ThrowDomainExceptionIfQuestionTitleContainsIncorrectSubstitution(string text, string variableName,
            Guid questionId, bool prefilled, IGroup parentGroup)
        {
            if (this.substitutionService.GetAllSubstitutionVariableNames(text).Length > 0  && prefilled)
            {
                throw new QuestionnaireException(DomainExceptionType.FeaturedQuestionTitleContainsSubstitutionReference,
                    "It is not allowed to use substitutins in Prefilled question title");
            }    

            this.ThrowDomainExceptionIfTextContainsIncorrectSubstitution(text, variableName, questionId, parentGroup);
        }

        private void ThrowDomainExceptionIfTextContainsIncorrectSubstitution(string text, string variableName,
            Guid entityId, IGroup parentGroup)
        {
            string[] substitutionReferences = this.substitutionService.GetAllSubstitutionVariableNames(text);
            if (substitutionReferences.Length == 0)
                return;

            List<string> unknownReferences, questionsIncorrectTypeOfReferenced, questionsIllegalPropagationScope, variablesIllegalPropagationScope, rostersIllegalPropagationScope;
            
            this.ValidateSubstitutionReferences(entityId, variableName, parentGroup, substitutionReferences,
                out unknownReferences,
                out questionsIncorrectTypeOfReferenced,
                out questionsIllegalPropagationScope,
                out variablesIllegalPropagationScope,
                out rostersIllegalPropagationScope);

            if (unknownReferences.Count > 0)
                throw new QuestionnaireException(
                    DomainExceptionType.TextContainsUnknownSubstitutionReference,
                    "Text contains unknown substitution references: " + String.Join(", ", unknownReferences.ToArray()));

            if (questionsIncorrectTypeOfReferenced.Count > 0)
                throw new QuestionnaireException(
                    DomainExceptionType.TextContainsSubstitutionReferenceQuestionOfInvalidType,
                    "Text contains substitution references to questions of illegal type: " +
                    String.Join(", ", questionsIncorrectTypeOfReferenced.ToArray()));

            if (questionsIllegalPropagationScope.Count > 0)
                throw new QuestionnaireException(
                    DomainExceptionType.TextContainsInvalidSubstitutionReference,
                    "Text contains illegal substitution references to questions: " +
                    String.Join(", ", questionsIllegalPropagationScope.ToArray()));

            if (variablesIllegalPropagationScope.Count > 0)
                throw new QuestionnaireException(
                    DomainExceptionType.TextContainsInvalidSubstitutionReference,
                    "Text contains illegal substitution references to variables: " +
                    String.Join(", ", variablesIllegalPropagationScope.ToArray()));

            if (rostersIllegalPropagationScope.Count > 0)
                throw new QuestionnaireException(
                    DomainExceptionType.TextContainsInvalidSubstitutionReference,
                    "Text contains illegal substitution references to rosters: " +
                    String.Join(", ", rostersIllegalPropagationScope.ToArray()));
        }

        private void ThrowDomainExceptionIfQuestionUsedInConditionOrValidationOfOtherQuestionsAndGroups(Guid questionId)
        {
            var question = this.innerDocument.FirstOrDefault<IQuestion>(x => x.PublicKey == questionId);

            this.ThrowDomainExceptionIfElementCountIsMoreThanExpected<IComposite>(
                condition:
                    x => this.IsGroupAndHasQuestionIdInCondition(x, question) || this.IsQuestionAndHasQuestionIdInConditionOrValidation(x, question),
                expectedCount: 0,
                exceptionType: DomainExceptionType.QuestionOrGroupDependOnAnotherQuestion,
                getExceptionDescription:
                    groupsAndQuestions => string.Format("One or more questions/sub-sections depend on {0}:{1}{2}",
                        question.QuestionText,
                        Environment.NewLine,
                        string.Join(Environment.NewLine, GetTitleList(groupsAndQuestions))));
        }

        private void ThrowDomainExceptionIfGroupQuestionsUsedAsRosterTitleQuestionOfOtherGroups(Guid groupId)
        {
            var groupQuestions = this.innerDocument.Find<IQuestion>(x => IsQuestionParent(groupId, x));

            var referencedQuestions = groupQuestions.ToDictionary(question => question.PublicKey,
                question => this.GetGroupsByRosterTitleId(question.PublicKey, groupId).Select(GetTitle));

            if (referencedQuestions.Values.Count(x => x.Any()) > 0)
            {
                throw new QuestionnaireException(DomainExceptionType.QuestionUsedAsRosterTitleOfOtherGroup, string.Join(Environment.NewLine,
                    referencedQuestions.Select(x => string.Format("Question {0} used as roster title question in sub-section(s):{1}{2}",
                        FormatQuestionForException(x.Key, this.innerDocument),
                        Environment.NewLine,
                        string.Join(Environment.NewLine, x.Value)))));
            }
        }

        
        private void ThrowDomainExceptionIfQuestionIsRosterTitleAndItsMovedToIncorrectGroup(IQuestion question, IGroup targetGroup)
        {

            if (!GetGroupsByRosterTitleId(question.PublicKey).Any())
                return;
            
            IGroup sourceRoster = GetFirstRosterParentGroupOrNull(question);
            IGroup targetRoster = GetFirstRosterParentGroupOrNull(targetGroup);

            if (targetRoster == null || (sourceRoster != null && targetRoster != null && sourceRoster.RosterSizeQuestionId.HasValue &&
                targetRoster.RosterSizeQuestionId.HasValue && sourceRoster.RosterSizeQuestionId != targetRoster.RosterSizeQuestionId))
                throw new QuestionnaireException(
                    string.Format(
                        "You can move a roster title question {0} only to a roster that has a roster source question {1}",
                        this.FormatQuestionForException(question.PublicKey, this.innerDocument),
                        this.FormatQuestionForException(sourceRoster.RosterSizeQuestionId.Value, this.innerDocument)));
        }

        private void ThrowDomainExceptionIfQuestionIsRosterSizeAndItsMovedToIncorrectGroup(AbstractQuestion question, IGroup targetGroup)
        {
            var groupsByRosterSizeQuestion =
                this.GetGroupsByRosterSizeQuestion(question.PublicKey).Select(x => x.PublicKey);

            if (!groupsByRosterSizeQuestion.Any())
                return;

            foreach (var groupByRosterSizeQuestion in groupsByRosterSizeQuestion)
            {
                if (
                    !this.IsReferencedItemInTheSameScopeWithReferencesItem(GetQuestionnaireItemDepthAsVector(targetGroup.PublicKey),
                        GetQuestionnaireItemDepthAsVector(groupByRosterSizeQuestion)))
                    //   if (GetQuestionnaireItemDepth(targetGroup.PublicKey) > GetQuestionnaireItemDepth(groupByRosterSizeQuestion) - 1)
                    throw new QuestionnaireException(string.Format(
                        "Roster source question {0} cannot be placed deeper then roster.",
                        FormatQuestionForException(question.PublicKey, this.innerDocument)));
            }
        }

        private void ThrowIfRosterInformationIsIncorrect(Guid groupId, bool isRoster, RosterSizeSourceType rosterSizeSource,
            Guid? rosterSizeQuestionId, FixedRosterTitle[] rosterFixedTitles, Guid? rosterTitleQuestionId, Func<Guid[]> rosterDepthFunc)
        {
            if (!isRoster) return;

            switch (rosterSizeSource)
            {
                case RosterSizeSourceType.Question:
                    this.ThrowIfRosterSizeQuestionIsIncorrect(groupId, rosterSizeQuestionId, rosterTitleQuestionId, rosterFixedTitles,
                        rosterDepthFunc);
                    break;
                case RosterSizeSourceType.FixedTitles:
                    this.ThrowIfRosterByFixedTitlesIsIncorrect(rosterSizeQuestionId, rosterTitleQuestionId, rosterFixedTitles);
                    break;
            }
        }

        private void ThrowIfRosterByFixedTitlesIsIncorrect(Guid? rosterSizeQuestionId, Guid? rosterTitleQuestionId,
            FixedRosterTitle[] rosterFixedTitles)
        {
            if (rosterFixedTitles == null || rosterFixedTitles.Length < 2)
            {
                throw new QuestionnaireException("List of titles for fixed set of items roster should contain at least two items");
            }
            
            if (rosterFixedTitles.Length > 250)
            {
                throw new QuestionnaireException("Number of titles for fixed set of items roster could not be more than 250");
            }

            if (rosterFixedTitles.Any(item => string.IsNullOrWhiteSpace(item.Title)))
            {
                throw new QuestionnaireException("Fixed set of items roster title could not have empty titles");
            }
            
            if (rosterSizeQuestionId.HasValue)
            {
                throw new QuestionnaireException("Fixed set of items roster could not have roster source question");
            }

            if (rosterTitleQuestionId.HasValue)
            {
                throw new QuestionnaireException("Fixed set of items roster could not have roster title question");
            }
        }

        private void ThrowIfRosterSizeQuestionIsIncorrect(Guid groupId, Guid? rosterSizeQuestionId, Guid? rosterTitleQuestionId,
            FixedRosterTitle[] rosterFixedTitles, Func<Guid[]> rosterDepthFunc)
        {
            if (!rosterSizeQuestionId.HasValue)
                throw new QuestionnaireException("Roster source question is empty");

            var localRosterSizeQuestionId = rosterSizeQuestionId.Value;
            var rosterSizeQuestion = this.innerDocument.Find<IQuestion>(localRosterSizeQuestionId);
            if (rosterSizeQuestion == null)
                // TODO: Guid should be replaced, but question is missing, so title or variable name cannot be found 
                throw new QuestionnaireException(string.Format(
                    "Roster source question {0} is missing in questionnaire.",
                    rosterSizeQuestionId));

            if (!RosterSizeQuestionTypes.Contains(rosterSizeQuestion.QuestionType))
                throw new QuestionnaireException(string.Format(
                    "Roster source question {0} should have numeric or categorical multi-select Answers or List type.",
                    FormatQuestionForException(localRosterSizeQuestionId, this.innerDocument)));

            if (
                !this.IsReferencedItemInTheSameScopeWithReferencesItem(this.GetQuestionnaireItemDepthAsVector(rosterSizeQuestionId),
                    rosterDepthFunc()))
                throw new QuestionnaireException(string.Format(
                    "Roster source question {0} cannot be placed deeper then roster.",
                    FormatQuestionForException(localRosterSizeQuestionId, this.innerDocument)));


            if (rosterSizeQuestion.QuestionType == QuestionType.MultyOption && rosterSizeQuestion.LinkedToQuestionId.HasValue)
                throw new QuestionnaireException(string.Format(
                    "Roster source question {0} should not be linked.",
                    FormatQuestionForException(localRosterSizeQuestionId, this.innerDocument)));

            if (rosterSizeQuestion.QuestionType == QuestionType.MultyOption && rosterTitleQuestionId.HasValue)
                throw new QuestionnaireException(string.Format(
                    "Roster having categorical multi-select question {0} as roster size source cannot have roster title question.",
                    this.FormatQuestionForException(localRosterSizeQuestionId, this.innerDocument)));

            if (rosterSizeQuestion.QuestionType == QuestionType.TextList && rosterTitleQuestionId.HasValue)
                throw new QuestionnaireException(string.Format(
                    "Roster having list question {0} as roster size source cannot have roster title question.",
                    this.FormatQuestionForException(localRosterSizeQuestionId, this.innerDocument)));

            if (rosterSizeQuestion.QuestionType == QuestionType.Numeric)
            {
                var numericQuestion = (INumericQuestion)rosterSizeQuestion;

                if (!numericQuestion.IsInteger)
                    throw new QuestionnaireException(string.Format(
                        "Roster source question {0} should be Integer.",
                        FormatQuestionForException(localRosterSizeQuestionId, this.innerDocument)));

                if (rosterTitleQuestionId.HasValue)
                {
                    var rosterTitleQuestion = this.innerDocument.Find<IQuestion>(rosterTitleQuestionId.Value);
                    if (rosterTitleQuestion == null)
                        // TODO: Guid should be replaced, but question is missing, so title or variable name cannot be found 
                        throw new QuestionnaireException(string.Format(
                            "Roster title question {0} is missing in questionnaire.", rosterTitleQuestionId));

                    if (
                        !IsRosterTitleInRosterByRosterSize(rosterTitleQuestion: rosterTitleQuestion,
                            rosterSizeQuestionId: localRosterSizeQuestionId, currentRosterId: groupId))
                        throw new QuestionnaireException(string.Format(
                            "Question for roster titles {0} should be placed only inside groups where roster source question is {1}",
                            FormatQuestionForException(rosterTitleQuestionId.Value, this.innerDocument),
                            FormatQuestionForException(localRosterSizeQuestionId, this.innerDocument)));
                }
            }

            if (rosterFixedTitles != null && rosterFixedTitles.Any())
            {
                throw new QuestionnaireException(string.Format("Roster fixed items list should be empty for roster by question: {0}.",
                    FormatGroupForException(groupId, this.innerDocument)));
            }
        }

        private void ThrowIfQuestionIsUsedAsRosterSize(Guid questionId)
        {
            var referencingRoster = this.innerDocument.Find<IGroup>(group => @group.RosterSizeQuestionId == questionId).FirstOrDefault();

            if (referencingRoster != null)
                throw new QuestionnaireException(
                    string.Format("Question {0} is referenced as roster source question by sub-section {1}.",
                        FormatQuestionForException(questionId, this.innerDocument),
                        FormatGroupForException(referencingRoster.PublicKey, this.innerDocument)));
        }

        private void ThrowIfQuestionIsUsedAsRosterTitle(Guid questionId)
        {
            var referencingRosterTitle =
                this.innerDocument.Find<IGroup>(
                    group =>
                        @group.RosterTitleQuestionId == questionId && group.RosterSizeQuestionId.HasValue &&
                            this.innerDocument.FirstOrDefault<IQuestion>(
                                question =>
                                    question.PublicKey == @group.RosterSizeQuestionId.Value && question.QuestionType == QuestionType.Numeric) != null)
                    .FirstOrDefault();

            if (referencingRosterTitle != null)
                throw new QuestionnaireException(
                    string.Format("Question {0} is referenced as roster title question by sub-section {1}.",
                        FormatQuestionForException(questionId, this.innerDocument),
                        FormatGroupForException(referencingRosterTitle.PublicKey, this.innerDocument)));
        }

        private void ThrowIfRosterHaveAQuestionThatUsedAsRosterTitleQuestionOfOtherGroups(IGroup group)
        {
            var allRosterTitleQuestions =
                this.innerDocument.Find<IGroup>(g => g.PublicKey != group.PublicKey && g.RosterTitleQuestionId.HasValue)
                    .Select(g => g.RosterTitleQuestionId.Value);


            if (!allRosterTitleQuestions.Any()) return;

            var allQuestionsInGroup = GetAllQuestionsInGroup(group).Select(q => q.PublicKey);

            var rosterTitleQuestionsOfOtherGroups =
                allQuestionsInGroup.Intersect(allRosterTitleQuestions);

            if (!rosterTitleQuestionsOfOtherGroups.Any()) return;

            throw new QuestionnaireException(
                string.Format(
                    "This roster can't become a sub-section because contains a roster title questions of other sub-section(s): {0}",
                    string.Join(Environment.NewLine,
                        rosterTitleQuestionsOfOtherGroups.Select(
                            questionId => FormatQuestionForException(questionId, this.innerDocument)))));
        }

        private void ThrowIfGroupCantBecomeARosterBecauseOfPrefilledQuestions(IGroup group)
        {
            var hasAnyPrefilledQuestion = this.innerDocument.GetEntitiesByType<AbstractQuestion>(@group).Any(question => question.Featured);

            if (!hasAnyPrefilledQuestion) return;

            var questionVariables = GetFilteredQuestionForException(@group, question => question.Featured);

            throw new QuestionnaireException(
                string.Format(
                    "This sub-section can't become a roster because contains pre-filled questions: {0}. Toggle off pre-filled property for that questions to complete this operation",
                    string.Join(Environment.NewLine, questionVariables)));
        }

        private void ThrowIfRosterCantBecomeAGroupBecauseOfReferencesOnRosterTitleInSubstitutions(IGroup group, bool wasRosterAndBecomeAGroup = false)
        {
            var hasAnyQuestionsWithRosterTitleInSubstitutions =
                this.innerDocument.GetEntitiesByType<AbstractQuestion>(@group)
                    .Where(x => wasRosterAndBecomeAGroup || GetFirstRosterParentGroupOrNull(x, group) == null)
                    .Any(
                        question =>
                            this.substitutionService.GetAllSubstitutionVariableNames(question.QuestionText)
                                .Contains(this.substitutionService.RosterTitleSubstitutionReference));

            if (!hasAnyQuestionsWithRosterTitleInSubstitutions) return;

            var questionVariables = GetFilteredQuestionForException(@group, question => this.substitutionService.GetAllSubstitutionVariableNames(question.QuestionText)
                                .Contains(this.substitutionService.RosterTitleSubstitutionReference));

            throw new QuestionnaireException(
                string.Format(
                    "This sub-section can't become a roster because contains questions with reference on roster title in substitution: {0}.",
                    string.Join(Environment.NewLine, questionVariables)));
        }

        private void ThrowIfRosterCantBecomeAGroupBecauseContainsLinkedSourceQuestions(IGroup group)
        {
            if (GetFirstRosterParentGroupOrNull(group.GetParent()) != null)
                return;

            var allQuestionsIdsFromGroup = this.GetAllQuestionsInGroup(@group).Select(question => question.PublicKey);

            var linkedQuestionSourcesInGroup = this.GetAllLinkedSourceQuestions().Intersect(allQuestionsIdsFromGroup);

            if (linkedQuestionSourcesInGroup.Any())
            {
                throw new QuestionnaireException(
                    string.Format(
                        "This {0} roster can't become a sub-section because contains linked source question(s): {1}",
                        FormatGroupForException(@group.PublicKey, this.innerDocument),
                        string.Join(Environment.NewLine,
                            linkedQuestionSourcesInGroup.Select(
                                questionId => this.FormatQuestionForException(questionId, this.innerDocument)))));
            }
        }

        private void ThrowDomainExceptionIfRosterQuestionsUsedAsLinkedSourceQuestions(IGroup group)
        {
            if (!this.IsRosterOrInsideRoster(@group)) return;

            var allQuestionsIdsFromGroup = this.GetAllQuestionsInGroup(@group).Select(question => question.PublicKey);

            var linkedQuestionSourcesInGroup = this.GetAllLinkedSourceQuestions().Intersect(allQuestionsIdsFromGroup);

            if (linkedQuestionSourcesInGroup.Any())
            {
                throw new QuestionnaireException(
                    string.Format(
                        "You can't delete {0} sub-section because it contains linked source question(s): {1}",
                        FormatGroupForException(@group.PublicKey, this.innerDocument),
                        string.Join(Environment.NewLine,
                            linkedQuestionSourcesInGroup.Select(
                                questionId => this.FormatQuestionForException(questionId, this.innerDocument)))));
            }
        }

        private void ThrowDomainExceptionIfFilteredComboboxIsInvalid(Guid questionId, Option[] options)
        {
            var categoricalOneAnswerQuestion = this.innerDocument.Find<SingleQuestion>(questionId);
            if (categoricalOneAnswerQuestion == null)
            {
                throw new QuestionnaireException(
                    DomainExceptionType.FilteredComboboxQuestionNotFound,
                    string.Format("Combo box with public key {0} can't be found", questionId));
            }

            if (!categoricalOneAnswerQuestion.IsFilteredCombobox.HasValue || !categoricalOneAnswerQuestion.IsFilteredCombobox.Value)
            {
                throw new QuestionnaireException(
                    DomainExceptionType.QuestionIsNotAFilteredCombobox,
                    string.Format("Question {0} is not a combo box", FormatQuestionForException(questionId, this.innerDocument)));
            }

            if (options != null && options.Length > maxFilteredComboboxOptionsCount)
            {
                throw new QuestionnaireException(
                    DomainExceptionType.FilteredComboboxQuestionOptionsMaxLength,
                    string.Format("Combo box question {0} contains more than {1} options",
                        FormatQuestionForException(questionId, this.innerDocument), maxFilteredComboboxOptionsCount));
            }
        }

        private void ThrowDomainExceptionIfCascadingComboboxIsInvalid(Guid questionId, Option[] options)
        {
            var categoricalOneAnswerQuestion = this.innerDocument.Find<SingleQuestion>(questionId);
            if (categoricalOneAnswerQuestion == null)
            {
                throw new QuestionnaireException(
                    DomainExceptionType.QuestionNotFound,
                    string.Format("Combo box with public key {0} can't be found", questionId));
            }
            
            if (options != null && options.Length > maxCascadingComboboxOptionsCount)
            {
                throw new QuestionnaireException(
                    DomainExceptionType.CategoricalCascadingQuestionOptionsMaxLength,
                    string.Format("Combo box question {0} contains more than {1} options",
                        FormatQuestionForException(questionId, this.innerDocument), maxCascadingComboboxOptionsCount));
            }
        }

        #endregion

        #region Utilities

        private static void PrepareGeneralProperties(ref string title, ref string variableName)
        {
            variableName = variableName?.Trim();
            title = title?.Trim();
        }

        private void ValidateSubstitutionReferences(Guid entityId, string variableName, IGroup parentGroup, string[] substitutionReferences,
            out List<string> unknownReferences, 
            out List<string> questionsIncorrectTypeOfReferenced,
            out List<string> questionsIllegalPropagationScope,
            out List<string> variablesIllegalPropagationScope,
            out List<string> rostersIllegalPropagationScope)
        {
            unknownReferences = new List<string>();
            questionsIncorrectTypeOfReferenced = new List<string>();
            questionsIllegalPropagationScope = new List<string>();
            variablesIllegalPropagationScope = new List<string>();
            rostersIllegalPropagationScope = new List<string>();

            var questions = this.innerDocument.GetEntitiesByType<AbstractQuestion>()
                .Where(q => q.PublicKey != entityId)
                .Where(q => !string.IsNullOrEmpty(q.StataExportCaption))
                .GroupBy(q => q.StataExportCaption, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var variables = this.innerDocument.GetEntitiesByType<Variable>()
                .Where(v => v.PublicKey != entityId && !string.IsNullOrEmpty(v.Name))
                .GroupBy(v => v.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var rosters = this.innerDocument.GetEntitiesByType<Group>()
                .Where(r => r.PublicKey != entityId && !string.IsNullOrEmpty(r.VariableName) && r.IsRoster)
                .GroupBy(r => r.VariableName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);


            var rosterVectorOfEntity = GetQuestionnaireItemDepthAsVector(parentGroup.PublicKey);

            foreach (var substitutionReference in substitutionReferences)
            {
                if (substitutionReference == this.substitutionService.RosterTitleSubstitutionReference)
                {
                    if (rosterVectorOfEntity.Length > 0)
                        continue;
                }
                //extract validity of variable name to separate method and make check validity of substitutionReference  
                if (substitutionReference.Length > 32)
                {
                    unknownReferences.Add(substitutionReference);
                    continue;
                }

                bool isQuestionReference = questions.ContainsKey(substitutionReference);
                bool isVariableReference = variables.ContainsKey(substitutionReference);
                bool isRosterReference = rosters.ContainsKey(substitutionReference);
                bool isSelfReference = string.Compare(substitutionReference, variableName, StringComparison.OrdinalIgnoreCase) == 0;

                if (!isQuestionReference && !isVariableReference && !isRosterReference && !isSelfReference)
                {
                    unknownReferences.Add(substitutionReference);
                }
                else if (isQuestionReference)
                {
                    var currentQuestion = questions[substitutionReference];
                    bool typeOfRefQuestionIsNotSupported = !(currentQuestion.QuestionType == QuestionType.DateTime ||
                        currentQuestion.QuestionType == QuestionType.Numeric ||
                        currentQuestion.QuestionType == QuestionType.SingleOption ||
                        currentQuestion.QuestionType == QuestionType.Text ||
                        currentQuestion.QuestionType == QuestionType.AutoPropagate ||
                        currentQuestion.QuestionType == QuestionType.QRBarcode);

                    if (typeOfRefQuestionIsNotSupported)
                        questionsIncorrectTypeOfReferenced.Add(substitutionReference);

                    if (!this.IsReferencedItemInTheSameScopeWithReferencesItem(this.GetQuestionnaireItemDepthAsVector(currentQuestion.PublicKey), rosterVectorOfEntity))
                        questionsIllegalPropagationScope.Add(substitutionReference);
                }
                else if (isVariableReference)
                {
                    var currentVariable = variables[substitutionReference];

                    if (!this.IsReferencedItemInTheSameScopeWithReferencesItem(this.GetQuestionnaireItemDepthAsVector(currentVariable.PublicKey), rosterVectorOfEntity))
                        variablesIllegalPropagationScope.Add(substitutionReference);
                }
                else if (isRosterReference)
                {
                    var referencedRoster = rosters[substitutionReference];

                    var rosterRosterVector = this.GetQuestionnaireItemDepthAsVector(referencedRoster.PublicKey);
                    if (!this.IsReferencedItemInTheSameScopeWithReferencesItem(rosterRosterVector, rosterVectorOfEntity))
                        rostersIllegalPropagationScope.Add(substitutionReference);
                }
            }
        }

        private static Answer[] ConvertOptionsToAnswers(Option[] options)
        {
            if (options == null)
                return null;

            return options.Select(ConvertOptionToAnswer).ToArray();
        }

        private static Answer ConvertOptionToAnswer(Option option)
        {
            return new Answer
            {
                PublicKey = option.Id,
                AnswerValue = option.Value,
                AnswerText = option.Title,
                ParentValue = option.ParentValue
            };
        }

        private IEnumerable<Guid> GetRosterSizeQuestionsOfGroupAndUpperRosters(IGroup group)
        {
            var targerGroup = @group;
            while (targerGroup != null)
            {
                if (targerGroup.IsRoster && targerGroup.RosterSizeQuestionId.HasValue)
                    yield return targerGroup.RosterSizeQuestionId.Value;

                targerGroup = targerGroup.GetParent() as IGroup;
            }
        }

        private IEnumerable<IQuestion> GetAllCategoricalLinkedQuestions()
        {
            return this.innerDocument.Find<IQuestion>(question => question.LinkedToQuestionId.HasValue);
        }

        private IEnumerable<Guid> GetAllLinkedSourceQuestions()
        {
            return this.GetAllCategoricalLinkedQuestions().Select(question => question.LinkedToQuestionId.Value);
        }

        private IEnumerable<Guid> GetAllRosterSizeQuestionIds()
        {
            return
                this.innerDocument.Find<IGroup>(group => group.RosterSizeQuestionId.HasValue)
                    .Select(group => group.RosterSizeQuestionId.Value);
        }

        private bool IsUnderPropagatableGroup(IComposite item)
        {
            return this.GetFirstRosterParentGroupOrNull(item) != null;
        }

        private IGroup GetFirstRosterParentGroupOrNull(IComposite item, IGroup stopGroup = null)
        {
            while (item != null)
            {
                var parentGroup = item as IGroup;
                if (stopGroup != null && parentGroup != null && parentGroup == stopGroup)
                {
                    return parentGroup.IsRoster ? parentGroup : (IGroup) null;
                }
                if (parentGroup != null && parentGroup.IsRoster)
                    return parentGroup;

                item = item.GetParent();
            }
            return null;
        }

        private Guid GetScopeOrRoster(IGroup entityAsGroup)
        {
            if (entityAsGroup.RosterSizeSource == RosterSizeSourceType.FixedTitles)
                return entityAsGroup.PublicKey;

            if (!entityAsGroup.RosterSizeQuestionId.HasValue)
                return entityAsGroup.PublicKey;

            var rosterSizeQuestion = innerDocument.Find<IQuestion>(entityAsGroup.RosterSizeQuestionId.Value);
            if (rosterSizeQuestion == null)
                return entityAsGroup.PublicKey;

            return rosterSizeQuestion.PublicKey;
        }

        private Guid[] GetQuestionnaireItemDepthAsVector(Guid? itemId)
        {
            if (!itemId.HasValue)
                return new Guid[0];

            var entity = innerDocument.Find<IComposite>(itemId.Value);
            if (entity == null)
                return new Guid[0];

            var scopeIds = new List<Guid>();

            var entityAsGroup = entity as IGroup;
            if (entityAsGroup != null && entityAsGroup.IsRoster)
            {
                scopeIds.Add(GetScopeOrRoster(entityAsGroup));
            }
            
            var currentParent = (IGroup)entity.GetParent();
            while (currentParent != null)
            {
                if (currentParent.IsRoster)
                    scopeIds.Add(GetScopeOrRoster(currentParent));

                currentParent = (IGroup)currentParent.GetParent();
            }
            return scopeIds.ToArray();
        }

        public IQuestion GetQuestionByStataCaption(string stataCaption)
        {
            return this.innerDocument.FirstOrDefault<IQuestion>(q => q.StataExportCaption == stataCaption);
        }

        private IQuestion GetQuestion(Guid questionId)
        {
            return this.innerDocument.FirstOrDefault<IQuestion>(q => q.PublicKey == questionId);
        }

        private static bool IsExpressionDefined(string expression)
        {
            return !string.IsNullOrWhiteSpace(expression);
        }

        private IEnumerable<IGroup> GetGroupsByRosterTitleId(Guid questionId, params Guid[] exceptGroups)
        {
            return this.innerDocument.Find<IGroup>(
                group => !exceptGroups.Contains(group.PublicKey) && (group.RosterTitleQuestionId == questionId));
        }

        private bool IsRosterOrInsideRoster(IGroup group)
        {
            while (group != null)
            {
                if (group.IsRoster)
                    return true;

                group = (IGroup)group.GetParent();
            }

            return false;
        }

        private bool ContainsRoster(IGroup group)
        {
            if (group != null)
            {
                foreach (var subGroup in group.Children.OfType<IGroup>())
                {
                    if (subGroup.IsRoster || ContainsRoster(subGroup))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool AreElementsUnique(IEnumerable<string> elements)
        {
            return elements.Distinct().Count() == elements.Count();
        }

        private bool IsReferencedItemInTheSameScopeWithReferencesItem(Guid[] referencedItemRosterVector, Guid[] referencesItemRosterVector)
        {
            if (referencedItemRosterVector.Length > referencesItemRosterVector.Length)
                return false;

            return referencedItemRosterVector.All(referencesItemRosterVector.Contains);
        }

        private bool IsRosterTitleInRosterByRosterSize(IQuestion rosterTitleQuestion, Guid rosterSizeQuestionId, Guid currentRosterId)
        {
            var groupsByRosterSizeQuestion =
                this.GetGroupsByRosterSizeQuestion(rosterSizeQuestionId).Select(x => x.PublicKey).ToHashSet();
            groupsByRosterSizeQuestion.Add(currentRosterId);

            var parentForRosterTitleQuestion = rosterTitleQuestion.GetParent();
            while (parentForRosterTitleQuestion != null)
            {
                if (groupsByRosterSizeQuestion.Contains(parentForRosterTitleQuestion.PublicKey))
                    return true;
                var parentGroup = parentForRosterTitleQuestion as IGroup;
                if (parentGroup != null && parentGroup.IsRoster)
                    break;

                parentForRosterTitleQuestion = parentForRosterTitleQuestion.GetParent();
            }

            return false;
        }

        private IGroup GetRosterByrVariableName(string rosterName)
        {
            return this.innerDocument.Find<IGroup>(group => group.VariableName == rosterName && group.IsRoster).FirstOrDefault();
        }

        private IEnumerable<IGroup> GetGroupsByRosterSizeQuestion(Guid rosterSizeQuestionId)
        {
            return this.innerDocument.Find<IGroup>(
                group => group.RosterSizeQuestionId == rosterSizeQuestionId);
        }

        private IEnumerable<IQuestion> GetAllQuestionsInGroup(IGroup group)
        {
            var questionsInGroup = new List<IQuestion>();

            foreach (var groupItem in group.Children)
            {
                var itemAsGroup = groupItem as IGroup;
                if (itemAsGroup != null)
                {
                    questionsInGroup.AddRange(GetAllQuestionsInGroup(itemAsGroup));
                }

                var itemAsQuestion = groupItem as IQuestion;
                if (itemAsQuestion != null)
                {
                    questionsInGroup.Add(itemAsQuestion);
                }
            }
            return questionsInGroup;
        }

        private string[] GetFilteredQuestionForException(IGroup @group, Func<AbstractQuestion, bool> filter)
        {
            return this.innerDocument.GetEntitiesByType<AbstractQuestion>(@group)
                .Where(filter)
                .Select(question => string.Format("'{0}', [{1}]", question.QuestionText, question.StataExportCaption))
                .ToArray();
        }

        private string FormatQuestionForException(Guid questionId, QuestionnaireDocument document)
        {
            var question = document.Find<IQuestion>(questionId);

            return string.Format("'{0}', {1}", question.QuestionText, question.StataExportCaption);
        }

        private IEnumerable<IGroup> GetAllParentGroups(IComposite entity)
        {
            var currentParent = (IGroup)entity.GetParent();

            while (currentParent != null)
            {
                yield return currentParent;

                currentParent = (IGroup)currentParent.GetParent();
            }
        }

        private bool IsQuestionParent(Guid groupId, IQuestion question)
        {
            return GetAllParentGroups(question).Any(x => x.PublicKey == groupId);
        }

        private IGroup GetGroupById(Guid groupId)
        {
            return this.innerDocument.Find<IGroup>(groupId);
        }

        private static string[] GetTitleList(IEnumerable<IComposite> groupsAndQuestions)
        {
            return groupsAndQuestions.Select(GetTitle).ToArray();
        }

        private static string GetTitle(IComposite composite)
        {
            var question = composite as IQuestion;
            var group = composite as IGroup;
            if (group != null)
            {
                return group.Title;
            }
            if (question != null)
            {
                return question.QuestionText;
            }
            return "<untitled>";
        }

        private bool IsQuestionAndHasQuestionIdInConditionOrValidation(IComposite composite, IQuestion sourceQuestion)
        {
            var question = composite as IQuestion;
            bool isSelfReferenceIsChecking = composite.PublicKey == sourceQuestion.PublicKey;

            if (isSelfReferenceIsChecking)
            {
                // we should allow do delete questions that reference itself in condition or validation expression
                return false;
            }

            if (question != null)
            {
                string questionId = sourceQuestion.PublicKey.ToString();
                string alias = sourceQuestion.StataExportCaption;

                IEnumerable<string> conditionIds = new List<string>();
                if (IsExpressionDefined(question.ConditionExpression))
                {
                    conditionIds = this.expressionProcessor.GetIdentifiersUsedInExpression(question.ConditionExpression);
                }

                List<string> validationIds = new List<string>();
                foreach (var validationCondition in question.ValidationConditions)
                {
                    if (IsExpressionDefined(validationCondition.Expression))
                    {
                        validationIds.AddRange(this.expressionProcessor.GetIdentifiersUsedInExpression(validationCondition.Expression));
                    }
                }

                return validationIds.Contains(questionId) || validationIds.Contains(alias) ||
                    conditionIds.Contains(questionId) || conditionIds.Contains(alias);
            }
            return false;
        }

        private bool IsGroupAndHasQuestionIdInCondition(IComposite composite, IQuestion question)
        {
            var group = composite as IGroup;
            if (group != null && IsExpressionDefined(group.ConditionExpression))
            {
                string alias = question.StataExportCaption;

                IEnumerable<string> conditionIds = this.expressionProcessor.GetIdentifiersUsedInExpression(group.ConditionExpression).ToList();
               
                return conditionIds.Contains(alias);
            }
            return false;
        }

        private static string FormatGroupForException(Guid groupId, QuestionnaireDocument questionnaireDocument)
        {
            return string.Format("'{0}'", GetGroupTitleForException(groupId, questionnaireDocument));
        }

        private static string GetGroupTitleForException(Guid groupId, QuestionnaireDocument questionnaireDocument)
        {
            var @group = questionnaireDocument.Find<IGroup>(groupId);

            return @group != null
                ? @group.Title ?? "<<NO GROUP TITLE>>"
                : "<<MISSING GROUP>>";
        }

        private static void UpdateCustomMappingsWithContextQuestion(Dictionary<string, string> customMappings, IQuestion contextQuestion)
        {
            customMappings["this"] = contextQuestion.StataExportCaption;
        }

        private Dictionary<string, string> BuildCustomMappingsFromIdsToIdentifiers()
        {
            return this.innerDocument
                .Find<IQuestion>(question => !string.IsNullOrWhiteSpace(question.StataExportCaption))
                .SelectMany(question => new []
                {
                    new KeyValuePair<string, string>(question.PublicKey.ToString(), question.StataExportCaption),
                    new KeyValuePair<string, string>(question.PublicKey.FormatGuid(), question.StataExportCaption),
                })
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value);
        }

        private static bool HasEnablementCondition(IGroup group)
        {
            return !string.IsNullOrWhiteSpace(@group.ConditionExpression);
        }

        
        private FixedRosterTitle[] GetRosterFixedTitlesOrThrow(FixedRosterTitleItem[] rosterFixedTitles)
        {
            if (rosterFixedTitles == null)
                return new FixedRosterTitle[0];

            if (rosterFixedTitles.Any(x => x == null))
            {
                throw new QuestionnaireException(
                    DomainExceptionType.SelectorValueSpecialCharacters,
                    "Invalid title list");
            }

            if (rosterFixedTitles.Any(x => String.IsNullOrWhiteSpace(x.Value)))
            {
                throw new QuestionnaireException(
                    DomainExceptionType.SelectorValueSpecialCharacters,
                    "Fixed set of items roster value is required");
            }

            if (rosterFixedTitles.Any(x => !x.Value.IsDecimal()))
            {
                throw new QuestionnaireException(
                    DomainExceptionType.SelectorValueSpecialCharacters,
                    "Fixed set of items roster value should have only number characters");
            }

            if (rosterFixedTitles.Select(x => x.Value).Distinct().Count() != rosterFixedTitles.Length)
            {
                throw new QuestionnaireException("Fixed set of items roster values must be unique");
            }

            return rosterFixedTitles.Select(item => new FixedRosterTitle(decimal.Parse(item.Value), item.Title)).ToArray();                
                
        }

        private static int GetMaxChildGroupNestingDepth(IGroup group)
        {
            int maxDepth = 1;
            Queue<Tuple<IGroup, int>> queue = new Queue<Tuple<IGroup, int>>();
            queue.Enqueue(Tuple.Create(group, 1));

            while (queue.Any())
            {
                var item = queue.Dequeue();
                foreach (var subGroup in item.Item1.Children.OfType<IGroup>())
                {
                    queue.Enqueue(Tuple.Create(subGroup, item.Item2 + 1));
                }

                if (item.Item2 > maxDepth)
                    maxDepth = item.Item2;
            }

            return maxDepth;
        }

        #endregion

        public void SetId(Guid id)
        {
            this.innerDocument.PublicKey = id;
        }

        #region factory methods
        
        private static IQuestion CreateQuestion(Guid publicKey, QuestionType questionType, QuestionScope questionScope,
            string questionText, string stataExportCaption, string variableLabel, string conditionExpression,
            bool hideIfDisabled, Order? answerOrder, bool featured, bool capital, string instructions,
            QuestionProperties questionProperties, string mask, Answer[] answers,
            Guid? linkedToQuestionId, Guid? linkedToRosterId, bool? isInteger,
            int? countOfDecimalPlaces, bool? areAnswersOrdered, int? maxAllowedAnswers,
            int? maxAnswerCount, bool? isFilteredCombobox, Guid? cascadeFromQuestionId,
            bool? yesNoView, IList<ValidationCondition> validationConditions,
            string linkedFilterExpression, bool isTimestamp)
        {
            AbstractQuestion question;

            switch (questionType)
            {
                case QuestionType.MultyOption:
                    question = new MultyOptionsQuestion()
                    {
                        AreAnswersOrdered = areAnswersOrdered ?? false,
                        MaxAllowedAnswers = maxAllowedAnswers,
                        YesNoView = yesNoView ?? false
                    };
                    UpdateAnswerList(answers, question, linkedToQuestionId);
                    break;
                case QuestionType.SingleOption:
                case QuestionType.YesNo:
                    question = new SingleQuestion();
                    UpdateAnswerList(answers, question, linkedToQuestionId);
                    break;

                case QuestionType.Text:
                    question = new TextQuestion()
                    {
                        Mask = mask
                    };
                    break;
                case QuestionType.DateTime:
                    question = new DateTimeQuestion()
                    {
                        IsTimestamp = isTimestamp
                    };
                    break;
                case QuestionType.Numeric:
                case QuestionType.AutoPropagate:
                    question = new NumericQuestion()
                    {
                        IsInteger = questionType == QuestionType.AutoPropagate ? true : isInteger ?? false,
                        CountOfDecimalPlaces = countOfDecimalPlaces,
                        QuestionType = QuestionType.Numeric,
                        UseFormatting = questionProperties?.UseFormatting ?? false
                    };
                    break;
                case QuestionType.GpsCoordinates:
                    question = new GpsCoordinateQuestion();
                    break;

                case QuestionType.TextList:
                    question = new TextListQuestion()
                    {
                        MaxAnswerCount = maxAnswerCount
                    };
                    break;
                case QuestionType.QRBarcode:
                    question = new QRBarcodeQuestion();
                    break;

                case QuestionType.Multimedia:
                    question = new MultimediaQuestion();
                    break;

                default:
                    throw new NotSupportedException(string.Format("Question type is not supported: {0}", questionType));
            }

            question.PublicKey = publicKey;
            question.QuestionType = questionType;
            question.QuestionScope = questionScope;
            question.QuestionText = System.Web.HttpUtility.HtmlDecode(questionText);
            question.StataExportCaption = stataExportCaption;
            question.VariableLabel = variableLabel;
            question.ConditionExpression = conditionExpression;
            question.HideIfDisabled = hideIfDisabled;
            question.ValidationExpression = null;
            question.ValidationMessage = null;
            question.AnswerOrder = answerOrder;
            question.Featured = featured;
            question.Instructions = instructions;
            question.Properties = questionProperties ?? new QuestionProperties(false, false);
            question.Capital = capital;
            question.LinkedToQuestionId = linkedToQuestionId;
            question.LinkedToRosterId = linkedToRosterId;
            question.LinkedFilterExpression = linkedFilterExpression;
            question.IsFilteredCombobox = isFilteredCombobox;
            question.CascadeFromQuestionId = cascadeFromQuestionId;
            question.ValidationConditions = validationConditions ?? new List<ValidationCondition>();

            return question;
        }
        private static void UpdateAnswerList(IEnumerable<Answer> answers, IQuestion question, Guid? linkedToQuestionId)
        {
            question.Answers?.Clear();

            if (linkedToQuestionId.HasValue || answers == null || !answers.Any()) return;

            foreach (var answer in answers)
            {
                question.AddAnswer(answer);
            }
        }

        private static IGroup CreateGroup(Guid id, string title, string variableName, string description, string enablingCondition, bool hideIfDisabled)
        {
            var group = new Group();
            group.Title = System.Web.HttpUtility.HtmlDecode(title);
            group.VariableName = variableName;
            group.PublicKey = id;
            group.Description = description;
            group.ConditionExpression = enablingCondition;
            group.HideIfDisabled = hideIfDisabled;
            return group;
        }

        #endregion

        public void RevertVersion(RevertVersionQuestionnaire command)
        {
            this.ThrowDomainExceptionIfViewerDoesNotHavePermissionsForEditQuestionnaire(command.ResponsibleId);

            var historyReferanceId = command.HistoryReferanceId;
            var questionnire = questionnireHistotyVersionsService.GetByHistoryVersion(historyReferanceId);
            if (questionnire == null)
                throw new ArgumentException($"Questionnire {Id} of version {historyReferanceId} didn't find");

            this.innerDocument = questionnire;
        }
    }
}