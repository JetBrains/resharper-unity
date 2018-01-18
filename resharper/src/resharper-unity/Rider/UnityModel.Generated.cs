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


namespace JetBrains.Platform.Unity.Model
{
  
  
  public class UnityModel : RdBindableBase {
    //fields
    //public fields
    [NotNull] public IRdProperty<bool> Play { get { return _Play; }}
    [NotNull] public IRdProperty<bool> Pause { get { return _Pause; }}
    [NotNull] public IRdCall<RdVoid, RdVoid> Step { get { return _Step; }}
    [NotNull] public IRdProperty<string> UnityPluginVersion { get { return _UnityPluginVersion; }}
    [NotNull] public IRdProperty<int> RiderProcessId { get { return _RiderProcessId; }}
    [NotNull] public IRdProperty<string> ApplicationPath { get { return _ApplicationPath; }}
    [NotNull] public IRdProperty<string> ApplicationVersion { get { return _ApplicationVersion; }}
    [NotNull] public IRdProperty<UnityLogModelInitialized> LogModelInitialized { get { return _LogModelInitialized; }}
    [NotNull] public RdEndpoint<RdVoid, bool> IsClientConnected { get { return _IsClientConnected; }}
    [NotNull] public RdEndpoint<RdOpenFileArgs, bool> OpenFileLineCol { get { return _OpenFileLineCol; }}
    [NotNull] public IRdCall<string, bool> UpdateUnityPlugin { get { return _UpdateUnityPlugin; }}
    [NotNull] public IRdCall<RdVoid, RdVoid> Refresh { get { return _Refresh; }}
    
    //private fields
    [NotNull] private readonly RdProperty<bool> _Play;
    [NotNull] private readonly RdProperty<bool> _Pause;
    [NotNull] private readonly RdCall<RdVoid, RdVoid> _Step;
    [NotNull] private readonly RdProperty<string> _UnityPluginVersion;
    [NotNull] private readonly RdProperty<int> _RiderProcessId;
    [NotNull] private readonly RdProperty<string> _ApplicationPath;
    [NotNull] private readonly RdProperty<string> _ApplicationVersion;
    [NotNull] private readonly RdProperty<UnityLogModelInitialized> _LogModelInitialized;
    [NotNull] private readonly RdEndpoint<RdVoid, bool> _IsClientConnected;
    [NotNull] private readonly RdEndpoint<RdOpenFileArgs, bool> _OpenFileLineCol;
    [NotNull] private readonly RdCall<string, bool> _UpdateUnityPlugin;
    [NotNull] private readonly RdCall<RdVoid, RdVoid> _Refresh;
    
    //primary constructor
    public UnityModel(
      [NotNull] RdProperty<bool> play,
      [NotNull] RdProperty<bool> pause,
      [NotNull] RdCall<RdVoid, RdVoid> step,
      [NotNull] RdProperty<string> unityPluginVersion,
      [NotNull] RdProperty<int> riderProcessId,
      [NotNull] RdProperty<string> applicationPath,
      [NotNull] RdProperty<string> applicationVersion,
      [NotNull] RdProperty<UnityLogModelInitialized> logModelInitialized,
      [NotNull] RdEndpoint<RdVoid, bool> isClientConnected,
      [NotNull] RdEndpoint<RdOpenFileArgs, bool> openFileLineCol,
      [NotNull] RdCall<string, bool> updateUnityPlugin,
      [NotNull] RdCall<RdVoid, RdVoid> refresh
    )
    {
      if (play == null) throw new ArgumentNullException("play");
      if (pause == null) throw new ArgumentNullException("pause");
      if (step == null) throw new ArgumentNullException("step");
      if (unityPluginVersion == null) throw new ArgumentNullException("unityPluginVersion");
      if (riderProcessId == null) throw new ArgumentNullException("riderProcessId");
      if (applicationPath == null) throw new ArgumentNullException("applicationPath");
      if (applicationVersion == null) throw new ArgumentNullException("applicationVersion");
      if (logModelInitialized == null) throw new ArgumentNullException("logModelInitialized");
      if (isClientConnected == null) throw new ArgumentNullException("isClientConnected");
      if (openFileLineCol == null) throw new ArgumentNullException("openFileLineCol");
      if (updateUnityPlugin == null) throw new ArgumentNullException("updateUnityPlugin");
      if (refresh == null) throw new ArgumentNullException("refresh");
      
      _Play = play;
      _Pause = pause;
      _Step = step;
      _UnityPluginVersion = unityPluginVersion;
      _RiderProcessId = riderProcessId;
      _ApplicationPath = applicationPath;
      _ApplicationVersion = applicationVersion;
      _LogModelInitialized = logModelInitialized;
      _IsClientConnected = isClientConnected;
      _OpenFileLineCol = openFileLineCol;
      _UpdateUnityPlugin = updateUnityPlugin;
      _Refresh = refresh;
      _Play.OptimizeNested = true;
      _Pause.OptimizeNested = true;
      _UnityPluginVersion.OptimizeNested = true;
      _RiderProcessId.OptimizeNested = true;
      _ApplicationPath.OptimizeNested = true;
      _ApplicationVersion.OptimizeNested = true;
    }
    //secondary constructor
    //statics
    
    
    
    public static void Register(ISerializers serializers)
    {
      if (!serializers.Toplevels.Add(typeof(UnityModel))) return;
      Protocol.InitializationLogger.Trace("REGISTER serializers for {0}", typeof(UnityModel).Name);
      
      serializers.Register(RdOpenFileArgs.Read, RdOpenFileArgs.Write);
      serializers.Register(RdLogEvent.Read, RdLogEvent.Write);
      serializers.RegisterEnum<RdLogEventType>();
      serializers.RegisterEnum<RdLogEventMode>();
      serializers.Register(UnityLogModelInitialized.Read, UnityLogModelInitialized.Write);
    }
    
    public UnityModel(Lifetime lifetime, IProtocol protocol) : this (
      new RdProperty<bool>(Serializers.ReadBool, Serializers.WriteBool).Static(1001),
      new RdProperty<bool>(Serializers.ReadBool, Serializers.WriteBool).Static(1002),
      new RdCall<RdVoid, RdVoid>(Serializers.ReadVoid, Serializers.WriteVoid, Serializers.ReadVoid, Serializers.WriteVoid).Static(1003),
      new RdProperty<string>(Serializers.ReadString, Serializers.WriteString).Static(1004),
      new RdProperty<int>(Serializers.ReadInt, Serializers.WriteInt).Static(1005),
      new RdProperty<string>(Serializers.ReadString, Serializers.WriteString).Static(1006),
      new RdProperty<string>(Serializers.ReadString, Serializers.WriteString).Static(1007),
      new RdProperty<UnityLogModelInitialized>(UnityLogModelInitialized.Read, UnityLogModelInitialized.Write).Static(1008),
      new RdEndpoint<RdVoid, bool>(Serializers.ReadVoid, Serializers.WriteVoid, Serializers.ReadBool, Serializers.WriteBool).Static(1009),
      new RdEndpoint<RdOpenFileArgs, bool>(RdOpenFileArgs.Read, RdOpenFileArgs.Write, Serializers.ReadBool, Serializers.WriteBool).Static(1010),
      new RdCall<string, bool>(Serializers.ReadString, Serializers.WriteString, Serializers.ReadBool, Serializers.WriteBool).Static(1011),
      new RdCall<RdVoid, RdVoid>(Serializers.ReadVoid, Serializers.WriteVoid, Serializers.ReadVoid, Serializers.WriteVoid).Static(1012)
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
      _Play.BindEx(lifetime, this, "play");
      _Pause.BindEx(lifetime, this, "pause");
      _Step.BindEx(lifetime, this, "step");
      _UnityPluginVersion.BindEx(lifetime, this, "unityPluginVersion");
      _RiderProcessId.BindEx(lifetime, this, "riderProcessId");
      _ApplicationPath.BindEx(lifetime, this, "applicationPath");
      _ApplicationVersion.BindEx(lifetime, this, "applicationVersion");
      _LogModelInitialized.BindEx(lifetime, this, "logModelInitialized");
      _IsClientConnected.BindEx(lifetime, this, "isClientConnected");
      _OpenFileLineCol.BindEx(lifetime, this, "openFileLineCol");
      _UpdateUnityPlugin.BindEx(lifetime, this, "updateUnityPlugin");
      _Refresh.BindEx(lifetime, this, "refresh");
    }
    //identify method
    public override void Identify(IIdentities ids) {
      _Play.IdentifyEx(ids);
      _Pause.IdentifyEx(ids);
      _Step.IdentifyEx(ids);
      _UnityPluginVersion.IdentifyEx(ids);
      _RiderProcessId.IdentifyEx(ids);
      _ApplicationPath.IdentifyEx(ids);
      _ApplicationVersion.IdentifyEx(ids);
      _LogModelInitialized.IdentifyEx(ids);
      _IsClientConnected.IdentifyEx(ids);
      _OpenFileLineCol.IdentifyEx(ids);
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
        printer.Print("play = "); _Play.PrintEx(printer); printer.Println();
        printer.Print("pause = "); _Pause.PrintEx(printer); printer.Println();
        printer.Print("step = "); _Step.PrintEx(printer); printer.Println();
        printer.Print("unityPluginVersion = "); _UnityPluginVersion.PrintEx(printer); printer.Println();
        printer.Print("riderProcessId = "); _RiderProcessId.PrintEx(printer); printer.Println();
        printer.Print("applicationPath = "); _ApplicationPath.PrintEx(printer); printer.Println();
        printer.Print("applicationVersion = "); _ApplicationVersion.PrintEx(printer); printer.Println();
        printer.Print("logModelInitialized = "); _LogModelInitialized.PrintEx(printer); printer.Println();
        printer.Print("isClientConnected = "); _IsClientConnected.PrintEx(printer); printer.Println();
        printer.Print("openFileLineCol = "); _OpenFileLineCol.PrintEx(printer); printer.Println();
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
    public RdLogEventMode Mode {get; private set;}
    [NotNull] public string Message {get; private set;}
    [NotNull] public string StackTrace {get; private set;}
    
    //private fields
    //primary constructor
    public RdLogEvent(
      RdLogEventType type,
      RdLogEventMode mode,
      [NotNull] string message,
      [NotNull] string stackTrace
    )
    {
      if (message == null) throw new ArgumentNullException("message");
      if (stackTrace == null) throw new ArgumentNullException("stackTrace");
      
      Type = type;
      Mode = mode;
      Message = message;
      StackTrace = stackTrace;
    }
    //secondary constructor
    //statics
    
    public static CtxReadDelegate<RdLogEvent> Read = (ctx, reader) => 
    {
      var type = (RdLogEventType)reader.ReadInt();
      var mode = (RdLogEventMode)reader.ReadInt();
      var message = reader.ReadString();
      var stackTrace = reader.ReadString();
      return new RdLogEvent(type, mode, message, stackTrace);
    };
    
    public static CtxWriteDelegate<RdLogEvent> Write = (ctx, writer, value) => 
    {
      writer.Write((int)value.Type);
      writer.Write((int)value.Mode);
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
      return Type == other.Type && Mode == other.Mode && Message == other.Message && StackTrace == other.StackTrace;
    }
    //hash code trait
    public override int GetHashCode()
    {
      unchecked {
        var hash = 0;
        hash = hash * 31 + (int) Type;
        hash = hash * 31 + (int) Mode;
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
        printer.Print("mode = "); Mode.PrintEx(printer); printer.Println();
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
  
  
  public enum RdLogEventMode {
    Edit,
    Play
  }
  
  
  public enum RdLogEventType {
    Error,
    Warning,
    Message
  }
  
  
  public class RdOpenFileArgs : IPrintable, IEquatable<RdOpenFileArgs> {
    //fields
    //public fields
    [NotNull] public string Path {get; private set;}
    public int Line {get; private set;}
    public int Col {get; private set;}
    
    //private fields
    //primary constructor
    public RdOpenFileArgs(
      [NotNull] string path,
      int line,
      int col
    )
    {
      if (path == null) throw new ArgumentNullException("path");
      
      Path = path;
      Line = line;
      Col = col;
    }
    //secondary constructor
    //statics
    
    public static CtxReadDelegate<RdOpenFileArgs> Read = (ctx, reader) => 
    {
      var path = reader.ReadString();
      var line = reader.ReadInt();
      var col = reader.ReadInt();
      return new RdOpenFileArgs(path, line, col);
    };
    
    public static CtxWriteDelegate<RdOpenFileArgs> Write = (ctx, writer, value) => 
    {
      writer.Write(value.Path);
      writer.Write(value.Line);
      writer.Write(value.Col);
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
      return Equals((RdOpenFileArgs) obj);
    }
    public bool Equals(RdOpenFileArgs other)
    {
      if (ReferenceEquals(null, other)) return false;
      if (ReferenceEquals(this, other)) return true;
      return Path == other.Path && Line == other.Line && Col == other.Col;
    }
    //hash code trait
    public override int GetHashCode()
    {
      unchecked {
        var hash = 0;
        hash = hash * 31 + Path.GetHashCode();
        hash = hash * 31 + Line.GetHashCode();
        hash = hash * 31 + Col.GetHashCode();
        return hash;
      }
    }
    //pretty print
    public void Print(PrettyPrinter printer)
    {
      printer.Println("RdOpenFileArgs (");
      using (printer.IndentCookie()) {
        printer.Print("path = "); Path.PrintEx(printer); printer.Println();
        printer.Print("line = "); Line.PrintEx(printer); printer.Println();
        printer.Print("col = "); Col.PrintEx(printer); printer.Println();
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
  
  
  public class UnityLogModelInitialized : RdBindableBase {
    //fields
    //public fields
    [NotNull] public ISink<RdLogEvent> Log { get { return _Log; }}
    
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
