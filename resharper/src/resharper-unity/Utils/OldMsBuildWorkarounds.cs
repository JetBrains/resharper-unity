using JetBrains.DocumentModel;

namespace JetBrains.ReSharper.Plugins.Unity.Utils
{
    public static class OldMsBuildWorkarounds
    {
        public static DocumentRange CreateDocumentRange(DocumentOffset startOffset, DocumentOffset endOffset)
        {
#if OLD_MSBUILD
            return new DocumentRange(ref startOffset, ref endOffset);
#else
            return new DocumentRange(startOffset, endOffset);
#endif      
        }

        public static bool RangeContains(DocumentRange range, DocumentOffset offset)
        {
#if OLD_MSBUILD
            return range.Contains(ref offset);
#else            
            return range.Contains(offset);
#endif
        }
        
        public static bool RangeContains(DocumentRange range, DocumentRange range2)
        {
#if OLD_MSBUILD
            return range.Contains(ref range2);
#else            
            return range.Contains(range2);
#endif
        }
    }
}