#if NET_4_6
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
using JetBrains.Util.Special;
using Lifetime = JetBrains.DataFlow.Lifetime;

// ReSharper disable RedundantEmptyObjectCreationArgumentList
// ReSharper disable InconsistentNaming
// ReSharper disable RedundantOverflowCheckingContext


namespace JetBrains.Platform.Unity.Model
{
  
  
  public class UnityModel : RdBindableBase {
    //fields
    //public fields
    [NotNull] public IRdProperty<bool> ServerConnected { get { return _ServerConnected; }}
    [NotNull] public IRdProperty<bool> ClientConnected { get { return _ClientConnected; }}
    [NotNull] public IRdProperty<bool> Play { get { return _Play; }}
    [NotNull] public IRdProperty<bool> Stop { get { return _Stop; }}
    [NotNull] public IRdProperty<bool> Pause { get { return _Pause; }}
    [NotNull] public IRdProperty<bool> Unpause { get { return _Unpause; }}
    [NotNull] public IRdProperty<string> UnityPluginVersion { get { return _UnityPluginVersion; }}
    [NotNull] public RdEndpoint<string, bool> UpdateUnityPlugin { get { return _UpdateUnityPlugin; }}
    [NotNull] public RdEndpoint<RdVoid, bool> Build { get { return _Build; }}
    [NotNull] public RdEndpoint<RdVoid, RdVoid> Refresh { get { return _Refresh; }}
    
    //private fields
    [NotNull] private readonly RdProperty<bool> _ServerConnected;
    [NotNull] private readonly RdProperty<bool> _ClientConnected;
    [NotNull] private readonly RdProperty<bool> _Play;
    [NotNull] private readonly RdProperty<bool> _Stop;
    [NotNull] private readonly RdProperty<bool> _Pause;
    [NotNull] private readonly RdProperty<bool> _Unpause;
    [NotNull] private readonly RdProperty<string> _UnityPluginVersion;
    [NotNull] private readonly RdEndpoint<string, bool> _UpdateUnityPlugin;
    [NotNull] private readonly RdEndpoint<RdVoid, bool> _Build;
    [NotNull] private readonly RdEndpoint<RdVoid, RdVoid> _Refresh;
    
    //primary constructor
    public UnityModel(
      [NotNull] RdProperty<bool> serverConnected,
      [NotNull] RdProperty<bool> clientConnected,
      [NotNull] RdProperty<bool> play,
      [NotNull] RdProperty<bool> stop,
      [NotNull] RdProperty<bool> pause,
      [NotNull] RdProperty<bool> unpause,
      [NotNull] RdProperty<string> unityPluginVersion,
      [NotNull] RdEndpoint<string, bool> updateUnityPlugin,
      [NotNull] RdEndpoint<RdVoid, bool> build,
      [NotNull] RdEndpoint<RdVoid, RdVoid> refresh
    )
    {
      if (serverConnected == null) throw new ArgumentNullException("serverConnected");
      if (clientConnected == null) throw new ArgumentNullException("clientConnected");
      if (play == null) throw new ArgumentNullException("play");
      if (stop == null) throw new ArgumentNullException("stop");
      if (pause == null) throw new ArgumentNullException("pause");
      if (unpause == null) throw new ArgumentNullException("unpause");
      if (unityPluginVersion == null) throw new ArgumentNullException("unityPluginVersion");
      if (updateUnityPlugin == null) throw new ArgumentNullException("updateUnityPlugin");
      if (build == null) throw new ArgumentNullException("build");
      if (refresh == null) throw new ArgumentNullException("refresh");
      
      _ServerConnected = serverConnected;
      _ClientConnected = clientConnected;
      _Play = play;
      _Stop = stop;
      _Pause = pause;
      _Unpause = unpause;
      _UnityPluginVersion = unityPluginVersion;
      _UpdateUnityPlugin = updateUnityPlugin;
      _Build = build;
      _Refresh = refresh;
      _ServerConnected.OptimizeNested = true;
      _ClientConnected.OptimizeNested = true;
      _Play.OptimizeNested = true;
      _Stop.OptimizeNested = true;
      _Pause.OptimizeNested = true;
      _Unpause.OptimizeNested = true;
      _UnityPluginVersion.OptimizeNested = true;
    }
    //secondary constructor
    //statics
    
    
    
    public static void Register(ISerializers serializers)
    {
      if (!serializers.Toplevels.Add(typeof(UnityModel))) return;
      Protocol.InitializationLogger.Trace("REGISTER serializers for {0}", typeof(UnityModel).Name);
      
      serializers.Register(RdLogEvent.Read, RdLogEvent.Write);
      serializers.RegisterEnum<RdLogEventType>();
    }
    
    public UnityModel(Lifetime lifetime, IProtocol protocol) : this (
      new RdProperty<bool>(Serializers.ReadBool, Serializers.WriteBool).Static(1001),
      new RdProperty<bool>(Serializers.ReadBool, Serializers.WriteBool).Static(1002),
      new RdProperty<bool>(Serializers.ReadBool, Serializers.WriteBool).Static(1003),
      new RdProperty<bool>(Serializers.ReadBool, Serializers.WriteBool).Static(1004),
      new RdProperty<bool>(Serializers.ReadBool, Serializers.WriteBool).Static(1005),
      new RdProperty<bool>(Serializers.ReadBool, Serializers.WriteBool).Static(1006),
      new RdProperty<string>(Serializers.ReadString, Serializers.WriteString).Static(1007),
      new RdEndpoint<string, bool>(Serializers.ReadString, Serializers.WriteString, Serializers.ReadBool, Serializers.WriteBool).Static(1008),
      new RdEndpoint<RdVoid, bool>(Serializers.ReadVoid, Serializers.WriteVoid, Serializers.ReadBool, Serializers.WriteBool).Static(1009),
      new RdEndpoint<RdVoid, RdVoid>(Serializers.ReadVoid, Serializers.WriteVoid, Serializers.ReadVoid, Serializers.WriteVoid).Static(1010)
    )
    {
      UnityModel.Register(protocol.Serializers);
      Register(protocol.Serializers);
      Bind(lifetime, protocol, GetType().Name);
      if (Protocol.InitializationLogger.IsTraceEnabled())
        Protocol.InitializationLogger.Trace ("CREATED toplevel object {0}", this.PrintToString());
    }
    //custom body
    //init method
    protected override void Init(Lifetime lifetime) {
      _ServerConnected.BindEx(lifetime, this, "serverConnected");
      _ClientConnected.BindEx(lifetime, this, "clientConnected");
      _Play.BindEx(lifetime, this, "play");
      _Stop.BindEx(lifetime, this, "stop");
      _Pause.BindEx(lifetime, this, "pause");
      _Unpause.BindEx(lifetime, this, "unpause");
      _UnityPluginVersion.BindEx(lifetime, this, "unityPluginVersion");
      _UpdateUnityPlugin.BindEx(lifetime, this, "updateUnityPlugin");
      _Build.BindEx(lifetime, this, "build");
      _Refresh.BindEx(lifetime, this, "refresh");
    }
    //identify method
    public override void Identify(IIdentities ids) {
      _ServerConnected.IdentifyEx(ids);
      _ClientConnected.IdentifyEx(ids);
      _Play.IdentifyEx(ids);
      _Stop.IdentifyEx(ids);
      _Pause.IdentifyEx(ids);
      _Unpause.IdentifyEx(ids);
      _UnityPluginVersion.IdentifyEx(ids);
      _UpdateUnityPlugin.IdentifyEx(ids);
      _Build.IdentifyEx(ids);
      _Refresh.IdentifyEx(ids);
    }
    //equals trait
    //hash code trait
    //pretty print
    public override void Print(PrettyPrinter printer)
    {
      printer.Println("UnityModel (");
      using (printer.IndentCookie()) {
        printer.Print("serverConnected = "); _ServerConnected.PrintEx(printer); printer.Println();
        printer.Print("clientConnected = "); _ClientConnected.PrintEx(printer); printer.Println();
        printer.Print("play = "); _Play.PrintEx(printer); printer.Println();
        printer.Print("stop = "); _Stop.PrintEx(printer); printer.Println();
        printer.Print("pause = "); _Pause.PrintEx(printer); printer.Println();
        printer.Print("unpause = "); _Unpause.PrintEx(printer); printer.Println();
        printer.Print("unityPluginVersion = "); _UnityPluginVersion.PrintEx(printer); printer.Println();
        printer.Print("updateUnityPlugin = "); _UpdateUnityPlugin.PrintEx(printer); printer.Println();
        printer.Print("build = "); _Build.PrintEx(printer); printer.Println();
        printer.Print("refresh = "); _Refresh.PrintEx(printer); printer.Println();
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
  
  
  public class RdLogEvent : IPrintable, IEquatable<RdLogEvent> {
    //fields
    //public fields
    public RdLogEventType Type {get; private set;}
    [NotNull] public string Message {get; private set;}
    [NotNull] public string StackTrace {get; private set;}
    
    //private fields
    //primary constructor
    public RdLogEvent(
      RdLogEventType type,
      [NotNull] string message,
      [NotNull] string stackTrace
    )
    {
      if (message == null) throw new ArgumentNullException("message");
      if (stackTrace == null) throw new ArgumentNullException("stackTrace");
      
      Type = type;
      Message = message;
      StackTrace = stackTrace;
    }
    //secondary constructor
    //statics
    
    public static CtxReadDelegate<RdLogEvent> Read = (ctx, reader) => 
    {
      var type = (RdLogEventType)reader.ReadInt();
      var message = reader.ReadString();
      var stackTrace = reader.ReadString();
      return new RdLogEvent(type, message, stackTrace);
    };
    
    public static CtxWriteDelegate<RdLogEvent> Write = (ctx, writer, value) => 
    {
      writer.Write((int)value.Type);
      writer.Write(value.Message);
      writer.Write(value.StackTrace);
    };
    //custom body
    //init method
    //identify method
    //equals trait
    public override bool Equals(object obj)
    {
      if (ReferenceEquals(null, obj)) return false;
      if (ReferenceEquals(this, obj)) return true;
      if (obj.GetType() != GetType()) return false;
      return Equals((RdLogEvent) obj);
    }
    public bool Equals(RdLogEvent other)
    {
      if (ReferenceEquals(null, other)) return false;
      if (ReferenceEquals(this, other)) return true;
      return Type == other.Type && Message == other.Message && StackTrace == other.StackTrace;
    }
    //hash code trait
    public override int GetHashCode()
    {
      unchecked {
        var hash = 0;
        hash = hash * 31 + (int) Type;
        hash = hash * 31 + Message.GetHashCode();
        hash = hash * 31 + StackTrace.GetHashCode();
        return hash;
      }
    }
    //pretty print
    public void Print(PrettyPrinter printer)
    {
      printer.Println("RdLogEvent (");
      using (printer.IndentCookie()) {
        printer.Print("type = "); Type.PrintEx(printer); printer.Println();
        printer.Print("message = "); Message.PrintEx(printer); printer.Println();
        printer.Print("stackTrace = "); StackTrace.PrintEx(printer); printer.Println();
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
  
  
  public enum RdLogEventType {
    Error,
    Warning,
    Message
  }
}
#endif