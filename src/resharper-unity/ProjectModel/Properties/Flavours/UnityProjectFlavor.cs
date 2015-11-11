using System;
using JetBrains.ProjectModel.Properties;

namespace JetBrains.ReSharper.Plugins.Unity.ProjectModel.Properties.Flavours
{
    [ProjectFlavor]
    public class UnityProjectFlavor : IProjectFlavor
    {
        public static Guid UnityProjectFlavorGuid = new Guid("{E097FAD1-6243-4DAD-9C02-E9B9EFC3FFC1}");

        public Guid Guid => UnityProjectFlavorGuid;
        public string FlavorName => "Unity Project";
    }
}