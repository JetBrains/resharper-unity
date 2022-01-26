using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

// ReSharper disable InconsistentNaming
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnassignedField.Global
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable MemberCanBePrivate.Global
#pragma warning disable 649

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.UnityEditorIntegration.Packages
{
    public class ManifestJson
    {
        public Dictionary<string, string> dependencies;

        public static Dictionary<string, Version> FromJson(string json)
        {
            return JsonConvert.DeserializeObject<ManifestJson>(json).dependencies
                .Select(a=>
                {
                    var versionString = a.Value.Replace("-preview", string.Empty);

                    if (Version.TryParse(versionString, out var result))
                    {
                        return new KeyValuePair<string, Version>(a.Key, result);
                    }
                    return new KeyValuePair<string, Version>(a.Key, null);
                }).ToDictionary(x=> x.Key, x=>x.Value);
        }
    }
}