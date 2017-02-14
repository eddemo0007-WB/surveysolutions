using System;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Ninject;
using Ninject.Modules;
using Prometheus.Advanced;
using WB.Core.BoundedContexts.Headquarters.Services.WebInterview;
using WB.Core.Infrastructure.Aggregates;
using WB.Core.Infrastructure.Implementation.Aggregates;
using WB.Infrastructure.Native.Storage;
using WB.UI.Headquarters.API.WebInterview.Pipeline;
using WB.UI.Headquarters.API.WebInterview.Services;

namespace WB.UI.Headquarters.API.WebInterview
{
    public class WebInterviewNinjectModule : NinjectModule
    {
        public override void Load()
        {
            GlobalHost.DependencyResolver = new NinjectDependencyResolver(this.Kernel);
            var pipiline = GlobalHost.DependencyResolver.Resolve<IHubPipeline>();

            pipiline.AddModule(new SignalrErrorHandler());
            pipiline.AddModule(new PlainSignalRTransactionManager());
            pipiline.AddModule(new WebInterviewAllowedModule());
            pipiline.AddModule(new WebInterviewStateManager());
            pipiline.AddModule(new WebInterviewConnectionsCounter());

            this.Bind<IWebInterviewNotificationService>().To<WebInterviewNotificationService>();
            this.Bind<IConnectionLimiter>().To<ConnectionLimiter>();
            this.Rebind<IEventSourcedAggregateRootRepository>().To<EventSourcedAggregateRootRepositoryWithWebCache>().InSingletonScope();
            this.Rebind<IAggregateRootCacheCleaner>().To<EventSourcedAggregateRootRepositoryWithWebCache>().InSingletonScope();

            DefaultCollectorRegistry.Instance.RegisterOnDemandCollectors(new IOnDemandCollector[]
            {
                new DotNetStatsCollector ()
            });

            this.Bind<IHubContext>()
                .ToMethod(context => GlobalHost.ConnectionManager.GetHubContext<WebInterview>())
                .InSingletonScope()
                .Named(@"WebInterview");
        }
    }
}