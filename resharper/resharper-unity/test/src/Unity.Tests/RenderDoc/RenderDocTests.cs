using System;
using JetBrains.ReSharper.Plugins.Unity.Shaders.RenderDoc;

namespace JetBrains.ReSharper.Plugins.Tests.RenderDoc;

public class RenderDocTests
{
    public void TestCaptures()
    {
        var config = new RenderDocConfig();
        var client = new RenderDocClient(config);
        foreach (var capture in client.GetCaptures())
        {
            Console.WriteLine(capture.Name);
        }
    }
}