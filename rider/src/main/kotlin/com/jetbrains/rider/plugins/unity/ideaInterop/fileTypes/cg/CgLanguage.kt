package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.cg

import com.jetbrains.rider.ideaInterop.fileTypes.RiderLanguageBase

object CgLanguage : RiderLanguageBase("Cg", "CG") {
    override fun isCaseSensitive(): Boolean = true
}
