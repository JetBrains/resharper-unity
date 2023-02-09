using System;
using JetBrains.Application.I18n;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api
{
    [SolutionComponent]
    public class UnityTypesProvider
    {
        private Lazy<UnityTypes> myTypes;

        public UnityTypesProvider()
        {
            CultureContextComponent.Instance.WhenNotNull(Lifetime.Eternal, (lifetime, instance) =>
            {
                lifetime.Bracket(() =>
                    {
                        myTypes = Lazy.Of(() =>
                        {
                            var apiXml = new ApiXml();
                            return apiXml.LoadTypes(instance.Culture.Value);
                        }, true);
                    },
                    () =>
                    {
                        myTypes = null;
                    });
            });
        }

        public UnityTypes Types => myTypes.Value;
    }
}