package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.cg

import com.jetbrains.rider.ideaInterop.fileTypes.RiderDummySyntaxHighlighter

class CgSyntaxHighlighter : RiderDummySyntaxHighlighter(CgLanguage)

/*class CgSyntaxHighlighter : RiderTableBasedSyntaxHighlighter(keywords.table) {
    companion object {
        val keywords = CgKeywords(true)
    }

    override fun getTokenHighlights(tokenType: IElementType): Array<TextAttributesKey> {
        if (keywords.tokenToHighlightMap.containsKey(tokenType))
            return pack(keywords.tokenToHighlightMap[tokenType])
        else
            return super.getTokenHighlights(tokenType)
    }
}*/

