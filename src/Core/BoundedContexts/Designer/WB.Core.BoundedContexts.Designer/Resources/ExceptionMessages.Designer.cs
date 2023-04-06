﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WB.Core.BoundedContexts.Designer.Resources {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class ExceptionMessages {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal ExceptionMessages() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("WB.Core.BoundedContexts.Designer.Resources.ExceptionMessages", typeof(ExceptionMessages).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Missing attachment with id {0}.
        /// </summary>
        public static string AttachmentIdIsMissing {
            get {
                return ResourceManager.GetString("AttachmentIdIsMissing", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Uploaded content is not supported.
        /// </summary>
        public static string Attachments_Unsupported_content {
            get {
                return ResourceManager.GetString("Attachments_Unsupported_content", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Image dimensions are too big.
        /// </summary>
        public static string Attachments_uploaded_file_image_is_too_big {
            get {
                return ResourceManager.GetString("Attachments_uploaded_file_image_is_too_big", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Uploaded file is not an image.
        /// </summary>
        public static string Attachments_uploaded_file_is_not_image {
            get {
                return ResourceManager.GetString("Attachments_uploaded_file_is_not_image", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to It is readonly Cover section. You cannot edit or re-order questions in it..
        /// </summary>
        public static string CantEditCoverPageInOldQuestionnaire {
            get {
                return ResourceManager.GetString("CantEditCoverPageInOldQuestionnaire", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You can&apos;t move to sub-section {0} because it position {1} in not acceptable..
        /// </summary>
        public static string CantMoveSubsectionInWrongPosition {
            get {
                return ResourceManager.GetString("CantMoveSubsectionInWrongPosition", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Question cannot be pasted here..
        /// </summary>
        public static string CantPasteQuestion {
            get {
                return ResourceManager.GetString("CantPasteQuestion", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cover section cannot be removed from questionnaire.
        /// </summary>
        public static string CantRemoveCoverPageInQuestionnaire {
            get {
                return ResourceManager.GetString("CantRemoveCoverPageInQuestionnaire", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Last existing section cannot be removed from questionnaire.
        /// </summary>
        public static string CantRemoveLastSectionInQuestionnaire {
            get {
                return ResourceManager.GetString("CantRemoveLastSectionInQuestionnaire", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Couldn&apos;t remove user, because it doesn&apos;t exist in share list.
        /// </summary>
        public static string CantRemoveUserFromTheList {
            get {
                return ResourceManager.GetString("CantRemoveUserFromTheList", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Categories in cascading question cannot have empty ParentValue column..
        /// </summary>
        public static string CategoricalCascadingOptionsCantContainsEmptyParentValueField {
            get {
                return ResourceManager.GetString("CategoricalCascadingOptionsCantContainsEmptyParentValueField", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Categories in cascading question cannot have not numeric value in ParentValue column..
        /// </summary>
        public static string CategoricalCascadingOptionsCantContainsNotDecimalParentValueField {
            get {
                return ResourceManager.GetString("CategoricalCascadingOptionsCantContainsNotDecimalParentValueField", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to There is at least one duplicate of &quot;Title&quot; and &quot;Parent Value&quot; pairs. The list should not contain any duplicates..
        /// </summary>
        public static string CategoricalCascadingOptionsContainsNotUniqueTitleAndParentValuePair {
            get {
                return ResourceManager.GetString("CategoricalCascadingOptionsContainsNotUniqueTitleAndParentValuePair", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to extract categories from uploaded file.
        /// </summary>
        public static string CategoriesCantBeExtracted {
            get {
                return ResourceManager.GetString("CategoriesCantBeExtracted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to column.
        /// </summary>
        public static string Column {
            get {
                return ResourceManager.GetString("Column", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Combo box question with public key {0} can&apos;t be found.
        /// </summary>
        public static string ComboboxCannotBeFound {
            get {
                return ResourceManager.GetString("ComboboxCannotBeFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cover section can contain only questions, static texts and variables.
        /// </summary>
        public static string CoverPageCanContainsOnlyQuestionsAndStaticTextsAndVariables {
            get {
                return ResourceManager.GetString("CoverPageCanContainsOnlyQuestionsAndStaticTextsAndVariables", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cover section must be first section in questionnaire.
        /// </summary>
        public static string CoverPageMustBeFirstInQuestionnaire {
            get {
                return ResourceManager.GetString("CoverPageMustBeFirstInQuestionnaire", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Duplicated categories in rows: {0}.
        /// </summary>
        public static string Excel_Categories_Duplicated {
            get {
                return ResourceManager.GetString("Excel_Categories_Duplicated", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Some categories don&apos;t have a parent id.
        /// </summary>
        public static string Excel_Categories_Empty_ParentId {
            get {
                return ResourceManager.GetString("Excel_Categories_Empty_ParentId", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [{0}] Empty title.
        /// </summary>
        public static string Excel_Categories_Empty_Text {
            get {
                return ResourceManager.GetString("Excel_Categories_Empty_Text", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [{0}] Empty value.
        /// </summary>
        public static string Excel_Categories_Empty_Value {
            get {
                return ResourceManager.GetString("Excel_Categories_Empty_Value", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [{0}] Invalid numeric value.
        /// </summary>
        public static string Excel_Categories_Int_Invalid {
            get {
                return ResourceManager.GetString("Excel_Categories_Int_Invalid", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Categories set should have at least 2 categories.
        /// </summary>
        public static string Excel_Categories_Less_2_Options {
            get {
                return ResourceManager.GetString("Excel_Categories_Less_2_Options", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Categories set has more than {0} categories.
        /// </summary>
        public static string Excel_Categories_More_Than_Limit {
            get {
                return ResourceManager.GetString("Excel_Categories_More_Than_Limit", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [{0}] Text of category should be less than 250 characters.
        /// </summary>
        public static string Excel_Categories_Text_More_Than_250 {
            get {
                return ResourceManager.GetString("Excel_Categories_Text_More_Than_250", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No categories in file.
        /// </summary>
        public static string Excel_NoCategories {
            get {
                return ResourceManager.GetString("Excel_NoCategories", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed attempt to add group {0} into group {1}. But group {1} doesn&apos;t exist in document {2}.
        /// </summary>
        public static string FailedToAddGroup {
            get {
                return ResourceManager.GetString("FailedToAddGroup", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Trying to import template of deleted questionnaire.
        /// </summary>
        public static string ImportOfDeletedQuestionnaire {
            get {
                return ResourceManager.GetString("ImportOfDeletedQuestionnaire", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Attachment name is too long. Attachment name should be less than {0}.
        /// </summary>
        public static string ImportOptions_AttachmentNameTooLong {
            get {
                return ResourceManager.GetString("ImportOptions_AttachmentNameTooLong", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Duplicated category &apos;{0}&apos; with parent value &apos;{1}&apos;.
        /// </summary>
        public static string ImportOptions_DuplicateByTitleAndParentIds {
            get {
                return ResourceManager.GetString("ImportOptions_DuplicateByTitleAndParentIds", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Parent question &apos;{0}&apos; has {1} categories with value &apos;{2}&apos;.
        /// </summary>
        public static string ImportOptions_DuplicatedParentValues {
            get {
                return ResourceManager.GetString("ImportOptions_DuplicatedParentValues", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Empty value.
        /// </summary>
        public static string ImportOptions_EmptyValue {
            get {
                return ResourceManager.GetString("ImportOptions_EmptyValue", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Some column(s) are missing. Make sure that you upload file with required columns.
        /// </summary>
        public static string ImportOptions_MissingRequiredColumns {
            get {
                return ResourceManager.GetString("ImportOptions_MissingRequiredColumns", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid 32 bit integer value &apos;{0}&apos;. Value should be between -2147483647 and 2147483647.
        /// </summary>
        public static string ImportOptions_NotNumber {
            get {
                return ResourceManager.GetString("ImportOptions_NotNumber", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Parent question doesn&apos;t have an category with value &apos;{0}&apos;.
        /// </summary>
        public static string ImportOptions_ParentValueNotFound {
            get {
                return ResourceManager.GetString("ImportOptions_ParentValueNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Only tab-separated values (*.tab, *.txt, *.tsv) or excel (*.xslx, *.xls, *.ods) files are accepted.
        /// </summary>
        public static string ImportOptions_Tab_Or_Excel_Only {
            get {
                return ResourceManager.GetString("ImportOptions_Tab_Or_Excel_Only", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Title is too long. Title length should be less than {0}.
        /// </summary>
        public static string ImportOptions_TitleTooLong {
            get {
                return ResourceManager.GetString("ImportOptions_TitleTooLong", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Duplicated category &apos;{0}&apos; with value &apos;{1}&apos;.
        /// </summary>
        public static string ImportOptions_ValueIsNotUnique {
            get {
                return ResourceManager.GetString("ImportOptions_ValueIsNotUnique", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid title list.
        /// </summary>
        public static string InvalidFixedTitle {
            get {
                return ResourceManager.GetString("InvalidFixedTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid user info..
        /// </summary>
        public static string InvalidUserInfo {
            get {
                return ResourceManager.GetString("InvalidUserInfo", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Non empty values for fixed roster titles are required.
        /// </summary>
        public static string InvalidValueOfFixedTitle {
            get {
                return ResourceManager.GetString("InvalidValueOfFixedTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to One or more questionnaire item(s) with same ID {0} already exists..
        /// </summary>
        public static string ItemWithIdExistsAlready {
            get {
                return ResourceManager.GetString("ItemWithIdExistsAlready", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Lookup table with such id already exist..
        /// </summary>
        public static string LookupTableAlreadyExist {
            get {
                return ResourceManager.GetString("LookupTableAlreadyExist", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Lookup table with id {0} doen&apos;t have content.
        /// </summary>
        public static string LookupTableHasEmptyContent {
            get {
                return ResourceManager.GetString("LookupTableHasEmptyContent", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Lookup table with such id is absent..
        /// </summary>
        public static string LookupTableIsAbsent {
            get {
                return ResourceManager.GetString("LookupTableIsAbsent", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Lookup table with id {0} is missing.
        /// </summary>
        public static string LookupTableIsMissing {
            get {
                return ResourceManager.GetString("LookupTableIsMissing", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to File with no data cannot be loaded.
        /// </summary>
        public static string LookupTables_cant_has_empty_content {
            get {
                return ResourceManager.GetString("LookupTables_cant_has_empty_content", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Value {0} cannot be parsed as decimal number. Column {1}, row {2}..
        /// </summary>
        public static string LookupTables_data_value_cannot_be_parsed {
            get {
                return ResourceManager.GetString("LookupTables_data_value_cannot_be_parsed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Headers with the same name are not allowed.
        /// </summary>
        public static string LookupTables_duplicating_headers_are_not_allowed {
            get {
                return ResourceManager.GetString("LookupTables_duplicating_headers_are_not_allowed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Empty or invalid headers are not allowed.
        /// </summary>
        public static string LookupTables_empty_or_invalid_header_are_not_allowed {
            get {
                return ResourceManager.GetString("LookupTables_empty_or_invalid_header_are_not_allowed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Mandatory rowcode column is missing.
        /// </summary>
        public static string LookupTables_rowcode_column_is_mandatory {
            get {
                return ResourceManager.GetString("LookupTables_rowcode_column_is_mandatory", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Value {0} cannot be parsed as long integer number. Column {1}, row {2}..
        /// </summary>
        public static string LookupTables_rowcode_value_cannot_be_parsed {
            get {
                return ResourceManager.GetString("LookupTables_rowcode_value_cannot_be_parsed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Rowcode values must be unique.
        /// </summary>
        public static string LookupTables_rowcode_values_must_be_unique {
            get {
                return ResourceManager.GetString("LookupTables_rowcode_values_must_be_unique", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Too many columns in uploaded file. Max columns count is {0}.
        /// </summary>
        public static string LookupTables_too_many_columns {
            get {
                return ResourceManager.GetString("LookupTables_too_many_columns", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Lookup table contains {1} rows, which exceeds the limit on the number of rows in a lookup table ({0} rows)..
        /// </summary>
        public static string LookupTables_too_many_rows {
            get {
                return ResourceManager.GetString("LookupTables_too_many_rows", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Macro already exists..
        /// </summary>
        public static string MacroAlreadyExist {
            get {
                return ResourceManager.GetString("MacroAlreadyExist", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Macro is absent..
        /// </summary>
        public static string MacroIsAbsent {
            get {
                return ResourceManager.GetString("MacroIsAbsent", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to One or more question(s) with same ID {0} already exist:{1}{2}..
        /// </summary>
        public static string MoreThanOneQuestionWithSameId {
            get {
                return ResourceManager.GetString("MoreThanOneQuestionWithSameId", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to One or more sub-section(s) with same ID {0} already exist:{1}{2}..
        /// </summary>
        public static string MoreThanOneSubSectionWithSameId {
            get {
                return ResourceManager.GetString("MoreThanOneSubSectionWithSameId", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No categories for parent cascading question &apos;{0}&apos; found.
        /// </summary>
        public static string NoParentCascadingOptions {
            get {
                return ResourceManager.GetString("NoParentCascadingOptions", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You don&apos;t have permissions for changing this questionnaire.
        /// </summary>
        public static string NoPremissionsToEditQuestionnaire {
            get {
                return ResourceManager.GetString("NoPremissionsToEditQuestionnaire", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Old attachment id is empty and file is absent for attachment {0} in questionnaire {1}.
        /// </summary>
        public static string OldAttachmentIdIsEmpty {
            get {
                return ResourceManager.GetString("OldAttachmentIdIsEmpty", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Only QuestionnaireDocuments are supported for now.
        /// </summary>
        public static string OnlyQuestionnaireDocumentsAreSupported {
            get {
                return ResourceManager.GetString("OnlyQuestionnaireDocumentsAreSupported", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Category value should only have numeric characters.
        /// </summary>
        public static string OptionValuesShouldBeNumbers {
            get {
                return ResourceManager.GetString("OptionValuesShouldBeNumbers", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Found errors in provided file.
        /// </summary>
        public static string ProvidedFileHasErrors {
            get {
                return ResourceManager.GetString("ProvidedFileHasErrors", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Question with public key {0} can&apos;t be found..
        /// </summary>
        public static string QuestionCannotBeFound {
            get {
                return ResourceManager.GetString("QuestionCannotBeFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Question {0} is not a combo box.
        /// </summary>
        public static string QuestionIsNotCombobox {
            get {
                return ResourceManager.GetString("QuestionIsNotCombobox", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Categories were not found.
        /// </summary>
        public static string Questionnaire_CategoriesWereNotFound {
            get {
                return ResourceManager.GetString("Questionnaire_CategoriesWereNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Questionnaire already supports cover.
        /// </summary>
        public static string QuestionnaireAlreadySupportedCover {
            get {
                return ResourceManager.GetString("QuestionnaireAlreadySupportedCover", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Questionnaire item with id {0} can&apos;t be found..
        /// </summary>
        public static string QuestionnaireCantBeFound {
            get {
                return ResourceManager.GetString("QuestionnaireCantBeFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Questionnaire {0} of version {1} can&apos;t be found.
        /// </summary>
        public static string QuestionnaireRevisionCantBeFound {
            get {
                return ResourceManager.GetString("QuestionnaireRevisionCantBeFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Questionnaire already supports cover page..
        /// </summary>
        public static string QuestionnaireSuportedNewCover {
            get {
                return ResourceManager.GetString("QuestionnaireSuportedNewCover", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Questionnaire&apos;s title cannot be empty or contain whitespace only..
        /// </summary>
        public static string QuestionnaireTitleIsEmpty {
            get {
                return ResourceManager.GetString("QuestionnaireTitleIsEmpty", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Question type is not supported: {0}.
        /// </summary>
        public static string QuestionTypeIsNotSupported {
            get {
                return ResourceManager.GetString("QuestionTypeIsNotSupported", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Header {0} was not found in excel file.
        /// </summary>
        public static string RequiredHeaderWasNotFound {
            get {
                return ResourceManager.GetString("RequiredHeaderWasNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Roster cannot be pasted here..
        /// </summary>
        public static string RosterCantBePaste {
            get {
                return ResourceManager.GetString("RosterCantBePaste", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to row.
        /// </summary>
        public static string Row {
            get {
                return ResourceManager.GetString("Row", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Section cannot have more than {0} child items.
        /// </summary>
        public static string SectionCantHaveMoreThan_Items {
            get {
                return ResourceManager.GetString("SectionCantHaveMoreThan_Items", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Static Text cannot be pasted here..
        /// </summary>
        public static string StaticTextCantBePaste {
            get {
                return ResourceManager.GetString("StaticTextCantBePaste", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Sub-section with public key {0} can&apos;t be found..
        /// </summary>
        public static string SubSectionCantBeFound {
            get {
                return ResourceManager.GetString("SubSectionCantBeFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Section or sub-section cannot have more than {0} direct child items.
        /// </summary>
        public static string SubsectionCantHaveMoreThan_DirectChildren {
            get {
                return ResourceManager.GetString("SubsectionCantHaveMoreThan_DirectChildren", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Sub-section or roster depth cannot be higher than {0}..
        /// </summary>
        public static string SubSectionDepthLimit {
            get {
                return ResourceManager.GetString("SubSectionDepthLimit", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Found errors in excel file.
        /// </summary>
        public static string TranlationExcelFileHasErrors {
            get {
                return ResourceManager.GetString("TranlationExcelFileHasErrors", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} has invalid id at [{1}].
        /// </summary>
        public static string TranslationCel_A_lIsInvalid {
            get {
                return ResourceManager.GetString("TranslationCel_A_lIsInvalid", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} has invalid index at [{1}].
        /// </summary>
        public static string TranslationCellIndexIsInvalid {
            get {
                return ResourceManager.GetString("TranslationCellIndexIsInvalid", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} has invalid type [{1}].
        /// </summary>
        public static string TranslationCellTypeIsInvalid {
            get {
                return ResourceManager.GetString("TranslationCellTypeIsInvalid", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Excel file is empty - contains no worksheets.
        /// </summary>
        public static string TranslationFileIsEmpty {
            get {
                return ResourceManager.GetString("TranslationFileIsEmpty", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to extract translations from uploaded file.
        /// </summary>
        public static string TranslationsCantBeExtracted {
            get {
                return ResourceManager.GetString("TranslationsCantBeExtracted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Worksheet with translations not found.
        /// </summary>
        public static string TranslationWorksheetIsMissing {
            get {
                return ResourceManager.GetString("TranslationWorksheetIsMissing", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unknown item type. Paste failed..
        /// </summary>
        public static string UnknownTypeCantBePaste {
            get {
                return ResourceManager.GetString("UnknownTypeCantBePaste", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to User {0} already exists in share list..
        /// </summary>
        public static string UserIsInTheList {
            get {
                return ResourceManager.GetString("UserIsInTheList", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to User {0} is an owner of this questionnaire. Please, input another email..
        /// </summary>
        public static string UserIsOwner {
            get {
                return ResourceManager.GetString("UserIsOwner", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Fixed set of items roster value should only have numeric characters.
        /// </summary>
        public static string ValueOfFixedTitleCantBeParsed {
            get {
                return ResourceManager.GetString("ValueOfFixedTitleCantBeParsed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Variable cannot be pasted here..
        /// </summary>
        public static string VariableCantBePaste {
            get {
                return ResourceManager.GetString("VariableCantBePaste", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Variable with id {0} was not found in questionnaire {1}.
        /// </summary>
        public static string VariableWithIdWasNotFound {
            get {
                return ResourceManager.GetString("VariableWithIdWasNotFound", resourceCulture);
            }
        }
    }
}
