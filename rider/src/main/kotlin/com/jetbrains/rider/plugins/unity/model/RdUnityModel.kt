package com.jetbrains.rider.plugins.unity.model

import com.jetbrains.rider.generator.nova.*
import com.jetbrains.rider.model.nova.ide.*


object RdUnityModel : Ext(IdeRoot) {
    init {
        map("data", PredefinedType.string, PredefinedType.string)
    }
}