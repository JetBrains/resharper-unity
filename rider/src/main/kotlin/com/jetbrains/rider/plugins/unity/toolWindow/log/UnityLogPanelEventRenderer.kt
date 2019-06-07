package com.jetbrains.rider.plugins.unity.toolWindow.log

import com.intellij.ui.ColoredListCellRenderer
import com.intellij.ui.RowIcon
import com.intellij.ui.SimpleTextAttributes
import com.intellij.ui.speedSearch.SpeedSearchUtil
import java.awt.Color
import javax.swing.JList

class UnityLogPanelEventRenderer : ColoredListCellRenderer<LogPanelItem>() {
    private val validTokens: Map<String, TokenType> = mapOf("<b>" to TokenType.Bold,
                                                            "</b>" to TokenType.BoldEnd,
                                                            "<i>" to TokenType.Italic,
                                                            "</i>" to TokenType.ItalicEnd,
                                                            "</color>" to TokenType.ColorEnd)

    override fun customizeCellRenderer(list: JList<out LogPanelItem>, event: LogPanelItem?, index: Int, selected: Boolean, hasFocus: Boolean) {
        if (event != null) {
            icon = RowIcon(event.type.getIcon(), event.mode.getIcon())
            var countPresentation = ""
            if (event.count > 1)
                countPresentation = " (" + event.count + ")"

            var tokens = tokenize(event.message)

            for ((i, token) in tokens.withIndex()) {
                if (token.type == TokenType.Bold) {
                    for (x in i until tokens.count()) {
                        if (tokens[x].type == TokenType.BoldEnd && !tokens[x].used) {
                            token.used = true
                            tokens[x].used = true

                            for (y in i until x) {
                                tokens[y].bold = true
                            }
                            break
                        }
                    }
                }
                if (token.type == TokenType.Italic) {
                    for (x in i until tokens.count()) {
                        if (tokens[x].type == TokenType.ItalicEnd && !tokens[x].used) {
                            token.used = true
                            tokens[x].used = true

                            for (y in i until x) {
                                tokens[y].italic = true
                            }
                            break
                        }
                    }
                }
                if (token.type == TokenType.Color) {
                    for (x in tokens.count()-1 downTo i) {
                        if (tokens[x].type == TokenType.ColorEnd && !tokens[x].used) {
                            token.used = true
                            tokens[x].used = true

                            var color = this.parseColor(token.token)
                            for (y in i until x) {
                                tokens[y].color = color
                            }
                            break
                        }
                    }
                }
            }

            for (token in tokens) {
                if (!token.used) {
                    var style = SimpleTextAttributes.STYLE_PLAIN
                    if (token.bold)
                        style = style or SimpleTextAttributes.STYLE_BOLD

                    if (token.italic)
                        style = style or SimpleTextAttributes.STYLE_ITALIC

                    append(token.token, SimpleTextAttributes(style, token.color))
                }
            }

            append(countPresentation)
            SpeedSearchUtil.applySpeedSearchHighlighting(list, this, true, selected)
        }
    }

    private fun tokenize(fullString: String): List<Token> {
        var tokens: MutableList<Token> = mutableListOf()

        var lastTokenIndex = 0

        for ((i) in fullString.withIndex()) {
            for (validToken in validTokens) {
                var lastIndex = checkToken(fullString, validToken.key, i)
                if (lastIndex != -1) {
                    addTokens(i, lastTokenIndex, tokens, fullString, fullString.substring(i, lastIndex + 1), validToken.value)

                    lastTokenIndex = lastIndex + 1
                    continue
                }

                lastIndex = checkToken(fullString, "<color=", i)
                if(lastIndex != -1)
                {
                    var token = getToken(fullString, i)
                    if(token == "")
                        break

                    val tempLastTokenIndex = i + token.length
                    token.replace(">", "")

                    addTokens(
                        i,
                        lastTokenIndex,
                        tokens,
                        fullString,
                        token.substring(token.indexOf('=')+1).trim('"'),
                        TokenType.Color
                    )
                    lastTokenIndex = tempLastTokenIndex + 1
                    break
                }
            }
        }

        tokens.add(Token(fullString.substring(lastTokenIndex), TokenType.String))

        return tokens
    }

    private fun addTokens(i: Int, lastTokenIndex: Int, tokens: MutableList<Token>, fullString: String, tokenString: String, type: TokenType) {
        if (i > lastTokenIndex)
            tokens.add(Token(fullString.substring(lastTokenIndex, i), TokenType.String))
        tokens.add(Token(tokenString, type))
    }

    private fun getToken(fullString: String, startIndex: Int) : String
    {
        if(fullString[startIndex] != '<')
            return ""

        for (i in startIndex until fullString.length) {
            if(fullString[i] == '>')
                return fullString.substring(startIndex until i)
        }

        return ""
    }

    private fun checkToken(fullString: String, expectedToken: String, startIndex: Int): Int {
        for (i in startIndex until fullString.length) {
            val expectedChar = expectedToken[i - startIndex]

            if (fullString[i] != expectedChar) {
                return -1
            }

            if (i - startIndex == expectedToken.length - 1) {
                return i
            }
        }

        return -1
    }

    private fun parseColor(color: String): Color? {
        try {
            when (color) {
                "aqua" -> return Color.decode("#00ffff")
                "black" -> return Color.decode("#000000")
                "blue" -> return Color.decode("#0000ff")
                "brown" -> return Color.decode("#a52a2a")
                "cyan" -> return Color.decode("#00ffff")
                "darkblue" -> return Color.decode("#0000a0")
                "fuchsia" -> return Color.decode("#ff00ff")
                "green" -> return Color.decode("#008000")
                "grey" -> return Color.decode("#808080")
                "lightblue" -> return Color.decode("#add8e6")
                "lime" -> return Color.decode("#00ff00")
                "magenta" -> return Color.decode("#ff00ff")
                "maroon" -> return Color.decode("#800000")
                "navy" -> return Color.decode("#000080")
                "olive" -> return Color.decode("#808000")
                "orange" -> return Color.decode("#ffa500")
                "purple" -> return Color.decode("#800080")
                "red" -> return Color.decode("#ff0000")
                "silver" -> return Color.decode("#c0c0c0")
                "teal" -> return Color.decode("#008080")
                "white" -> return Color.decode("#ffffff")
                "yellow" -> return Color.decode("#ffff00")
                else -> return when {
                    color.length == 8 -> Color.decode(color.substring(0..7))
                    color.length == 7 -> Color.decode(color)
                    else -> null
                }
            }
        } catch (t: Throwable) {
            return Color.white
        }
    }

    data class Token(
        val token: String,
        val type: TokenType,
        var bold: Boolean = false,
        var italic: Boolean = false,
        var used: Boolean = false,
        var color: Color? = null
    )

    enum class TokenType {
        String,
        Bold,
        BoldEnd,
        Italic,
        ItalicEnd,
        Color,
        ColorEnd
    }
}