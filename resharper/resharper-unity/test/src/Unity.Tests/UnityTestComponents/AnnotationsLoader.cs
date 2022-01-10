using System;
using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.Diagnostics;
using JetBrains.Metadata.Utils;
using JetBrains.ReSharper.Psi.ExtensionsAPI.ExternalAnnotations;
using JetBrains.TestFramework.Utils;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Tests
{
    [ShellComponent]
    public class AnnotationsLoader : IExternalAnnotationsFileProvider
    {
        private readonly OneToSetMap<string, VirtualFileSystemPath> myAnnotations;

        public AnnotationsLoader()
        {
            myAnnotations = new OneToSetMap<string, VirtualFileSystemPath>(StringComparer.OrdinalIgnoreCase);
            var testDataPathBase = TestUtil.GetTestDataPathBase(GetType().Assembly).ToVirtualFileSystemPath();
            var annotationsPath = testDataPathBase.Parent.Parent / "src" / "Unity" / "annotations";
            Assertion.Assert(annotationsPath.ExistsDirectory, $"Cannot find annotations: {annotationsPath}");
            var annotationFiles = annotationsPath.GetChildFiles();
            foreach (var annotationFile in annotationFiles)
            {
                myAnnotations.Add(annotationFile.NameWithoutExtension, annotationFile);
            }
        }

        public IEnumerable<VirtualFileSystemPath> GetAnnotationsFiles(AssemblyNameInfo assemblyName = null, VirtualFileSystemPath assemblyLocation = null)
        {
            if (assemblyName == null)
                return myAnnotations.Values;
            return myAnnotations[assemblyName.Name];
        }
    }
}