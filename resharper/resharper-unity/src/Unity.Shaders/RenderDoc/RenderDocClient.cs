#nullable enable
using System.Collections.Generic;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.RenderDoc;

public class RenderDocClient
{
    private RenderDocConfig myConfig;
    
    public RenderDocClient(RenderDocConfig config)
    {
        myConfig = config;
    }

    public IEnumerable<RenderDocCapture> GetCaptures()
    {
        yield break;
    }

    public void Replay(string rdcPath)
    {
        
    }
}