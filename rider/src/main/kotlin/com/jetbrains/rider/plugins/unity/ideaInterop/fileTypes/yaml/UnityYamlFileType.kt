package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.yaml

import com.jetbrains.rider.ideaInterop.fileTypes.RiderLanguageFileTypeBase
import icons.UnityIcons

object UnityYamlFileType : RiderLanguageFileTypeBase(UnityYamlLanguage) {
    override fun getDefaultExtension() = "unity"
    override fun getDescription() = "Unity asset file"
    override fun getIcon() = UnityIcons.FileTypes.UnityYaml
    override fun getName() = "UnityYaml"
}

