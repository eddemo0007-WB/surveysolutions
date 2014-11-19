﻿using System;
using System.Linq;
using System.Reflection;
using Microsoft.Practices.ServiceLocation;
using WB.Core.GenericSubdomains.Logging;
using WB.Core.Infrastructure.FileSystem;
using WB.Core.SharedKernels.DataCollection.Exceptions;
using WB.Core.SharedKernels.DataCollection.Implementation.Accessors;

namespace WB.Core.SharedKernels.DataCollection.Implementation.Providers
{
    internal class InterviewExpressionStatePrototypeProvider : IInterviewExpressionStatePrototypeProvider
    {
        private static ILogger Logger
        {
            get { return ServiceLocator.Current.GetInstance<ILogger>(); }
        }

        private readonly IQuestionnaireAssemblyFileAccessor questionnareAssemblyFileAccessor;
        private readonly IFileSystemAccessor fileSystemAccessor;

        public InterviewExpressionStatePrototypeProvider(IQuestionnaireAssemblyFileAccessor questionnareAssemblyFileAccessor, IFileSystemAccessor fileSystemAccessor)
        {
            this.questionnareAssemblyFileAccessor = questionnareAssemblyFileAccessor;
            this.fileSystemAccessor = fileSystemAccessor;
        }

        public IInterviewExpressionState GetExpressionState(Guid questionnaireId, long questionnaireVersion)
        {
            string assemblyFile = this.questionnareAssemblyFileAccessor.GetFullPathToAssembly(questionnaireId, questionnaireVersion);

            if (!fileSystemAccessor.IsFileExists(assemblyFile))
            {
                Logger.Fatal(String.Format("Assembly was not found. Questionnaire={0}, version={1}, search={2}", 
                    questionnaireId, questionnaireVersion, assemblyFile));
                throw new InterviewException("Interview loading error. Code EC0002");
            }

            try
            {
                //path is cached
                //if assembly was loaded from this path it won't be loaded again 
                var compiledAssembly = Assembly.LoadFrom(assemblyFile);
                Type interviewExpressionStateType = compiledAssembly.GetTypes().
                    SingleOrDefault(type => !(type.IsAbstract || type.IsGenericTypeDefinition || type.IsInterface) && type.GetInterfaces().Contains(typeof(IInterviewExpressionState)));

                if (interviewExpressionStateType == null)
                    throw new Exception("Type implementing IInterviewExpressionState was not found");

                try
                {
                    var interviewExpressionState = Activator.CreateInstance(interviewExpressionStateType) as IInterviewExpressionState;

                    return interviewExpressionState;
                }
                catch (Exception e)
                {
                    Logger.Fatal("Error on activating interview expression state. Cannot cast to created object to IInterviewExpressionState", e);
                    return null;
                }
            }
            catch (Exception exception)
            {
                Logger.Fatal("Error on assembly loading", exception);
                if (exception.InnerException != null)
                    Logger.Fatal("Error on assembly loading", exception.InnerException);

                //hide original one
                throw new InterviewException("Interview loading error. Code EC0001");
            }
        }
    }
}
