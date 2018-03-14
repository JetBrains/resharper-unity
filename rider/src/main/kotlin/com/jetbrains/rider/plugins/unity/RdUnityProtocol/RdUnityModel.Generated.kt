@file:Suppress("PackageDirectoryMismatch", "UnusedImport", "unused", "LocalVariableName")
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



class RdUnityModel private constructor(
    private val _data : RdMap<String, String>
) : RdExtBase() {
    //companion
    
    companion object : ISerializersOwner {
        
        override fun registerSerializersCore(serializers : ISerializers) {
        }
        
        
        
    }
    override val serializersOwner : ISerializersOwner get() = RdUnityModel
    override val serializationHash : Long get() = -8346968635933216692L
    
    //fields
    val data : IMutableViewableMap<String, String> get() = _data
    
    //initializer
    init {
        _data.optimizeNested = true
    }
    
    init {
        bindableChildren.add("data" to _data)
    }
    
    //secondary constructor
    internal constructor(
    ) : this (
        RdMap<String, String>(FrameworkMarshallers.String, FrameworkMarshallers.String)
    )
    
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
val Solution.rdUnityModel get() = getOrCreateExtension("rdUnityModel", ::RdUnityModel)

