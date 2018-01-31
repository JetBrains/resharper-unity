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
  
  
  [JetBrains.Application.ShellComponent]
  public class RdUnityModel : RdBindableBase {
    //fields
    //public fields
    [NotNull] public IViewableMap<string, string> Data { get { return _Data; }}
    
    //private fields
    [NotNull] private readonly RdMap<string, string> _Data;
    
    //primary constructor
    public RdUnityModel(
      [NotNull] RdMap<string, string> data
    )
    {
      if (data == null) throw new ArgumentNullException("data");
      
      _Data = data;
      _Data.OptimizeNested = true;
    }
    //secondary constructor
    //statics
    
    
    
    public static void Register(ISerializers serializers)
    {
      if (!serializers.Toplevels.Add(typeof(RdUnityModel))) return;
      Protocol.InitializationLogger.Trace("REGISTER serializers for {0}", typeof(RdUnityModel).Name);
      
    }
    
    public RdUnityModel(Lifetime lifetime, IProtocol protocol) : this (
      new RdMap<string, string>(Serializers.ReadString, Serializers.WriteString, Serializers.ReadString, Serializers.WriteString).Static(1001)
    )
    {
      IdeRoot.Register(protocol.Serializers);
      Register(protocol.Serializers);
      Bind(lifetime, protocol, GetType().Name);
      if (Protocol.InitializationLogger.IsTraceEnabled())
        Protocol.InitializationLogger.Trace ("CREATED toplevel object {0}", this.PrintToString());
    }
    //custom body
    //init method
    protected override void Init(Lifetime lifetime) {
      _Data.BindEx(lifetime, this, "data");
    }
    //identify method
    public override void Identify(IIdentities ids) {
      _Data.IdentifyEx(ids);
    }
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
}
