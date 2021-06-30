package com.jetbrains.rider.plugins.unity.css.uss

import com.intellij.lexer.Lexer
import com.intellij.psi.css.impl.util.CssHighlighter
import com.intellij.psi.css.impl.util.CssHighlighterLexer
import com.jetbrains.rider.plugins.unity.css.uss.codeInsight.css.UssCssElementDescriptorFactory

class UssSyntaxHighlighter: CssHighlighter() {

    override fun getHighlightingLexer(): Lexer {
        // Make sure our property values (enums) are highlighted as "Property Value" instead of plain old "identifier"
        return CssHighlighterLexer(UssCssElementDescriptorFactory.getInstance().getValueIdentifiers())
    }
}