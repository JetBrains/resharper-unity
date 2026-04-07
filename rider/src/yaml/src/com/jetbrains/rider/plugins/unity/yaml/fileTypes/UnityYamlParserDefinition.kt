package com.jetbrains.rider.plugins.unity.yaml.fileTypes

import com.intellij.lexer.DummyLexer
import com.intellij.openapi.project.Project
import com.intellij.psi.tree.IElementType
import com.jetbrains.rider.ideaInterop.fileTypes.RiderFileElementType
import com.jetbrains.rider.ideaInterop.fileTypes.RiderParserDefinitionBase

class UnityYamlParserDefinition : RiderParserDefinitionBase(UnityYamlFileElementType, UnityYamlFileType) {
    companion object {
        val UnityYamlElementType: IElementType = IElementType("RIDER_UNITY_YAML", UnityYamlLanguage)
        val UnityYamlFileElementType: RiderFileElementType = RiderFileElementType("RIDER_UNITY_YAML_FILE", UnityYamlLanguage, UnityYamlElementType)
    }

    override fun createLexer(project: Project?): DummyLexer = DummyLexer(UnityYamlFileElementType)
}