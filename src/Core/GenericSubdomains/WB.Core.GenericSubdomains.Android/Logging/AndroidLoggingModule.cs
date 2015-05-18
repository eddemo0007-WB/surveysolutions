﻿using Ninject.Modules;

using WB.Core.GenericSubdomains.Utils.Services;

namespace WB.Core.GenericSubdomains.Android.Logging
{
    public class AndroidLoggingModule : NinjectModule
    {
        public override void Load()
        {
            this.Bind<ILogger>().To<FileLogger>().InSingletonScope().WithConstructorArgument("appName", "WBCapi");
        }
    }
}