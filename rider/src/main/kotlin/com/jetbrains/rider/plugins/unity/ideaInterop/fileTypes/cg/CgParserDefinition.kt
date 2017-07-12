package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.cg

import com.intellij.lexer.DummyLexer
import com.intellij.openapi.project.Project
import com.intellij.psi.tree.IElementType
import com.jetbrains.rider.ideaInterop.fileTypes.RiderFileElementType
import com.jetbrains.rider.ideaInterop.fileTypes.RiderParserDefinitionBase

class CgParserDefinition : RiderParserDefinitionBase(CgFileElementType, CgFileType) {
    companion object {
        val CgElementType = IElementType("RIDER_CG", CgLanguage)
        val CgFileElementType = RiderFileElementType("RIDER_CG_FILE", CgLanguage, CgElementType)
    }

    override fun createLexer(project: Project?) = DummyLexer(CgFileElementType)
}