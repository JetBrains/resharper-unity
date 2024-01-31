using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions
{
    [ContextActionGroup(Id = GroupID, Name = GroupID)]
    public static class CSharpUnityContextActions
    {
        public const string GroupID = CSharpContextActions.GroupID + " (Unity)";
    }
}