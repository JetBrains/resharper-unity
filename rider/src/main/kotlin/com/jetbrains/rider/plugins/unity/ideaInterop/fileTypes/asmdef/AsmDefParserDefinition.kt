package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.asmdef

import com.intellij.lexer.DummyLexer
import com.intellij.openapi.project.Project
import com.intellij.psi.tree.IElementType
import com.jetbrains.rider.ideaInterop.fileTypes.RiderFileElementType
import com.jetbrains.rider.ideaInterop.fileTypes.RiderParserDefinitionBase

class AsmDefParserDefinition : RiderParserDefinitionBase(AsmDefFileElementType, AsmDefFileType) {
    companion object {
        val AsmDefElementType = IElementType("RIDER_ASMDEF", AsmDefLanguage)
        val AsmDefFileElementType = RiderFileElementType("RIDER_ASMDEF_FILE", AsmDefLanguage, AsmDefElementType)
    }

    override fun createLexer(project: Project?) = DummyLexer(AsmDefFileElementType)
}