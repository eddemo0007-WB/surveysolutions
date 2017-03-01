using System;

using Machine.Specifications;

using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.V5.CustomFunctions;
using WB.Tests.Abc;

namespace WB.Tests.Unit.SharedKernels.DataCollection.YesNoAnswersTests
{
    internal class when_indexing_YesNoAnswers_with_absent_option_code1
    {
        Establish context = () =>
        {
            answers = Create.Entity.YesNoAnswers(allCodes, new YesNoAnswersOnly(selectedYes, selectedNo));
        };

        Because of = () =>
            exception = Catch.Only<IndexOutOfRangeException>(() => answers[100].IsNo());
        
        It should_return_true = () =>
            exception.ShouldNotBeNull();

        private static YesNoAnswers answers;
        private static Exception exception;
        private static readonly decimal[] allCodes = new decimal[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        private static readonly decimal[] selectedYes = new decimal[] { 1, 2, 8 };
        private static readonly decimal[] selectedNo = new decimal[] { 10, 6, 3 };
    }
}