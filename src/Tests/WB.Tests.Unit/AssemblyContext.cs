﻿using Machine.Specifications;
using Microsoft.Practices.ServiceLocation;
using Moq;
using Ncqrs;
using WB.Core.BoundedContexts.Designer.Implementation.Services;
using WB.Core.BoundedContexts.Designer.Services;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.Files.Implementation.FileSystem;
using WB.Core.Infrastructure.FileSystem;
using WB.Core.SharedKernels.SurveySolutions.Implementation.Services;
using WB.Core.SharedKernels.SurveySolutions.Services;

namespace WB.Tests.Unit
{
    internal class AssemblyContext : IAssemblyContext
    {
        public void OnAssemblyStart()
        {
            SetupServiceLocator();
        }

        public void OnAssemblyComplete() {}

        public static void SetupServiceLocator()
        {
            if (ServiceLocator.IsLocationProviderSet)
                return;

            var serviceLocator = Stub<IServiceLocator>.WithNotEmptyValues;

            ServiceLocator.SetLocatorProvider(() => serviceLocator);

            Setup.InstanceToMockedServiceLocator<ISubstitutionService>(new SubstitutionService());
            Setup.InstanceToMockedServiceLocator<IKeywordsProvider>(new KeywordsProvider(new SubstitutionService()));
            Setup.InstanceToMockedServiceLocator<IFileSystemAccessor>(new FileSystemIOAccessor());

            Setup.InstanceToMockedServiceLocator<ILogger>(Mock.Of<ILogger>());
            Setup.InstanceToMockedServiceLocator<IClock>(Mock.Of<IClock>());
        }
    }
}
