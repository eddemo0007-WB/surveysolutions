using Moq;
using WB.Core.BoundedContexts.Supervisor.Synchronization;
using WB.Core.Infrastructure.PlainStorage;

namespace WB.Core.BoundedContexts.Supervisor.Tests.Synchronization.QuestionnaireSynchronizerTests
{
    internal class HeadquartersPullContextTestable : HeadquartersPullContext
    {
        private int pushedErrorsCount = 0;
        public int PushedErrorsCount { get { return this.pushedErrorsCount; } }
        public HeadquartersPullContextTestable()
            : base(Mock.Of<IPlainStorageAccessor<SynchronizationStatus>>()) { }

        public override void PushError(string message)
        {
            base.PushError(message);
            this.pushedErrorsCount++;
        }
    }
}