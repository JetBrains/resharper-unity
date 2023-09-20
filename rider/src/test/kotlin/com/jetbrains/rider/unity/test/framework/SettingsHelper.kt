package com.jetbrains.rider.unity.test.framework

import com.jetbrains.rider.test.framework.combine
import java.io.File

class SettingsHelper {
    companion object{

        private val disableYamlDotSettingsContents = """<wpf:ResourceDictionary xml:space="preserve" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:s="clr-namespace:System;assembly=mscorlib" xmlns:ss="urn:shemas-jetbrains-com:settings-storage-xaml" xmlns:wpf="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
	        <s:Boolean x:Key="/Default/CodeEditing/Unity/IsAssetIndexingEnabled/@EntryValue">False</s:Boolean>
            </wpf:ResourceDictionary>"""
        fun disableIsAssetIndexingEnabledSetting(activeSolution: String, activeSolutionDirectory: File) {
            val dotSettingsFile = activeSolutionDirectory.combine("$activeSolution.sln.DotSettings.user")
            dotSettingsFile.writeText(disableYamlDotSettingsContents)
        }
    }
}