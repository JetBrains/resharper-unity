using System;
using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.Metadata.Utils;
using JetBrains.ReSharper.Psi.ExtensionsAPI.ExternalAnnotations;
using JetBrains.TestFramework.Utils;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Tests
{
    [ShellComponent]
    public class AnnotationsLoader : IExternalAnnotationsFileProvider
    {
        private readonly OneToSetMap<string, FileSystemPath> myAnnotations;

        public AnnotationsLoader()
        {
            myAnnotations = new OneToSetMap<string, FileSystemPath>(StringComparer.OrdinalIgnoreCase);
            var testDataPathBase = TestUtil.GetTestDataPathBase(GetType().Assembly);
            var annotationsPath = testDataPathBase.Parent.Parent / "src" / "resharper-unity" / "annotations";
            Assertion.Assert(annotationsPath.ExistsDirectory, $"Cannot find annotations: {annotationsPath}");
            var annotationFiles = annotationsPath.GetChildFiles();
            foreach (var annotationFile in annotationFiles)
            {
                myAnnotations.Add(annotationFile.NameWithoutExtension, annotationFile);
            }
        }

        public IEnumerable<FileSystemPath> GetAnnotationsFiles(AssemblyNameInfo assemblyName = null, FileSystemPath assemblyLocation = null)
        {
            if (assemblyName == null)
                return myAnnotations.Values;
            return myAnnotations[assemblyName.Name];
        }
    }
}