using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

namespace ApiParser
{
    public static class Program
    {
        private static int ourProgress;

        public static void Main(string[] args)
        {
            var progPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var dataPath = Path.Combine(progPath, @"Unity\Editor\Data");
            var monoPath = Path.Combine(dataPath, @"Mono\lib\mono\unity");
            var basePath = Path.Combine(dataPath, @"Managed");
            TypeResolver.AddAssembly(typeof(string).Assembly);
            TypeResolver.AddAssembly(Assembly.LoadFrom(Path.Combine(basePath, @"UnityEngine.dll")));
            TypeResolver.AddAssembly(Assembly.LoadFrom(Path.Combine(monoPath, @"UnityScript.dll")));
            TypeResolver.AddAssembly(Assembly.LoadFrom(Path.Combine(basePath, @"UnityEditor.dll")));

            Console.Clear();
            var path = Path.Combine(dataPath, @"Documentation\en\ScriptReference");

            var parser = new ApiParser(path);
            parser.Progress += ( s, e ) =>
            {
                if (e.Percent <= ourProgress) return;

                ourProgress = e.Percent;
                Console.SetCursorPosition( 0, 0 );
                Console.WriteLine( "{0,5} / {1,5} ({2,3}%)", e.Current, e.Total, e.Percent );
            };

            parser.ParseFolder();

            using (var writer = new XmlTextWriter(@"api.xml", Encoding.UTF8) {Formatting = Formatting.Indented})
            {
                parser.ExportTo(writer);
            }

            // Console.WriteLine( "Press <Enter> key to continue..." );
            // Console.ReadLine();
        }
    }
}
