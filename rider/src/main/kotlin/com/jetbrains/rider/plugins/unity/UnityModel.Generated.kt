@file:Suppress("PackageDirectoryMismatch", "UnusedImport", "unused", "LocalVariableName")
package com.jetbrains.rider.plugins.unity

import com.jetbrains.rider.framework.*
import com.jetbrains.rider.framework.base.*
import com.jetbrains.rider.framework.impl.*

import com.jetbrains.rider.util.lifetime.*
import com.jetbrains.rider.util.reactive.*
import com.jetbrains.rider.util.string.*
import com.jetbrains.rider.util.trace

import java.io.*
import java.util.*
import java.net.*



class UnityModel private constructor(
    private val _play : RdOptionalProperty<Boolean>,
    private val _pause : RdOptionalProperty<Boolean>,
    private val _step : RdCall<Unit, Unit>,
    private val _unityPluginVersion : RdOptionalProperty<String>,
    private val _riderProcessId : RdOptionalProperty<Int>,
    private val _applicationPath : RdOptionalProperty<String>,
    private val _applicationVersion : RdOptionalProperty<String>,
    private val _logModelInitialized : RdOptionalProperty<UnityLogModelInitialized>,
    private val _isBackendConnected : RdEndpoint<Unit, Boolean>,
    private val _getUnityEditorState : RdCall<Unit, UnityEditorState>,
    private val _openFileLineCol : RdEndpoint<RdOpenFileArgs, Boolean>,
    private val _updateUnityPlugin : RdCall<String, Boolean>,
    private val _refresh : RdCall<Boolean, Unit>,
    private val _unitTestLaunch : RdOptionalProperty<UnitTestLaunch>
) : RdExtBase() {
    //companion

    companion object : ISerializersOwner {

        override fun registerSerializersCore(serializers : ISerializers) {
            serializers.register(RdOpenFileArgs)
            serializers.register(RdLogEvent)
            serializers.register(RdLogEventType.marshaller)
            serializers.register(RdLogEventMode.marshaller)
            serializers.register(UnityLogModelInitialized)
            serializers.register(TestResult)
            serializers.register(RunResult)
            serializers.register(UnitTestLaunch)
            serializers.register(UnityEditorState.marshaller)
            serializers.register(Status.marshaller)
            UnityModel.register(serializers)
        }


        fun create(lifetime: Lifetime, protocol: IProtocol) : UnityModel {
            UnityModel.register(protocol.serializers)

            return UnityModel ().apply {
                identify(protocol.identity, RdId.Null.mix("UnityModel"))
                bind(lifetime, protocol, "UnityModel")
            }
        }

    }
    override val serializersOwner : ISerializersOwner get() = UnityModel
    override val serializationHash : Long get() = -6185208528177099467L

    //fields
    val play : IOptProperty<Boolean> get() = _play
    val pause : IOptProperty<Boolean> get() = _pause
    val step : IRdCall<Unit, Unit> get() = _step
    val unityPluginVersion : IOptProperty<String> get() = _unityPluginVersion
    val riderProcessId : IOptProperty<Int> get() = _riderProcessId
    val applicationPath : IOptProperty<String> get() = _applicationPath
    val applicationVersion : IOptProperty<String> get() = _applicationVersion
    val logModelInitialized : IOptProperty<UnityLogModelInitialized> get() = _logModelInitialized
    val isBackendConnected : RdEndpoint<Unit, Boolean> get() = _isBackendConnected
    val getUnityEditorState : IRdCall<Unit, UnityEditorState> get() = _getUnityEditorState
    val openFileLineCol : RdEndpoint<RdOpenFileArgs, Boolean> get() = _openFileLineCol
    val updateUnityPlugin : IRdCall<String, Boolean> get() = _updateUnityPlugin
    val refresh : IRdCall<Boolean, Unit> get() = _refresh
    val unitTestLaunch : IOptProperty<UnitTestLaunch> get() = _unitTestLaunch

    //initializer
    init {
        _play.optimizeNested = true
        _pause.optimizeNested = true
        _unityPluginVersion.optimizeNested = true
        _riderProcessId.optimizeNested = true
        _applicationPath.optimizeNested = true
        _applicationVersion.optimizeNested = true
    }

    init {
        bindableChildren.add("play" to _play)
        bindableChildren.add("pause" to _pause)
        bindableChildren.add("step" to _step)
        bindableChildren.add("unityPluginVersion" to _unityPluginVersion)
        bindableChildren.add("riderProcessId" to _riderProcessId)
        bindableChildren.add("applicationPath" to _applicationPath)
        bindableChildren.add("applicationVersion" to _applicationVersion)
        bindableChildren.add("logModelInitialized" to _logModelInitialized)
        bindableChildren.add("isBackendConnected" to _isBackendConnected)
        bindableChildren.add("getUnityEditorState" to _getUnityEditorState)
        bindableChildren.add("openFileLineCol" to _openFileLineCol)
        bindableChildren.add("updateUnityPlugin" to _updateUnityPlugin)
        bindableChildren.add("refresh" to _refresh)
        bindableChildren.add("unitTestLaunch" to _unitTestLaunch)
    }

    //secondary constructor
    private constructor(
    ) : this (
        RdOptionalProperty<Boolean>(FrameworkMarshallers.Bool),
        RdOptionalProperty<Boolean>(FrameworkMarshallers.Bool),
        RdCall<Unit, Unit>(FrameworkMarshallers.Void, FrameworkMarshallers.Void),
        RdOptionalProperty<String>(FrameworkMarshallers.String),
        RdOptionalProperty<Int>(FrameworkMarshallers.Int),
        RdOptionalProperty<String>(FrameworkMarshallers.String),
        RdOptionalProperty<String>(FrameworkMarshallers.String),
        RdOptionalProperty<UnityLogModelInitialized>(UnityLogModelInitialized),
        RdEndpoint<Unit, Boolean>(FrameworkMarshallers.Void, FrameworkMarshallers.Bool),
        RdCall<Unit, UnityEditorState>(FrameworkMarshallers.Void, UnityEditorState.marshaller),
        RdEndpoint<RdOpenFileArgs, Boolean>(RdOpenFileArgs, FrameworkMarshallers.Bool),
        RdCall<String, Boolean>(FrameworkMarshallers.String, FrameworkMarshallers.Bool),
        RdCall<Boolean, Unit>(FrameworkMarshallers.Bool, FrameworkMarshallers.Void),
        RdOptionalProperty<UnitTestLaunch>(UnitTestLaunch)
    )

    //equals trait
    //hash code trait
    //pretty print
    override fun print(printer: PrettyPrinter) {
        printer.println("UnityModel (")
        printer.indent {
            print("play = "); _play.print(printer); println()
            print("pause = "); _pause.print(printer); println()
            print("step = "); _step.print(printer); println()
            print("unityPluginVersion = "); _unityPluginVersion.print(printer); println()
            print("riderProcessId = "); _riderProcessId.print(printer); println()
            print("applicationPath = "); _applicationPath.print(printer); println()
            print("applicationVersion = "); _applicationVersion.print(printer); println()
            print("logModelInitialized = "); _logModelInitialized.print(printer); println()
            print("isBackendConnected = "); _isBackendConnected.print(printer); println()
            print("getUnityEditorState = "); _getUnityEditorState.print(printer); println()
            print("openFileLineCol = "); _openFileLineCol.print(printer); println()
            print("updateUnityPlugin = "); _updateUnityPlugin.print(printer); println()
            print("refresh = "); _refresh.print(printer); println()
            print("unitTestLaunch = "); _unitTestLaunch.print(printer); println()
        }
        printer.print(")")
    }
}


data class RdLogEvent (
    val type : RdLogEventType,
    val mode : RdLogEventMode,
    val message : String,
    val stackTrace : String
) : IPrintable {
    //companion

    companion object : IMarshaller<RdLogEvent> {
        override val _type: Class<RdLogEvent> = RdLogEvent::class.java

        @Suppress("UNCHECKED_CAST")
        override fun read(ctx: SerializationCtx, buffer: AbstractBuffer): RdLogEvent {
            val type = buffer.readEnum<RdLogEventType>()
            val mode = buffer.readEnum<RdLogEventMode>()
            val message = buffer.readString()
            val stackTrace = buffer.readString()
            return RdLogEvent(type, mode, message, stackTrace)
        }

        override fun write(ctx: SerializationCtx, buffer: AbstractBuffer, value: RdLogEvent) {
            buffer.writeEnum(value.type)
            buffer.writeEnum(value.mode)
            buffer.writeString(value.message)
            buffer.writeString(value.stackTrace)
        }

    }
    //fields
    //initializer
    //secondary constructor
    //equals trait
    override fun equals(other: Any?): Boolean {
        if (this === other) return true
        if (other?.javaClass != javaClass) return false

        other as RdLogEvent

        if (type != other.type) return false
        if (mode != other.mode) return false
        if (message != other.message) return false
        if (stackTrace != other.stackTrace) return false

        return true
    }
    //hash code trait
    override fun hashCode(): Int {
        var __r = 0
        __r = __r*31 + type.hashCode()
        __r = __r*31 + mode.hashCode()
        __r = __r*31 + message.hashCode()
        __r = __r*31 + stackTrace.hashCode()
        return __r
    }
    //pretty print
    override fun print(printer: PrettyPrinter) {
        printer.println("RdLogEvent (")
        printer.indent {
            print("type = "); type.print(printer); println()
            print("mode = "); mode.print(printer); println()
            print("message = "); message.print(printer); println()
            print("stackTrace = "); stackTrace.print(printer); println()
        }
        printer.print(")")
    }
}


enum class RdLogEventMode {
    Edit,
    Play;

    companion object { val marshaller = FrameworkMarshallers.enum<RdLogEventMode>() }
}


enum class RdLogEventType {
    Error,
    Warning,
    Message;

    companion object { val marshaller = FrameworkMarshallers.enum<RdLogEventType>() }
}


data class RdOpenFileArgs (
    val path : String,
    val line : Int,
    val col : Int
) : IPrintable {
    //companion

    companion object : IMarshaller<RdOpenFileArgs> {
        override val _type: Class<RdOpenFileArgs> = RdOpenFileArgs::class.java

        @Suppress("UNCHECKED_CAST")
        override fun read(ctx: SerializationCtx, buffer: AbstractBuffer): RdOpenFileArgs {
            val path = buffer.readString()
            val line = buffer.readInt()
            val col = buffer.readInt()
            return RdOpenFileArgs(path, line, col)
        }

        override fun write(ctx: SerializationCtx, buffer: AbstractBuffer, value: RdOpenFileArgs) {
            buffer.writeString(value.path)
            buffer.writeInt(value.line)
            buffer.writeInt(value.col)
        }

    }
    //fields
    //initializer
    //secondary constructor
    //equals trait
    override fun equals(other: Any?): Boolean {
        if (this === other) return true
        if (other?.javaClass != javaClass) return false

        other as RdOpenFileArgs

        if (path != other.path) return false
        if (line != other.line) return false
        if (col != other.col) return false

        return true
    }
    //hash code trait
    override fun hashCode(): Int {
        var __r = 0
        __r = __r*31 + path.hashCode()
        __r = __r*31 + line.hashCode()
        __r = __r*31 + col.hashCode()
        return __r
    }
    //pretty print
    override fun print(printer: PrettyPrinter) {
        printer.println("RdOpenFileArgs (")
        printer.indent {
            print("path = "); path.print(printer); println()
            print("line = "); line.print(printer); println()
            print("col = "); col.print(printer); println()
        }
        printer.print(")")
    }
}


data class RunResult (
    val passed : Boolean
) : IPrintable {
    //companion

    companion object : IMarshaller<RunResult> {
        override val _type: Class<RunResult> = RunResult::class.java

        @Suppress("UNCHECKED_CAST")
        override fun read(ctx: SerializationCtx, buffer: AbstractBuffer): RunResult {
            val passed = buffer.readBool()
            return RunResult(passed)
        }

        override fun write(ctx: SerializationCtx, buffer: AbstractBuffer, value: RunResult) {
            buffer.writeBool(value.passed)
        }

    }
    //fields
    //initializer
    //secondary constructor
    //equals trait
    override fun equals(other: Any?): Boolean {
        if (this === other) return true
        if (other?.javaClass != javaClass) return false

        other as RunResult

        if (passed != other.passed) return false

        return true
    }
    //hash code trait
    override fun hashCode(): Int {
        var __r = 0
        __r = __r*31 + passed.hashCode()
        return __r
    }
    //pretty print
    override fun print(printer: PrettyPrinter) {
        printer.println("RunResult (")
        printer.indent {
            print("passed = "); passed.print(printer); println()
        }
        printer.print(")")
    }
}


enum class Status {
    Pending,
    Running,
    Passed,
    Failed;

    companion object { val marshaller = FrameworkMarshallers.enum<Status>() }
}


data class TestResult (
    val testId : String,
    val output : String,
    val duration : Int,
    val status : Status
) : IPrintable {
    //companion

    companion object : IMarshaller<TestResult> {
        override val _type: Class<TestResult> = TestResult::class.java

        @Suppress("UNCHECKED_CAST")
        override fun read(ctx: SerializationCtx, buffer: AbstractBuffer): TestResult {
            val testId = buffer.readString()
            val output = buffer.readString()
            val duration = buffer.readInt()
            val status = buffer.readEnum<Status>()
            return TestResult(testId, output, duration, status)
        }

        override fun write(ctx: SerializationCtx, buffer: AbstractBuffer, value: TestResult) {
            buffer.writeString(value.testId)
            buffer.writeString(value.output)
            buffer.writeInt(value.duration)
            buffer.writeEnum(value.status)
        }

    }
    //fields
    //initializer
    //secondary constructor
    //equals trait
    override fun equals(other: Any?): Boolean {
        if (this === other) return true
        if (other?.javaClass != javaClass) return false

        other as TestResult

        if (testId != other.testId) return false
        if (output != other.output) return false
        if (duration != other.duration) return false
        if (status != other.status) return false

        return true
    }
    //hash code trait
    override fun hashCode(): Int {
        var __r = 0
        __r = __r*31 + testId.hashCode()
        __r = __r*31 + output.hashCode()
        __r = __r*31 + duration.hashCode()
        __r = __r*31 + status.hashCode()
        return __r
    }
    //pretty print
    override fun print(printer: PrettyPrinter) {
        printer.println("TestResult (")
        printer.indent {
            print("testId = "); testId.print(printer); println()
            print("output = "); output.print(printer); println()
            print("duration = "); duration.print(printer); println()
            print("status = "); status.print(printer); println()
        }
        printer.print(")")
    }
}


class UnitTestLaunch private constructor(
    val testNames : List<String>,
    val testGroups : List<String>,
    val testCategories : List<String>,
    private val _testResult : RdSignal<TestResult>,
    private val _runResult : RdSignal<RunResult>
) : RdBindableBase() {
    //companion

    companion object : IMarshaller<UnitTestLaunch> {
        override val _type: Class<UnitTestLaunch> = UnitTestLaunch::class.java

        @Suppress("UNCHECKED_CAST")
        override fun read(ctx: SerializationCtx, buffer: AbstractBuffer): UnitTestLaunch {
            val _id = RdId.read(buffer)
            val testNames = buffer.readList {buffer.readString()}
            val testGroups = buffer.readList {buffer.readString()}
            val testCategories = buffer.readList {buffer.readString()}
            val _testResult = RdSignal.read(ctx, buffer, TestResult)
            val _runResult = RdSignal.read(ctx, buffer, RunResult)
            return UnitTestLaunch(testNames, testGroups, testCategories, _testResult, _runResult).withId(_id)
        }

        override fun write(ctx: SerializationCtx, buffer: AbstractBuffer, value: UnitTestLaunch) {
            value.rdid.write(buffer)
            buffer.writeList(value.testNames) {v -> buffer.writeString(v)}
            buffer.writeList(value.testGroups) {v -> buffer.writeString(v)}
            buffer.writeList(value.testCategories) {v -> buffer.writeString(v)}
            RdSignal.write(ctx, buffer, value._testResult)
            RdSignal.write(ctx, buffer, value._runResult)
        }

    }
    //fields
    val testResult : ISource<TestResult> get() = _testResult
    val runResult : ISource<RunResult> get() = _runResult

    //initializer
    init {
        bindableChildren.add("testResult" to _testResult)
        bindableChildren.add("runResult" to _runResult)
    }

    //secondary constructor
    constructor(
        testNames : List<String>,
        testGroups : List<String>,
        testCategories : List<String>
    ) : this (
        testNames,
        testGroups,
        testCategories,
        RdSignal<TestResult>(TestResult),
        RdSignal<RunResult>(RunResult)
    )

    //equals trait
    //hash code trait
    //pretty print
    override fun print(printer: PrettyPrinter) {
        printer.println("UnitTestLaunch (")
        printer.indent {
            print("testNames = "); testNames.print(printer); println()
            print("testGroups = "); testGroups.print(printer); println()
            print("testCategories = "); testCategories.print(printer); println()
            print("testResult = "); _testResult.print(printer); println()
            print("runResult = "); _runResult.print(printer); println()
        }
        printer.print(")")
    }
}


enum class UnityEditorState {
    Disconnected,
    Idle,
    Play,
    Refresh;

    companion object { val marshaller = FrameworkMarshallers.enum<UnityEditorState>() }
}


class UnityLogModelInitialized private constructor(
    private val _log : RdSignal<RdLogEvent>
) : RdBindableBase() {
    //companion

    companion object : IMarshaller<UnityLogModelInitialized> {
        override val _type: Class<UnityLogModelInitialized> = UnityLogModelInitialized::class.java

        @Suppress("UNCHECKED_CAST")
        override fun read(ctx: SerializationCtx, buffer: AbstractBuffer): UnityLogModelInitialized {
            val _id = RdId.read(buffer)
            val _log = RdSignal.read(ctx, buffer, RdLogEvent)
            return UnityLogModelInitialized(_log).withId(_id)
        }

        override fun write(ctx: SerializationCtx, buffer: AbstractBuffer, value: UnityLogModelInitialized) {
            value.rdid.write(buffer)
            RdSignal.write(ctx, buffer, value._log)
        }

    }
    //fields
    val log : ISource<RdLogEvent> get() = _log

    //initializer
    init {
        bindableChildren.add("log" to _log)
    }

    //secondary constructor
    constructor(
    ) : this (
        RdSignal<RdLogEvent>(RdLogEvent)
    )

    //equals trait
    //hash code trait
    //pretty print
    override fun print(printer: PrettyPrinter) {
        printer.println("UnityLogModelInitialized (")
        printer.indent {
            print("log = "); _log.print(printer); println()
        }
        printer.print(")")
    }
}
