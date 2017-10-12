#if NET_4_6
#elif RIDER

using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using JetBrains.Application;
using JetBrains.Annotations;

using JetBrains.Platform.RdFramework;
using JetBrains.Platform.RdFramework.Base;
using JetBrains.Platform.RdFramework.Impl;
using JetBrains.Platform.RdFramework.Tasks;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.Platform.RdFramework.Text;

using JetBrains.Util;
using JetBrains.Util.PersistentMap;
using Lifetime = JetBrains.DataFlow.Lifetime;

// ReSharper disable RedundantEmptyObjectCreationArgumentList
// ReSharper disable InconsistentNaming
// ReSharper disable RedundantOverflowCheckingContext


namespace JetBrains.Platform.Unity.Model
{
  
  
  public class UnityModel : RdBindableBase {
    //fields
    //public fields
    [NotNull] public IRdProperty<bool> HostConnected { get { return _HostConnected; }}
    [NotNull] public IRdProperty<bool> Play { get { return _Play; }}
    
    //private fields
    [NotNull] private readonly RdProperty<bool> _HostConnected;
    [NotNull] private readonly RdProperty<bool> _Play;
    
    //primary constructor
    public UnityModel(
      [NotNull] RdProperty<bool> hostConnected,
      [NotNull] RdProperty<bool> play
    )
    {
      if (hostConnected == null) throw new ArgumentNullException("hostConnected");
      if (play == null) throw new ArgumentNullException("play");
      
      _HostConnected = hostConnected;
      _Play = play;
      _HostConnected.OptimizeNested = true;
      _Play.OptimizeNested = true;
    }
    //secondary constructor
    //statics
    
    
    
    private void Register(ISerializers serializers)
    {
    }
    
    public UnityModel(Lifetime lifetime, IProtocol protocol) : this (
      new RdProperty<bool>(Serializers.ReadBool, Serializers.WriteBool).Static(1001),
      new RdProperty<bool>(Serializers.ReadBool, Serializers.WriteBool).Static(1002)
    )
    {
      Register(protocol.Serializers);
      Bind(lifetime, protocol, GetType().Name);
    }
    //custom body
    //init method
    protected override void Init(Lifetime lifetime) {
      _HostConnected.BindEx(lifetime, this, "hostConnected");
      _Play.BindEx(lifetime, this, "play");
    }
    //identify method
    public override void Identify(IIdentities ids) {
      _HostConnected.IdentifyEx(ids);
      _Play.IdentifyEx(ids);
    }
    //equals trait
    //hash code trait
    //pretty print
    public override void Print(PrettyPrinter printer)
    {
      printer.Println("UnityModel (");
      using (printer.IndentCookie()) {
        printer.Print("hostConnected = "); _HostConnected.PrintEx(printer); printer.Println();
        printer.Print("play = "); _Play.PrintEx(printer); printer.Println();
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
#endif