﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Machine.Specifications;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using Main.Core.Entities.SubEntities.Question;
using WB.Core.BoundedContexts.Headquarters.AssignmentImport.Parser;
using WB.Core.SharedKernels.DataCollection.DataTransferObjects.Preloading;
using WB.Tests.Abc;

namespace WB.Tests.Unit.SharedKernels.SurveyManagement.PreloadedDataServiceTests
{
    internal class when_CreatePreloadedDataDtoFromSampleData_is_called : PreloadedDataServiceTestContext
    {
        Establish context = () =>
        {
            questionnaireDocument =
                CreateQuestionnaireDocumentWithOneChapter(
                    new NumericQuestion()
                    {
                        StataExportCaption = "nq1",
                        QuestionType = QuestionType.Numeric,
                        PublicKey = Guid.NewGuid()
                    },
                    new TextQuestion()
                    {
                        StataExportCaption = "tq1",
                        QuestionType = QuestionType.Text,
                        PublicKey = Guid.NewGuid()
                    },
                    Create.Entity.FixedRoster(rosterId: rosterGroupId,
                        obsoleteFixedTitles: new[] {"a"},
                        children: new IComposite[]
                        {
                            new NumericQuestion()
                            {
                                StataExportCaption = "nq2",
                                QuestionType = QuestionType.Numeric,
                                PublicKey = Guid.NewGuid()
                            }
                        }));

            importDataParsingService = CreatePreloadedDataService(questionnaireDocument);
        };

        Because of =
            () =>
                result =
                    importDataParsingService.CreatePreloadedDataDtoFromAssignmentData(CreatePreloadedDataByFile(new[] { "Id", "nq1" }, 
                    new[] { new[] { "1", "2" } }, 
                    "some file name"));

        It should_return_not_null_result = () =>
            result.ShouldNotBeNull();

        It should_result_has_1_items = () =>
           result.Length.ShouldEqual(1);

        private static ImportDataParsingService importDataParsingService;
        private static QuestionnaireDocument questionnaireDocument;
        private static PreloadedDataRecord[] result;
        private static Guid rosterGroupId = Guid.NewGuid();
    }
}
