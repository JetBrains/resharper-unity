package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.yaml

import com.jetbrains.rider.ideaInterop.fileTypes.RiderLanguageFileTypeBase
import com.jetbrains.rider.plugins.unity.UnityBundle
import icons.UnityIcons

object UnityYamlFileType : RiderLanguageFileTypeBase(UnityYamlLanguage) {
    override fun getDefaultExtension() = "unity"
    override fun getDescription() = UnityBundle.message("label.unity.asset.file")
    override fun getIcon() = UnityIcons.FileTypes.UnityYaml
    override fun getName() = "UnityYaml"
}

