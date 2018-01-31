@file:Suppress("PackageDirectoryMismatch", "UnusedImport", "unused")
package com.jetbrains.rider.model

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



class RdUnityModel (
    private val _data : RdMap<String, String>
) : RdBindableBase() {
    //companion
    
    companion object {
        
        public fun register(serializers : ISerializers) {
            if (!serializers.toplevels.add(RdUnityModel::class.java)) return
            Protocol.initializationLogger.trace { "REGISTER serializers for "+RdUnityModel::class.java.simpleName }
        }
        
        fun create(lifetime : Lifetime, protocol : IProtocol) : RdUnityModel {
            IdeRoot.register(protocol.serializers)
            register(protocol.serializers)
            
            val __res = RdUnityModel (
                RdMap<String, String>(FrameworkMarshallers.String, FrameworkMarshallers.String).static(1001))
            __res.bind(lifetime, protocol, RdUnityModel::class.java.simpleName)
            
            Protocol.initializationLogger.trace { "CREATED toplevel object "+__res.printToString() }
            
            return __res
        }
        
    }
    //fields
    val data : IMutableViewableMap<String, String> get() = _data
    
    //initializer
    init {
        _data.optimizeNested = true
    }
    
    //secondary constructor
    //init method
    override fun init(lifetime: Lifetime) {
        _data.bind(lifetime, this, "data")
    }
    //identify method
    override fun identify(ids: IIdentities) {
        _data.identify(ids)
    }
    //equals trait
    //hash code trait
    //pretty print
    override fun print(printer: PrettyPrinter) {
        printer.println("RdUnityModel (")
        printer.indent {
            print("data = "); _data.print(printer); println()
        }
        printer.print(")")
    }
}
