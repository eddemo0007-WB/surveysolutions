﻿using Ninject.Modules;
using WB.Core.SharedKernels.Enumerator.Services;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure;
using WB.Infrastructure.Shared.Enumerator.Internals.MapService;
using WB.UI.Tester.Implementation.Services;

namespace WB.UI.Tester.Ninject
{
    public class TesterUIModule : NinjectModule
    {
        public override void Load()
        {
            this.Bind<IViewModelNavigationService>().To<ViewModelNavigationService>();

            this.Bind<IMapService>().To<MapService>().WithConstructorArgument("urlToCheckMaps", "https://download.mysurvey.solutions");

#if EXCLUDEEXTENSIONS
            this.Bind<IAreaEditService>().To<WB.UI.Shared.Enumerator.CustomServices.AreaEditor.DummyAreaEditService>();
#else
            this.Bind<IAreaEditService>().To<WB.UI.Shared.Extensions.CustomServices.AreaEditor.AreaEditService>();
#endif
        }
    }
}