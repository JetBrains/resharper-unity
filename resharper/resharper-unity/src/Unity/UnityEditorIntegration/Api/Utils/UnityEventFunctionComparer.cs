using System;
using System.Collections.Generic;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api.Utils
{
    // Sort event functions, mostly alphabetically, but with some commonly used messages at the top
    internal class UnityEventFunctionComparer : IComparer<string>
    {
        private static readonly OrderedHashSet<string> ourSpecialNames = new OrderedHashSet<string>();

        static UnityEventFunctionComparer()
        {
            ourSpecialNames.Add("Awake");
            ourSpecialNames.Add("Reset");
            ourSpecialNames.Add("Start");
            ourSpecialNames.Add("Update");
            ourSpecialNames.Add("FixedUpdate");
            ourSpecialNames.Add("LateUpdate");
            ourSpecialNames.Add("OnEnable");
            ourSpecialNames.Add("OnDisable");
            ourSpecialNames.Add("OnDestroy");
            ourSpecialNames.Add("OnGUI");
        }

        public int Compare(string x, string y)
        {
            var xi = ourSpecialNames.IndexOf(x);
            var yi = ourSpecialNames.IndexOf(y);
            // -1 -> x is less than y, so goes to top
            if (xi == -1 && yi > -1)
                return 1;
            if (xi > -1 && yi == -1)
                return -1;
            if (xi == -1 && yi == -1)
                return string.Compare(x, y, StringComparison.InvariantCulture);
            return xi > yi ? 1 : (xi < yi ? -1 : 0);
        }
    }
}