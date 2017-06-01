using System;

namespace WB.Core.SharedKernels.DataCollection.ExpressionStorage
{
    public interface IInterviewLevel
    {
        Func<bool> GetConditionExpression(Identity entitIdentity);
        Func<bool>[] GetValidationExpressions(Identity entitIdentity);
        Func<IInterviewLevel, bool> GetLinkedQuestionFilter(Identity identity);
        Func<object> GetVariableExpression(Identity identity);
        Func<int, bool> GetCategoricalFilter(Identity identity);
    }
}