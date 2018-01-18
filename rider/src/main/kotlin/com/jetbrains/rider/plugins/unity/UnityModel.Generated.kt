@file:Suppress("PackageDirectoryMismatch", "UnusedImport", "unused")
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



class UnityModel (
    private val _play : RdProperty<Boolean>,
    private val _pause : RdProperty<Boolean>,
    private val _step : RdCall<Unit, Unit>,
    private val _unityPluginVersion : RdProperty<String>,
    private val _riderProcessId : RdProperty<Int>,
    private val _applicationPath : RdProperty<String>,
    private val _applicationVersion : RdProperty<String>,
    private val _logModelInitialized : RdProperty<UnityLogModelInitialized>,
    private val _isClientConnected : RdEndpoint<Unit, Boolean>,
    private val _openFileLineCol : RdEndpoint<RdOpenFileArgs, Boolean>,
    private val _updateUnityPlugin : RdCall<String, Boolean>,
    private val _refresh : RdCall<Unit, Unit>
) : RdBindableBase() {
    //companion
    
    companion object {
        
        public fun register(serializers : ISerializers) {
            if (!serializers.toplevels.add(UnityModel::class.java)) return
            Protocol.initializationLogger.trace { "REGISTER serializers for "+UnityModel::class.java.simpleName }
            serializers.register(RdOpenFileArgs)
            serializers.register(RdLogEvent)
            serializers.register(RdLogEventType.marshaller)
            serializers.register(RdLogEventMode.marshaller)
            serializers.register(UnityLogModelInitialized)
        }
        
        fun create(lifetime : Lifetime, protocol : IProtocol) : UnityModel {
            UnityModel.register(protocol.serializers)
            register(protocol.serializers)
            
            val __res = UnityModel (
                RdProperty<Boolean>(FrameworkMarshallers.Bool).static(1001),
                RdProperty<Boolean>(FrameworkMarshallers.Bool).static(1002),
                RdCall<Unit, Unit>(FrameworkMarshallers.Void, FrameworkMarshallers.Void).static(1003),
                RdProperty<String>(FrameworkMarshallers.String).static(1004),
                RdProperty<Int>(FrameworkMarshallers.Int).static(1005),
                RdProperty<String>(FrameworkMarshallers.String).static(1006),
                RdProperty<String>(FrameworkMarshallers.String).static(1007),
                RdProperty<UnityLogModelInitialized>(UnityLogModelInitialized).static(1008),
                RdEndpoint<Unit, Boolean>(FrameworkMarshallers.Void, FrameworkMarshallers.Bool).static(1009),
                RdEndpoint<RdOpenFileArgs, Boolean>(RdOpenFileArgs, FrameworkMarshallers.Bool).static(1010),
                RdCall<String, Boolean>(FrameworkMarshallers.String, FrameworkMarshallers.Bool).static(1011),
                RdCall<Unit, Unit>(FrameworkMarshallers.Void, FrameworkMarshallers.Void).static(1012))
            __res.bind(lifetime, protocol, UnityModel::class.java.simpleName)
            
            Protocol.initializationLogger.trace { "CREATED toplevel object "+__res.printToString() }
            
            return __res
        }
        
    }
    //fields
    val play : IProperty<Boolean> get() = _play
    val pause : IProperty<Boolean> get() = _pause
    val step : IRdCall<Unit, Unit> get() = _step
    val unityPluginVersion : IProperty<String> get() = _unityPluginVersion
    val riderProcessId : IProperty<Int> get() = _riderProcessId
    val applicationPath : IProperty<String> get() = _applicationPath
    val applicationVersion : IProperty<String> get() = _applicationVersion
    val logModelInitialized : IProperty<UnityLogModelInitialized> get() = _logModelInitialized
    val isClientConnected : RdEndpoint<Unit, Boolean> get() = _isClientConnected
    val openFileLineCol : RdEndpoint<RdOpenFileArgs, Boolean> get() = _openFileLineCol
    val updateUnityPlugin : IRdCall<String, Boolean> get() = _updateUnityPlugin
    val refresh : IRdCall<Unit, Unit> get() = _refresh
    
    //initializer
    init {
        _play.optimizeNested = true
        _pause.optimizeNested = true
        _unityPluginVersion.optimizeNested = true
        _riderProcessId.optimizeNested = true
        _applicationPath.optimizeNested = true
        _applicationVersion.optimizeNested = true
    }
    
    //secondary constructor
    //init method
    override fun init(lifetime: Lifetime) {
        _play.bind(lifetime, this, "play")
        _pause.bind(lifetime, this, "pause")
        _step.bind(lifetime, this, "step")
        _unityPluginVersion.bind(lifetime, this, "unityPluginVersion")
        _riderProcessId.bind(lifetime, this, "riderProcessId")
        _applicationPath.bind(lifetime, this, "applicationPath")
        _applicationVersion.bind(lifetime, this, "applicationVersion")
        _logModelInitialized.bind(lifetime, this, "logModelInitialized")
        _isClientConnected.bind(lifetime, this, "isClientConnected")
        _openFileLineCol.bind(lifetime, this, "openFileLineCol")
        _updateUnityPlugin.bind(lifetime, this, "updateUnityPlugin")
        _refresh.bind(lifetime, this, "refresh")
    }
    //identify method
    override fun identify(ids: IIdentities) {
        _play.identify(ids)
        _pause.identify(ids)
        _step.identify(ids)
        _unityPluginVersion.identify(ids)
        _riderProcessId.identify(ids)
        _applicationPath.identify(ids)
        _applicationVersion.identify(ids)
        _logModelInitialized.identify(ids)
        _isClientConnected.identify(ids)
        _openFileLineCol.identify(ids)
        _updateUnityPlugin.identify(ids)
        _refresh.identify(ids)
    }
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
            print("isClientConnected = "); _isClientConnected.print(printer); println()
            print("openFileLineCol = "); _openFileLineCol.print(printer); println()
            print("updateUnityPlugin = "); _updateUnityPlugin.print(printer); println()
            print("refresh = "); _refresh.print(printer); println()
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
    //init method
    //identify method
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
    //init method
    //identify method
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


class UnityLogModelInitialized (
    private val _log : RdSignal<RdLogEvent>
) : RdBindableBase() {
    //companion
    
    companion object : IMarshaller<UnityLogModelInitialized> {
        override val _type: Class<UnityLogModelInitialized> = UnityLogModelInitialized::class.java
        
        @Suppress("UNCHECKED_CAST")
        override fun read(ctx: SerializationCtx, buffer: AbstractBuffer): UnityLogModelInitialized {
            val _log = RdSignal.read(ctx, buffer, RdLogEvent)
            return UnityLogModelInitialized(_log)
        }
        
        override fun write(ctx: SerializationCtx, buffer: AbstractBuffer, value: UnityLogModelInitialized) {
            RdSignal.write(ctx, buffer, value._log)
        }
        
    }
    //fields
    val log : ISink<RdLogEvent> get() = _log
    
    //initializer
    //secondary constructor
    constructor(
    ) : this (
        RdSignal<RdLogEvent>(RdLogEvent)
    )
    
    //init method
    override fun init(lifetime: Lifetime) {
        _log.bind(lifetime, this, "log")
    }
    //identify method
    override fun identify(ids: IIdentities) {
        _log.identify(ids)
    }
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
