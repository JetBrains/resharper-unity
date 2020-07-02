package com.jetbrains.rider.plugins.unity.spellchecker

import com.intellij.spellchecker.BundledDictionaryProvider

class UnityBundledDictionaryProvider : BundledDictionaryProvider {
    override fun getBundledDictionaries(): Array<String> = arrayOf("unity.dic")
}