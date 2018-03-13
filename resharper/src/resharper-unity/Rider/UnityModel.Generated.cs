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
  
  
  public class UnityModel : RdExtBase {
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
    [NotNull] public RdEndpoint<RdVoid, bool> IsBackendConnected { get { return _IsBackendConnected; }}
    [NotNull] public IRdCall<RdVoid, UnityEditorState> GetUnityEditorState { get { return _GetUnityEditorState; }}
    [NotNull] public RdEndpoint<RdOpenFileArgs, bool> OpenFileLineCol { get { return _OpenFileLineCol; }}
    [NotNull] public IRdCall<string, bool> UpdateUnityPlugin { get { return _UpdateUnityPlugin; }}
    [NotNull] public IRdCall<bool, RdVoid> Refresh { get { return _Refresh; }}
    [NotNull] public IRdProperty<JetBrains.Platform.Unity.Model.UnitTestLaunch> UnitTestLaunch { get { return _UnitTestLaunch; }}
    
    //private fields
    [NotNull] private readonly RdProperty<bool> _Play;
    [NotNull] private readonly RdProperty<bool> _Pause;
    [NotNull] private readonly RdCall<RdVoid, RdVoid> _Step;
    [NotNull] private readonly RdProperty<string> _UnityPluginVersion;
    [NotNull] private readonly RdProperty<int> _RiderProcessId;
    [NotNull] private readonly RdProperty<string> _ApplicationPath;
    [NotNull] private readonly RdProperty<string> _ApplicationVersion;
    [NotNull] private readonly RdProperty<UnityLogModelInitialized> _LogModelInitialized;
    [NotNull] private readonly RdEndpoint<RdVoid, bool> _IsBackendConnected;
    [NotNull] private readonly RdCall<RdVoid, UnityEditorState> _GetUnityEditorState;
    [NotNull] private readonly RdEndpoint<RdOpenFileArgs, bool> _OpenFileLineCol;
    [NotNull] private readonly RdCall<string, bool> _UpdateUnityPlugin;
    [NotNull] private readonly RdCall<bool, RdVoid> _Refresh;
    [NotNull] private readonly RdProperty<JetBrains.Platform.Unity.Model.UnitTestLaunch> _UnitTestLaunch;
    
    //primary constructor
    private UnityModel(
      [NotNull] RdProperty<bool> play,
      [NotNull] RdProperty<bool> pause,
      [NotNull] RdCall<RdVoid, RdVoid> step,
      [NotNull] RdProperty<string> unityPluginVersion,
      [NotNull] RdProperty<int> riderProcessId,
      [NotNull] RdProperty<string> applicationPath,
      [NotNull] RdProperty<string> applicationVersion,
      [NotNull] RdProperty<UnityLogModelInitialized> logModelInitialized,
      [NotNull] RdEndpoint<RdVoid, bool> isBackendConnected,
      [NotNull] RdCall<RdVoid, UnityEditorState> getUnityEditorState,
      [NotNull] RdEndpoint<RdOpenFileArgs, bool> openFileLineCol,
      [NotNull] RdCall<string, bool> updateUnityPlugin,
      [NotNull] RdCall<bool, RdVoid> refresh,
      [NotNull] RdProperty<JetBrains.Platform.Unity.Model.UnitTestLaunch> unitTestLaunch
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
      if (isBackendConnected == null) throw new ArgumentNullException("isBackendConnected");
      if (getUnityEditorState == null) throw new ArgumentNullException("getUnityEditorState");
      if (openFileLineCol == null) throw new ArgumentNullException("openFileLineCol");
      if (updateUnityPlugin == null) throw new ArgumentNullException("updateUnityPlugin");
      if (refresh == null) throw new ArgumentNullException("refresh");
      if (unitTestLaunch == null) throw new ArgumentNullException("unitTestLaunch");
      
      _Play = play;
      _Pause = pause;
      _Step = step;
      _UnityPluginVersion = unityPluginVersion;
      _RiderProcessId = riderProcessId;
      _ApplicationPath = applicationPath;
      _ApplicationVersion = applicationVersion;
      _LogModelInitialized = logModelInitialized;
      _IsBackendConnected = isBackendConnected;
      _GetUnityEditorState = getUnityEditorState;
      _OpenFileLineCol = openFileLineCol;
      _UpdateUnityPlugin = updateUnityPlugin;
      _Refresh = refresh;
      _UnitTestLaunch = unitTestLaunch;
      _Play.OptimizeNested = true;
      _Pause.OptimizeNested = true;
      _UnityPluginVersion.OptimizeNested = true;
      _RiderProcessId.OptimizeNested = true;
      _ApplicationPath.OptimizeNested = true;
      _ApplicationVersion.OptimizeNested = true;
      BindableChildren.Add(new KeyValuePair<string, object>("play", _Play));
      BindableChildren.Add(new KeyValuePair<string, object>("pause", _Pause));
      BindableChildren.Add(new KeyValuePair<string, object>("step", _Step));
      BindableChildren.Add(new KeyValuePair<string, object>("unityPluginVersion", _UnityPluginVersion));
      BindableChildren.Add(new KeyValuePair<string, object>("riderProcessId", _RiderProcessId));
      BindableChildren.Add(new KeyValuePair<string, object>("applicationPath", _ApplicationPath));
      BindableChildren.Add(new KeyValuePair<string, object>("applicationVersion", _ApplicationVersion));
      BindableChildren.Add(new KeyValuePair<string, object>("logModelInitialized", _LogModelInitialized));
      BindableChildren.Add(new KeyValuePair<string, object>("isBackendConnected", _IsBackendConnected));
      BindableChildren.Add(new KeyValuePair<string, object>("getUnityEditorState", _GetUnityEditorState));
      BindableChildren.Add(new KeyValuePair<string, object>("openFileLineCol", _OpenFileLineCol));
      BindableChildren.Add(new KeyValuePair<string, object>("updateUnityPlugin", _UpdateUnityPlugin));
      BindableChildren.Add(new KeyValuePair<string, object>("refresh", _Refresh));
      BindableChildren.Add(new KeyValuePair<string, object>("unitTestLaunch", _UnitTestLaunch));
    }
    //secondary constructor
    private UnityModel (
    ) : this (
      new RdProperty<bool>(JetBrains.Platform.RdFramework.Impl.Serializers.ReadBool, JetBrains.Platform.RdFramework.Impl.Serializers.WriteBool),
      new RdProperty<bool>(JetBrains.Platform.RdFramework.Impl.Serializers.ReadBool, JetBrains.Platform.RdFramework.Impl.Serializers.WriteBool),
      new RdCall<RdVoid, RdVoid>(JetBrains.Platform.RdFramework.Impl.Serializers.ReadVoid, JetBrains.Platform.RdFramework.Impl.Serializers.WriteVoid, JetBrains.Platform.RdFramework.Impl.Serializers.ReadVoid, JetBrains.Platform.RdFramework.Impl.Serializers.WriteVoid),
      new RdProperty<string>(JetBrains.Platform.RdFramework.Impl.Serializers.ReadString, JetBrains.Platform.RdFramework.Impl.Serializers.WriteString),
      new RdProperty<int>(JetBrains.Platform.RdFramework.Impl.Serializers.ReadInt, JetBrains.Platform.RdFramework.Impl.Serializers.WriteInt),
      new RdProperty<string>(JetBrains.Platform.RdFramework.Impl.Serializers.ReadString, JetBrains.Platform.RdFramework.Impl.Serializers.WriteString),
      new RdProperty<string>(JetBrains.Platform.RdFramework.Impl.Serializers.ReadString, JetBrains.Platform.RdFramework.Impl.Serializers.WriteString),
      new RdProperty<UnityLogModelInitialized>(UnityLogModelInitialized.Read, UnityLogModelInitialized.Write),
      new RdEndpoint<RdVoid, bool>(JetBrains.Platform.RdFramework.Impl.Serializers.ReadVoid, JetBrains.Platform.RdFramework.Impl.Serializers.WriteVoid, JetBrains.Platform.RdFramework.Impl.Serializers.ReadBool, JetBrains.Platform.RdFramework.Impl.Serializers.WriteBool),
      new RdCall<RdVoid, UnityEditorState>(JetBrains.Platform.RdFramework.Impl.Serializers.ReadVoid, JetBrains.Platform.RdFramework.Impl.Serializers.WriteVoid, ReadUnityEditorState, WriteUnityEditorState),
      new RdEndpoint<RdOpenFileArgs, bool>(RdOpenFileArgs.Read, RdOpenFileArgs.Write, JetBrains.Platform.RdFramework.Impl.Serializers.ReadBool, JetBrains.Platform.RdFramework.Impl.Serializers.WriteBool),
      new RdCall<string, bool>(JetBrains.Platform.RdFramework.Impl.Serializers.ReadString, JetBrains.Platform.RdFramework.Impl.Serializers.WriteString, JetBrains.Platform.RdFramework.Impl.Serializers.ReadBool, JetBrains.Platform.RdFramework.Impl.Serializers.WriteBool),
      new RdCall<bool, RdVoid>(JetBrains.Platform.RdFramework.Impl.Serializers.ReadBool, JetBrains.Platform.RdFramework.Impl.Serializers.WriteBool, JetBrains.Platform.RdFramework.Impl.Serializers.ReadVoid, JetBrains.Platform.RdFramework.Impl.Serializers.WriteVoid),
      new RdProperty<JetBrains.Platform.Unity.Model.UnitTestLaunch>(JetBrains.Platform.Unity.Model.UnitTestLaunch.Read, JetBrains.Platform.Unity.Model.UnitTestLaunch.Write)
    ) {}
    //statics
    
    public static CtxReadDelegate<UnityEditorState> ReadUnityEditorState = new CtxReadDelegate<UnityEditorState>(JetBrains.Platform.RdFramework.Impl.Serializers.ReadEnum<UnityEditorState>);
    
    public static CtxWriteDelegate<UnityEditorState> WriteUnityEditorState = new CtxWriteDelegate<UnityEditorState>(JetBrains.Platform.RdFramework.Impl.Serializers.WriteEnum<UnityEditorState>);
    
    protected override long SerializationHash => -6185208528177099467L;
    
    protected override Action<ISerializers> Register => RegisterDeclaredTypesSerializers;
    public static void RegisterDeclaredTypesSerializers(ISerializers serializers)
    {
      serializers.Register(RdOpenFileArgs.Read, RdOpenFileArgs.Write);
      serializers.Register(RdLogEvent.Read, RdLogEvent.Write);
      serializers.RegisterEnum<RdLogEventType>();
      serializers.RegisterEnum<RdLogEventMode>();
      serializers.Register(UnityLogModelInitialized.Read, UnityLogModelInitialized.Write);
      serializers.Register(TestResult.Read, TestResult.Write);
      serializers.Register(RunResult.Read, RunResult.Write);
      serializers.Register(JetBrains.Platform.Unity.Model.UnitTestLaunch.Read, JetBrains.Platform.Unity.Model.UnitTestLaunch.Write);
      serializers.RegisterEnum<UnityEditorState>();
      serializers.RegisterEnum<Status>();
      
      serializers.RegisterToplevelOnce(typeof(UnityModel), UnityModel.RegisterDeclaredTypesSerializers);
    }
    
    public UnityModel(Lifetime lifetime, IProtocol protocol) : this()
    {
      Identify(protocol.Identities, RdId.Root.Mix(GetType().Name));
      Bind(lifetime, protocol, GetType().Name);
      if (Protocol.InitializationLogger.IsTraceEnabled())
        Protocol.InitializationLogger.Trace ("CREATED toplevel object {0}", this.PrintToString());
    }
    //custom body
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
        printer.Print("isBackendConnected = "); _IsBackendConnected.PrintEx(printer); printer.Println();
        printer.Print("getUnityEditorState = "); _GetUnityEditorState.PrintEx(printer); printer.Println();
        printer.Print("openFileLineCol = "); _OpenFileLineCol.PrintEx(printer); printer.Println();
        printer.Print("updateUnityPlugin = "); _UpdateUnityPlugin.PrintEx(printer); printer.Println();
        printer.Print("refresh = "); _Refresh.PrintEx(printer); printer.Println();
        printer.Print("unitTestLaunch = "); _UnitTestLaunch.PrintEx(printer); printer.Println();
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
  
  
  public class RunResult : IPrintable, IEquatable<RunResult> {
    //fields
    //public fields
    public bool Passed {get; private set;}
    
    //private fields
    //primary constructor
    public RunResult(
      bool passed
    )
    {
      Passed = passed;
    }
    //secondary constructor
    //statics
    
    public static CtxReadDelegate<RunResult> Read = (ctx, reader) => 
    {
      var passed = reader.ReadBool();
      return new RunResult(passed);
    };
    
    public static CtxWriteDelegate<RunResult> Write = (ctx, writer, value) => 
    {
      writer.Write(value.Passed);
    };
    //custom body
    //equals trait
    public override bool Equals(object obj)
    {
      if (ReferenceEquals(null, obj)) return false;
      if (ReferenceEquals(this, obj)) return true;
      if (obj.GetType() != GetType()) return false;
      return Equals((RunResult) obj);
    }
    public bool Equals(RunResult other)
    {
      if (ReferenceEquals(null, other)) return false;
      if (ReferenceEquals(this, other)) return true;
      return Passed == other.Passed;
    }
    //hash code trait
    public override int GetHashCode()
    {
      unchecked {
        var hash = 0;
        hash = hash * 31 + Passed.GetHashCode();
        return hash;
      }
    }
    //pretty print
    public void Print(PrettyPrinter printer)
    {
      printer.Println("RunResult (");
      using (printer.IndentCookie()) {
        printer.Print("passed = "); Passed.PrintEx(printer); printer.Println();
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
  
  
  public enum Status {
    Pending,
    Running,
    Passed,
    Failed
  }
  
  
  public class TestResult : IPrintable, IEquatable<TestResult> {
    //fields
    //public fields
    [NotNull] public string TestId {get; private set;}
    [NotNull] public string Output {get; private set;}
    public int Duration {get; private set;}
    public JetBrains.Platform.Unity.Model.Status Status {get; private set;}
    
    //private fields
    //primary constructor
    public TestResult(
      [NotNull] string testId,
      [NotNull] string output,
      int duration,
      JetBrains.Platform.Unity.Model.Status status
    )
    {
      if (testId == null) throw new ArgumentNullException("testId");
      if (output == null) throw new ArgumentNullException("output");
      
      TestId = testId;
      Output = output;
      Duration = duration;
      Status = status;
    }
    //secondary constructor
    //statics
    
    public static CtxReadDelegate<TestResult> Read = (ctx, reader) => 
    {
      var testId = reader.ReadString();
      var output = reader.ReadString();
      var duration = reader.ReadInt();
      var status = (JetBrains.Platform.Unity.Model.Status)reader.ReadInt();
      return new TestResult(testId, output, duration, status);
    };
    
    public static CtxWriteDelegate<TestResult> Write = (ctx, writer, value) => 
    {
      writer.Write(value.TestId);
      writer.Write(value.Output);
      writer.Write(value.Duration);
      writer.Write((int)value.Status);
    };
    //custom body
    //equals trait
    public override bool Equals(object obj)
    {
      if (ReferenceEquals(null, obj)) return false;
      if (ReferenceEquals(this, obj)) return true;
      if (obj.GetType() != GetType()) return false;
      return Equals((TestResult) obj);
    }
    public bool Equals(TestResult other)
    {
      if (ReferenceEquals(null, other)) return false;
      if (ReferenceEquals(this, other)) return true;
      return TestId == other.TestId && Output == other.Output && Duration == other.Duration && Status == other.Status;
    }
    //hash code trait
    public override int GetHashCode()
    {
      unchecked {
        var hash = 0;
        hash = hash * 31 + TestId.GetHashCode();
        hash = hash * 31 + Output.GetHashCode();
        hash = hash * 31 + Duration.GetHashCode();
        hash = hash * 31 + (int) Status;
        return hash;
      }
    }
    //pretty print
    public void Print(PrettyPrinter printer)
    {
      printer.Println("TestResult (");
      using (printer.IndentCookie()) {
        printer.Print("testId = "); TestId.PrintEx(printer); printer.Println();
        printer.Print("output = "); Output.PrintEx(printer); printer.Println();
        printer.Print("duration = "); Duration.PrintEx(printer); printer.Println();
        printer.Print("status = "); Status.PrintEx(printer); printer.Println();
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
  
  
  public class UnitTestLaunch : RdBindableBase {
    //fields
    //public fields
    [NotNull] public List<string> TestNames {get; private set;}
    [NotNull] public List<string> TestGroups {get; private set;}
    [NotNull] public List<string> TestCategories {get; private set;}
    [NotNull] public ISink<JetBrains.Platform.Unity.Model.TestResult> TestResult { get { return _TestResult; }}
    [NotNull] public ISink<JetBrains.Platform.Unity.Model.RunResult> RunResult { get { return _RunResult; }}
    
    //private fields
    [NotNull] private readonly RdSignal<JetBrains.Platform.Unity.Model.TestResult> _TestResult;
    [NotNull] private readonly RdSignal<JetBrains.Platform.Unity.Model.RunResult> _RunResult;
    
    //primary constructor
    private UnitTestLaunch(
      [NotNull] List<string> testNames,
      [NotNull] List<string> testGroups,
      [NotNull] List<string> testCategories,
      [NotNull] RdSignal<JetBrains.Platform.Unity.Model.TestResult> testResult,
      [NotNull] RdSignal<JetBrains.Platform.Unity.Model.RunResult> runResult
    )
    {
      if (testNames == null) throw new ArgumentNullException("testNames");
      if (testGroups == null) throw new ArgumentNullException("testGroups");
      if (testCategories == null) throw new ArgumentNullException("testCategories");
      if (testResult == null) throw new ArgumentNullException("testResult");
      if (runResult == null) throw new ArgumentNullException("runResult");
      
      TestNames = testNames;
      TestGroups = testGroups;
      TestCategories = testCategories;
      _TestResult = testResult;
      _RunResult = runResult;
      BindableChildren.Add(new KeyValuePair<string, object>("testResult", _TestResult));
      BindableChildren.Add(new KeyValuePair<string, object>("runResult", _RunResult));
    }
    //secondary constructor
    public UnitTestLaunch (
      [NotNull] List<string> testNames,
      [NotNull] List<string> testGroups,
      [NotNull] List<string> testCategories
    ) : this (
      testNames,
      testGroups,
      testCategories,
      new RdSignal<JetBrains.Platform.Unity.Model.TestResult>(JetBrains.Platform.Unity.Model.TestResult.Read, JetBrains.Platform.Unity.Model.TestResult.Write),
      new RdSignal<JetBrains.Platform.Unity.Model.RunResult>(JetBrains.Platform.Unity.Model.RunResult.Read, JetBrains.Platform.Unity.Model.RunResult.Write)
    ) {}
    //statics
    
    public static CtxReadDelegate<UnitTestLaunch> Read = (ctx, reader) => 
    {
      var _id = RdId.Read(reader);
      var testNames = ReadStringList(ctx, reader);
      var testGroups = ReadStringList(ctx, reader);
      var testCategories = ReadStringList(ctx, reader);
      var testResult = RdSignal<JetBrains.Platform.Unity.Model.TestResult>.Read(ctx, reader, JetBrains.Platform.Unity.Model.TestResult.Read, JetBrains.Platform.Unity.Model.TestResult.Write);
      var runResult = RdSignal<JetBrains.Platform.Unity.Model.RunResult>.Read(ctx, reader, JetBrains.Platform.Unity.Model.RunResult.Read, JetBrains.Platform.Unity.Model.RunResult.Write);
      return new UnitTestLaunch(testNames, testGroups, testCategories, testResult, runResult).WithId(_id);
    };
    public static CtxReadDelegate<List<string>> ReadStringList = JetBrains.Platform.RdFramework.Impl.Serializers.ReadString.List();
    
    public static CtxWriteDelegate<UnitTestLaunch> Write = (ctx, writer, value) => 
    {
      value.RdId.Write(writer);
      WriteStringList(ctx, writer, value.TestNames);
      WriteStringList(ctx, writer, value.TestGroups);
      WriteStringList(ctx, writer, value.TestCategories);
      RdSignal<JetBrains.Platform.Unity.Model.TestResult>.Write(ctx, writer, value._TestResult);
      RdSignal<JetBrains.Platform.Unity.Model.RunResult>.Write(ctx, writer, value._RunResult);
    };
    public static CtxWriteDelegate<List<string>> WriteStringList = JetBrains.Platform.RdFramework.Impl.Serializers.WriteString.List();
    //custom body
    //equals trait
    //hash code trait
    //pretty print
    public override void Print(PrettyPrinter printer)
    {
      printer.Println("UnitTestLaunch (");
      using (printer.IndentCookie()) {
        printer.Print("testNames = "); TestNames.PrintEx(printer); printer.Println();
        printer.Print("testGroups = "); TestGroups.PrintEx(printer); printer.Println();
        printer.Print("testCategories = "); TestCategories.PrintEx(printer); printer.Println();
        printer.Print("testResult = "); _TestResult.PrintEx(printer); printer.Println();
        printer.Print("runResult = "); _RunResult.PrintEx(printer); printer.Println();
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
  
  
  public enum UnityEditorState {
    Disconnected,
    Idle,
    Play,
    Refresh
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
      BindableChildren.Add(new KeyValuePair<string, object>("log", _Log));
    }
    //secondary constructor
    public UnityLogModelInitialized (
    ) : this (
      new RdSignal<RdLogEvent>(RdLogEvent.Read, RdLogEvent.Write)
    ) {}
    //statics
    
    public static CtxReadDelegate<UnityLogModelInitialized> Read = (ctx, reader) => 
    {
      var _id = RdId.Read(reader);
      var log = RdSignal<RdLogEvent>.Read(ctx, reader, RdLogEvent.Read, RdLogEvent.Write);
      return new UnityLogModelInitialized(log).WithId(_id);
    };
    
    public static CtxWriteDelegate<UnityLogModelInitialized> Write = (ctx, writer, value) => 
    {
      value.RdId.Write(writer);
      RdSignal<RdLogEvent>.Write(ctx, writer, value._Log);
    };
    //custom body
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
