using System;
using JetBrains.Application.I18n;
using JetBrains.ProjectModel;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api
{
    [SolutionComponent]
    public class UnityTypesProvider
    {
        private readonly Lazy<UnityTypes> myTypes;

        public UnityTypesProvider(CultureContextComponent cultureContextComponent)
        {
            myTypes = Lazy.Of(() =>
            {
                var apiXml = new ApiXml();
                return apiXml.LoadTypes(cultureContextComponent.Culture.Value);
            }, true);
        }

        public UnityTypes Types => myTypes.Value;
    }
}