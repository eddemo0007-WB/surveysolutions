﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Machine.Specifications;
using Main.Core.Documents;
using Main.Core.Entities.SubEntities;
using Main.Core.Entities.SubEntities.Question;
using WB.Core.BoundedContexts.Headquarters.AssignmentImport.Parser;
using WB.Core.GenericSubdomains.Portable.Implementation.ServiceVariables;
using WB.Core.SharedKernels.DataCollection.ValueObjects;

namespace WB.Tests.Unit.SharedKernels.SurveyManagement.PreloadedDataServiceTests
{
    internal class when_GetAvalibleIdListForParent_is_called_for_file_corresponding_to_roster_triggered_by_numeric_question : PreloadedDataServiceTestContext
    {
        Establish context = () =>
        {
            questionnaireDocument =
                CreateQuestionnaireDocumentWithOneChapter(
                    new NumericQuestion() { PublicKey = rosterSizeQuestionId, QuestionType = QuestionType.Numeric, StataExportCaption = rosterSizeQuestionVariableName, IsInteger = true},
                    new Group("Roster Group")
                    {
                        IsRoster = true,
                        RosterSizeSource = RosterSizeSourceType.Question,
                        PublicKey = rosterGroupId,
                        RosterSizeQuestionId = rosterSizeQuestionId
                    });

            importDataParsingService = CreatePreloadedDataService(questionnaireDocument);
        };

        Because of =
            () =>
                result =
                    importDataParsingService.GetAvailableIdListForParent(
                        CreatePreloadedDataByFile(new string[] { ServiceColumns.InterviewId, rosterSizeQuestionVariableName }, new string[][] { new string[] { "1","3" } },
                            questionnaireDocument.Title), new ValueVector<Guid> { rosterSizeQuestionId }, new []{"1"}, new PreloadedDataByFile[0]);

        It should_return_array_with_0_1_2= () =>
            result.ShouldEqual(new []{0, 1,2}); 

        private static ImportDataParsingService importDataParsingService;
        private static QuestionnaireDocument questionnaireDocument;
        private static int[] result;
        private static Guid rosterGroupId = Guid.NewGuid();
        private static Guid rosterSizeQuestionId = Guid.NewGuid();
        private static string rosterSizeQuestionVariableName = "var";
    }
}
