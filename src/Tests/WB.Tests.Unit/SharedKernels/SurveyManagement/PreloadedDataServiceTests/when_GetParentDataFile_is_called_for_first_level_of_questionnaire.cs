﻿using System;
using System.Linq;
using Machine.Specifications;
using Main.Core.Documents;
using WB.Core.BoundedContexts.Headquarters.AssignmentImport.Parser;
using WB.Tests.Abc;

namespace WB.Tests.Unit.SharedKernels.SurveyManagement.PreloadedDataServiceTests
{
    internal class when_GetParentDataFile_is_called_for_first_level_of_questionnaire : PreloadedDataServiceTestContext
    {
        Establish context = () =>
        {
            questionnaireDocument =
                CreateQuestionnaireDocumentWithOneChapter(Create.Entity.FixedRoster(rosterId: rosterGroupId, title: "Roster Group", variable: "roster",
                        obsoleteFixedTitles: new[] { "1" }));

            importDataParsingService = CreatePreloadedDataService(questionnaireDocument);
        };

        Because of =
           () =>
               result =
                   importDataParsingService.GetParentDataFile("roster",  Create.Entity.PreloadedData(
                           CreatePreloadedDataByFile(null, null, "roster"), 
                           CreatePreloadedDataByFile(null, null, questionnaireDocument.Title)));

        It should_return_not_null_result = () =>
            result.ShouldNotBeNull();

        It should_result_filename_be_equal_to_top_level_file = () =>
            result.FileName.SequenceEqual(questionnaireDocument.Title);

        private static ImportDataParsingService importDataParsingService;
        private static QuestionnaireDocument questionnaireDocument;
        private static PreloadedDataByFile result;
        private static Guid rosterGroupId = Guid.NewGuid();
    }
}
