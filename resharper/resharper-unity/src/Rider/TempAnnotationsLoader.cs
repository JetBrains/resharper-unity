using System;
using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.Metadata.Utils;
using JetBrains.ReSharper.Psi.ExtensionsAPI.ExternalAnnotations;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    // Temporary workaround for RIDER-13547
    [ShellComponent]
    public class TempAnnotationsLoader : IExternalAnnotationsFileProvider
    {
        private readonly OneToSetMap<string, FileSystemPath> myAnnotations;

        public TempAnnotationsLoader()
        {
            myAnnotations = new OneToSetMap<string, FileSystemPath>(StringComparer.OrdinalIgnoreCase);
            var annotationsPath = GetType().Assembly.GetPath().Directory / "Extensions" / "JetBrains.Unity" / "annotations";
            var annotationFiles = annotationsPath.GetChildFiles();
            foreach (var annotationFile in annotationFiles)
                myAnnotations.Add(annotationFile.NameWithoutExtension, annotationFile);
        }

        public IEnumerable<FileSystemPath> GetAnnotationsFiles(AssemblyNameInfo assemblyName = null, FileSystemPath assemblyLocation = null)
        {
            if (assemblyName == null)
                return myAnnotations.Values;
            return myAnnotations[assemblyName.Name];
        }
    }
}