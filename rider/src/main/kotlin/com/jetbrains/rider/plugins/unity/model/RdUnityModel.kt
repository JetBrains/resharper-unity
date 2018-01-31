package com.jetbrains.rider.plugins.unity.model

import com.jetbrains.rider.generator.nova.Ext
import com.jetbrains.rider.generator.nova.PredefinedType
import com.jetbrains.rider.generator.nova.map
import com.jetbrains.rider.model.nova.ide.IdeRoot


object RdUnityModel : Ext(IdeRoot){
    init{
        map("data", PredefinedType.string, PredefinedType.string)}
}