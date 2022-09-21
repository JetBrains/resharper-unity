using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.Json.Psi;
using JetBrains.ReSharper.Plugins.Json.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Files;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.InputActions.Psi.Caches
{
    [PsiComponent]
    public class InputActionCache : SimpleICache<List<InputActionCacheItem>>
    {
        public InputActionCache(Lifetime lifetime,
            IShellLocks shellLocks,
            IPersistentIndexManager persistentIndexManager)
            : base(lifetime, shellLocks, persistentIndexManager, InputActionCacheItem.Marshaller)
        {
        }

        public override object? Build(IPsiSourceFile sourceFile, bool isStartup)
        {
            if (!IsApplicable(sourceFile))
                return null;
            
            var file = sourceFile.GetDominantPsiFile<JsonNewLanguage>() as IJsonNewFile;
            var rootObject = file?.GetRootObject();

            var results = new List<InputActionCacheItem>();

            if (rootObject != null)
            {
                foreach (var member in rootObject.MembersEnumerable)
                {
                    if (member.Key == "maps")
                    {
                        if (member.Value is IJsonNewArray val)
                        {
                            foreach (var jsonNewValue in val.Values)
                            {
                                var jsonNewObject = (IJsonNewObject)jsonNewValue;
                                foreach (var newMember in jsonNewObject.MembersEnumerable)
                                {
                                    if (newMember is { Key: "actions" })
                                    {
                                        if (newMember.Value is IJsonNewArray actions)
                                        {
                                            foreach (var actionsValue in actions.Values)
                                            {
                                                foreach (var actionMembers in ((IJsonNewObject) actionsValue).Members)
                                                {
                                                    if (actionMembers.Key == "name")
                                                    {
                                                        var nameProperty = (IJsonNewLiteralExpression)actionMembers.Value;
                                                        results.Add(new InputActionCacheItem(nameProperty.GetStringValue(),
                                                            nameProperty.GetTreeStartOffset().Offset));
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            return results;
        }
        
        protected override bool IsApplicable(IPsiSourceFile sf)
        {
            return base.IsApplicable(sf) && sf.IsInputActions() && sf.IsLanguageSupported<JsonNewLanguage>();
        }

        public bool ContainsName(string name)
        {
            return Map.SelectMany(item => item.Value)
                .Any(inputActionCacheItem => inputActionCacheItem.Name == name);
        }
    }
}