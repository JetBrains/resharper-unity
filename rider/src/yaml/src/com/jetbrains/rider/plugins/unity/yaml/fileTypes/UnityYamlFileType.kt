package com.jetbrains.rider.plugins.unity.yaml.fileTypes

import com.jetbrains.rider.ideaInterop.fileTypes.RiderLanguageFileTypeBase
import icons.UnityIcons
import javax.swing.Icon

object UnityYamlFileType : RiderLanguageFileTypeBase(UnityYamlLanguage) {
    override fun getDefaultExtension(): String = "unity"
    override fun getDescription(): String = UnityYamlBundle.message("label.unity.asset.file")
    override fun getIcon(): Icon = UnityIcons.FileTypes.UnityYaml
    override fun getName(): String = "UnityYaml"
}

