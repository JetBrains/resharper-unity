using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

using JetBrains.Platform.RdFramework;
using JetBrains.Platform.RdFramework.Base;
using JetBrains.Platform.RdFramework.Impl;
using JetBrains.Platform.RdFramework.Tasks;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.Platform.RdFramework.Text;

using JetBrains.Util;
using JetBrains.Util.Logging;
using JetBrains.Util.PersistentMap;
using Lifetime = JetBrains.DataFlow.Lifetime;

// ReSharper disable RedundantEmptyObjectCreationArgumentList
// ReSharper disable InconsistentNaming
// ReSharper disable RedundantOverflowCheckingContext


namespace JetBrains.Rider.Model
{
  
  
  public class RdUnityModel : RdExtBase {
    //fields
    //public fields
    [NotNull] public IViewableMap<string, string> Data { get { return _Data; }}
    
    //private fields
    [NotNull] private readonly RdMap<string, string> _Data;
    
    //primary constructor
    private RdUnityModel(
      [NotNull] RdMap<string, string> data
    )
    {
      if (data == null) throw new ArgumentNullException("data");
      
      _Data = data;
      _Data.OptimizeNested = true;
      BindableChildren.Add(new KeyValuePair<string, object>("data", _Data));
    }
    //secondary constructor
    internal RdUnityModel (
    ) : this (
      new RdMap<string, string>(JetBrains.Platform.RdFramework.Impl.Serializers.ReadString, JetBrains.Platform.RdFramework.Impl.Serializers.WriteString, JetBrains.Platform.RdFramework.Impl.Serializers.ReadString, JetBrains.Platform.RdFramework.Impl.Serializers.WriteString)
    ) {}
    //statics
    
    
    
    protected override long SerializationHash => -8346968635933216692L;
    
    protected override Action<ISerializers> Register => RegisterDeclaredTypesSerializers;
    public static void RegisterDeclaredTypesSerializers(ISerializers serializers)
    {
      
      serializers.RegisterToplevelOnce(typeof(IdeRoot), IdeRoot.RegisterDeclaredTypesSerializers);
    }
    
    //custom body
    //equals trait
    //hash code trait
    //pretty print
    public override void Print(PrettyPrinter printer)
    {
      printer.Println("RdUnityModel (");
      using (printer.IndentCookie()) {
        printer.Print("data = "); _Data.PrintEx(printer); printer.Println();
      }
      printer.Print(")");
    }
    //toString
    public override string ToString()
    {
      var printer = new SingleLinePrettyPrinter();
      Print(printer);
      return printer.ToString();
    }
  }
  public static class SolutionRdUnityModelEx
   {
    public static RdUnityModel GetRdUnityModel(this Solution solution)
    {
      return solution.GetOrCreateExtension("rdUnityModel", () => new RdUnityModel());
    }
  }
}
