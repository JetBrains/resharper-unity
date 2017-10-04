package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.shaderLab

import com.intellij.openapi.editor.colors.TextAttributesKey
import com.intellij.psi.tree.IElementType
import com.jetbrains.rider.ideaInterop.fileTypes.RiderDummySyntaxHighlighter
import com.jetbrains.rider.ideaInterop.fileTypes.RiderTableBasedSyntaxHighlighter
import com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.cg.CgKeywords

class ShaderLabSyntaxHighlighter : RiderDummySyntaxHighlighter(ShaderLabLanguage)

/*class ShaderLabSyntaxHighlighter : RiderTableBasedSyntaxHighlighter(keywords.table) {
    companion object {
        val keywords = CgKeywords(false)
    }

    override fun getTokenHighlights(tokenType: IElementType): Array<TextAttributesKey> {
        if (keywords.tokenToHighlightMap.containsKey(tokenType))
            return pack(keywords.tokenToHighlightMap[tokenType])
        else
            return super.getTokenHighlights(tokenType)
    }
}*/

