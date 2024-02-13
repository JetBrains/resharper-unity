package com.jetbrains.rider.plugins.unity.csharp.completion

import com.intellij.openapi.project.Project
import com.intellij.psi.PsiElement
import com.jetbrains.rider.ideaInterop.fileTypes.csharp.completion.CSharpIdentifierPartHelper
import com.jetbrains.rider.languages.fileTypes.csharp.kotoparser.parser.CsAttributeDeclarationNode
import com.jetbrains.rider.languages.fileTypes.csharp.psi.impl.CSharpDummyDeclaration
import com.jetbrains.rider.languages.fileTypes.csharp.psi.impl.CSharpNonInterpolatedStringLiteralExpressionImpl
import com.jetbrains.rider.languages.fileTypes.csharp.psi.impl.CSharpPsiElementBase
import com.jetbrains.rider.plugins.unity.FrontendBackendHost
import com.jetbrains.rider.plugins.unity.isUnityProject

class OdinLayoutAttributeCSharpIdentifierPartHelper : CSharpIdentifierPartHelper {
    private val knownAttributes = setOf(
        "BoxGroup",
        "ButtonGroup",
        "HorizontalGroup",
        "ResponsiveButtonGroup",
        "VerticalGroup",
        "FoldoutGroup",
        "TabGroup",
        "ToggleGroup",
        "TitleGroup",
        "HideIfGroup",
        "ShowIfGroup",
        )

    private fun hasOdinPackage(project: Project): Boolean {
        val host = FrontendBackendHost.getInstance(project)

        return host.technologies.contains("Odin")
    }

    override fun isApplicable(file: PsiElement, offset: Int): Boolean {

        if (!file.project.isUnityProject())
            return false

        if (!hasOdinPackage(file.project))
            return false

        val element = file.findElementAt(offset)?.parent
        if (element !is CSharpPsiElementBase)
            return false

        if (element !is CSharpNonInterpolatedStringLiteralExpressionImpl)
            return false

        val attributeDeclaration = element.parent.parent
        if (attributeDeclaration !is CSharpDummyDeclaration)
            return false

        if (attributeDeclaration.astNodeType !is CsAttributeDeclarationNode)
            return false

        val name = attributeDeclaration.declaredName
        return knownAttributes.contains(name) || knownAttributes.contains(name + "Attribute")
    }

    override fun acceptCharForIdentifierPart(character: Char): Boolean {
        return character == '/'
    }

    override fun acceptCharForIdentifierStart(character: Char): Boolean {
        return acceptCharForIdentifierPart(character)
    }
}