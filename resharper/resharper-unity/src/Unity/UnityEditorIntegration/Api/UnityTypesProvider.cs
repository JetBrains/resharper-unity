using System;
using JetBrains.ProjectModel;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api
{
    [SolutionComponent]
    public class UnityTypesProvider
    {
        private readonly Lazy<UnityTypes> myTypes;

        public UnityTypesProvider()
        {
            myTypes = Lazy.Of(() =>
            {
                var apiXml = new ApiXml();
                return apiXml.LoadTypes();
            }, true);
        }

        public UnityTypes Types => myTypes.Value;
    }
}