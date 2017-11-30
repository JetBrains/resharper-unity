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
    [NotNull] public IRdProperty<bool> Play { get { return _Play; }}
    [NotNull] public IRdProperty<bool> Stop { get { return _Stop; }}
    [NotNull] public IRdProperty<bool> Pause { get { return _Pause; }}
    [NotNull] public IRdProperty<bool> Unpause { get { return _Unpause; }}
    [NotNull] public IRdProperty<string> UnityPluginVersion { get { return _UnityPluginVersion; }}
    [NotNull] public IRdProperty<UnityLogModelInitialized> LogModelInitialized { get { return _LogModelInitialized; }}
    [NotNull] public IRdCall<RdVoid, bool> IsClientConnected { get { return _IsClientConnected; }}
    [NotNull] public RdEndpoint<string, bool> UpdateUnityPlugin { get { return _UpdateUnityPlugin; }}
    [NotNull] public RdEndpoint<RdVoid, RdVoid> Refresh { get { return _Refresh; }}
    
    //private fields
    [NotNull] private readonly RdProperty<bool> _ServerConnected;
    [NotNull] private readonly RdProperty<bool> _Play;
    [NotNull] private readonly RdProperty<bool> _Stop;
    [NotNull] private readonly RdProperty<bool> _Pause;
    [NotNull] private readonly RdProperty<bool> _Unpause;
    [NotNull] private readonly RdProperty<string> _UnityPluginVersion;
    [NotNull] private readonly RdProperty<UnityLogModelInitialized> _LogModelInitialized;
    [NotNull] private readonly RdCall<RdVoid, bool> _IsClientConnected;
    [NotNull] private readonly RdEndpoint<string, bool> _UpdateUnityPlugin;
    [NotNull] private readonly RdEndpoint<RdVoid, RdVoid> _Refresh;
    
    //primary constructor
    public UnityModel(
      [NotNull] RdProperty<bool> serverConnected,
      [NotNull] RdProperty<bool> play,
      [NotNull] RdProperty<bool> stop,
      [NotNull] RdProperty<bool> pause,
      [NotNull] RdProperty<bool> unpause,
      [NotNull] RdProperty<string> unityPluginVersion,
      [NotNull] RdProperty<UnityLogModelInitialized> logModelInitialized,
      [NotNull] RdCall<RdVoid, bool> isClientConnected,
      [NotNull] RdEndpoint<string, bool> updateUnityPlugin,
      [NotNull] RdEndpoint<RdVoid, RdVoid> refresh
    )
    {
      if (serverConnected == null) throw new ArgumentNullException("serverConnected");
      if (play == null) throw new ArgumentNullException("play");
      if (stop == null) throw new ArgumentNullException("stop");
      if (pause == null) throw new ArgumentNullException("pause");
      if (unpause == null) throw new ArgumentNullException("unpause");
      if (unityPluginVersion == null) throw new ArgumentNullException("unityPluginVersion");
      if (logModelInitialized == null) throw new ArgumentNullException("logModelInitialized");
      if (isClientConnected == null) throw new ArgumentNullException("isClientConnected");
      if (updateUnityPlugin == null) throw new ArgumentNullException("updateUnityPlugin");
      if (refresh == null) throw new ArgumentNullException("refresh");
      
      _ServerConnected = serverConnected;
      _Play = play;
      _Stop = stop;
      _Pause = pause;
      _Unpause = unpause;
      _UnityPluginVersion = unityPluginVersion;
      _LogModelInitialized = logModelInitialized;
      _IsClientConnected = isClientConnected;
      _UpdateUnityPlugin = updateUnityPlugin;
      _Refresh = refresh;
      _ServerConnected.OptimizeNested = true;
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
      serializers.Register(UnityLogModelInitialized.Read, UnityLogModelInitialized.Write);
    }
    
    public UnityModel(Lifetime lifetime, IProtocol protocol) : this (
      new RdProperty<bool>(Serializers.ReadBool, Serializers.WriteBool).Static(1001),
      new RdProperty<bool>(Serializers.ReadBool, Serializers.WriteBool).Static(1002),
      new RdProperty<bool>(Serializers.ReadBool, Serializers.WriteBool).Static(1003),
      new RdProperty<bool>(Serializers.ReadBool, Serializers.WriteBool).Static(1004),
      new RdProperty<bool>(Serializers.ReadBool, Serializers.WriteBool).Static(1005),
      new RdProperty<string>(Serializers.ReadString, Serializers.WriteString).Static(1006),
      new RdProperty<UnityLogModelInitialized>(UnityLogModelInitialized.Read, UnityLogModelInitialized.Write).Static(1007),
      new RdCall<RdVoid, bool>(Serializers.ReadVoid, Serializers.WriteVoid, Serializers.ReadBool, Serializers.WriteBool).Static(1008),
      new RdEndpoint<string, bool>(Serializers.ReadString, Serializers.WriteString, Serializers.ReadBool, Serializers.WriteBool).Static(1009),
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
      _Play.BindEx(lifetime, this, "play");
      _Stop.BindEx(lifetime, this, "stop");
      _Pause.BindEx(lifetime, this, "pause");
      _Unpause.BindEx(lifetime, this, "unpause");
      _UnityPluginVersion.BindEx(lifetime, this, "unityPluginVersion");
      _LogModelInitialized.BindEx(lifetime, this, "logModelInitialized");
      _IsClientConnected.BindEx(lifetime, this, "isClientConnected");
      _UpdateUnityPlugin.BindEx(lifetime, this, "updateUnityPlugin");
      _Refresh.BindEx(lifetime, this, "refresh");
    }
    //identify method
    public override void Identify(IIdentities ids) {
      _ServerConnected.IdentifyEx(ids);
      _Play.IdentifyEx(ids);
      _Stop.IdentifyEx(ids);
      _Pause.IdentifyEx(ids);
      _Unpause.IdentifyEx(ids);
      _UnityPluginVersion.IdentifyEx(ids);
      _LogModelInitialized.IdentifyEx(ids);
      _IsClientConnected.IdentifyEx(ids);
      _UpdateUnityPlugin.IdentifyEx(ids);
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
        printer.Print("play = "); _Play.PrintEx(printer); printer.Println();
        printer.Print("stop = "); _Stop.PrintEx(printer); printer.Println();
        printer.Print("pause = "); _Pause.PrintEx(printer); printer.Println();
        printer.Print("unpause = "); _Unpause.PrintEx(printer); printer.Println();
        printer.Print("unityPluginVersion = "); _UnityPluginVersion.PrintEx(printer); printer.Println();
        printer.Print("logModelInitialized = "); _LogModelInitialized.PrintEx(printer); printer.Println();
        printer.Print("isClientConnected = "); _IsClientConnected.PrintEx(printer); printer.Println();
        printer.Print("updateUnityPlugin = "); _UpdateUnityPlugin.PrintEx(printer); printer.Println();
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
  
  
  public class UnityLogModelInitialized : RdBindableBase {
    //fields
    //public fields
    [NotNull] public ISource<RdLogEvent> Log { get { return _Log; }}
    
    //private fields
    [NotNull] private readonly RdSignal<RdLogEvent> _Log;
    
    //primary constructor
    private UnityLogModelInitialized(
      [NotNull] RdSignal<RdLogEvent> log
    )
    {
      if (log == null) throw new ArgumentNullException("log");
      
      _Log = log;
    }
    //secondary constructor
    public UnityLogModelInitialized (
    ) : this (
      new RdSignal<RdLogEvent>(RdLogEvent.Read, RdLogEvent.Write)
    ) {}
    //statics
    
    public static CtxReadDelegate<UnityLogModelInitialized> Read = (ctx, reader) => 
    {
      var log = RdSignal<RdLogEvent>.Read(ctx, reader, RdLogEvent.Read, RdLogEvent.Write);
      return new UnityLogModelInitialized(log);
    };
    
    public static CtxWriteDelegate<UnityLogModelInitialized> Write = (ctx, writer, value) => 
    {
      RdSignal<RdLogEvent>.Write(ctx, writer, value._Log);
    };
    //custom body
    //init method
    protected override void Init(Lifetime lifetime) {
      _Log.BindEx(lifetime, this, "log");
    }
    //identify method
    public override void Identify(IIdentities ids) {
      _Log.IdentifyEx(ids);
    }
    //equals trait
    //hash code trait
    //pretty print
    public override void Print(PrettyPrinter printer)
    {
      printer.Println("UnityLogModelInitialized (");
      using (printer.IndentCookie()) {
        printer.Print("log = "); _Log.PrintEx(printer); printer.Println();
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
