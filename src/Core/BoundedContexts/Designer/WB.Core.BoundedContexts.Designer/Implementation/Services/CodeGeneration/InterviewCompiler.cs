﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using WB.Core.BoundedContexts.Designer.Services.CodeGeneration;
using WB.Core.Infrastructure.FileSystem;
using WB.Core.SharedKernels.DataCollection;

namespace WB.Core.BoundedContexts.Designer.Implementation.Services.CodeGeneration
{
    public class RoslynCompiler : IDynamicCompiler
    {
        private readonly IDynamicCompilerSettings compilerSettings;
        private readonly IFileSystemAccessor fileSystemAccessor;

        public RoslynCompiler(IDynamicCompilerSettings compilerSettings, IFileSystemAccessor fileSystemAccessor)
        {
            this.compilerSettings = compilerSettings;
            this.fileSystemAccessor = fileSystemAccessor;
        }
        
        public EmitResult GenerateAssemblyAsString(Guid templateId, Dictionary<string, string> generatedClasses,
            string[] referencedPortableAssemblies,
            out string generatedAssembly)
        {
            IEnumerable<SyntaxTree> syntaxTrees = generatedClasses.Select(
                    generatedClass => SyntaxFactory.ParseSyntaxTree(generatedClass.Value, path: generatedClass.Key))
                    .ToArray();

            List<PortableExecutableReference> metadataReferences =
                compilerSettings.DefaultReferencedPortableAssemblies.Select(
                    defaultReferencedPortableAssembly =>
                        AssemblyMetadata.CreateFromFile(
                            fileSystemAccessor.CombinePath(compilerSettings.PortableAssembliesPath,
                                defaultReferencedPortableAssembly)).GetReference()).ToList();

            metadataReferences.AddRange(
                referencedPortableAssemblies.Select(
                    defaultReferencedPortableAssembly =>
                        AssemblyMetadata.CreateFromFile(
                            fileSystemAccessor.CombinePath(compilerSettings.PortableAssembliesPath,
                                defaultReferencedPortableAssembly)).GetReference()));

            metadataReferences.Add(AssemblyMetadata.CreateFromFile(typeof (Identity).Assembly.Location).GetReference());

            Guid uniqueAssemblySuffix = Guid.NewGuid();


            CSharpCompilation compilation = CSharpCompilation.Create(
                String.Format("rules-{0}-{1}.dll", templateId, uniqueAssemblySuffix),
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, 
                    checkOverflow: true, 
                    optimizationLevel: OptimizationLevel.Release, 
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default),
                syntaxTrees: syntaxTrees,
                references: metadataReferences);

            EmitResult compileResult;
            generatedAssembly = string.Empty;
           
            using (var stream = new MemoryStream())
            {
                compileResult = compilation.Emit(stream);

                if (compileResult.Success)
                {
                    stream.Position = 0;
                    generatedAssembly = Convert.ToBase64String(stream.ToArray());
                }
            }

            return compileResult;
        }
    }
}