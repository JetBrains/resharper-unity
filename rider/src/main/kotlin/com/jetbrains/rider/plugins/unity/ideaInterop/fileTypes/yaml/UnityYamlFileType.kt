package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.yaml

import com.jetbrains.rider.ideaInterop.fileTypes.RiderLanguageFileTypeBase
import icons.UnityIcons

object UnityYamlFileType : RiderLanguageFileTypeBase(UnityYamlLanguage) {
    override fun getDefaultExtension() = "unity"
    override fun getDescription() = "Unity YAML file"
    override fun getIcon() = UnityIcons.FileTypes.UnityYaml
    override fun getName() = "Unity YAML"
}

