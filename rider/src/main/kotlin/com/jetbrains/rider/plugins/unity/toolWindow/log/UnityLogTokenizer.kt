package com.jetbrains.rider.plugins.unity.toolWindow.log

import com.intellij.openapi.util.NlsSafe
import java.awt.Color
import java.util.*

class UnityLogTokenizer {

    private val validTokens = mapOf("<b>" to UnityLogTokenType.Bold,
        "</b>" to UnityLogTokenType.BoldEnd,
        "<i>" to UnityLogTokenType.Italic,
        "</i>" to UnityLogTokenType.ItalicEnd,
        "<color=*>" to UnityLogTokenType.Color,
        "</color>" to UnityLogTokenType.ColorEnd,
        "<size=*>" to UnityLogTokenType.Size,
        "</size>" to UnityLogTokenType.SizeEnd,
        "<material=*>" to UnityLogTokenType.Material,
        "</material>" to UnityLogTokenType.MaterialEnd,
        "<quad=*>" to UnityLogTokenType.Quad)

    private val startToEndMapping = mapOf(UnityLogTokenType.Bold to UnityLogTokenType.BoldEnd,
        UnityLogTokenType.Italic to UnityLogTokenType.ItalicEnd,
        UnityLogTokenType.Color to UnityLogTokenType.ColorEnd,
        UnityLogTokenType.Size to UnityLogTokenType.SizeEnd,
        UnityLogTokenType.Material to UnityLogTokenType.MaterialEnd)

    fun tokenize(fullString: String): List<Token> {
        val tokens: MutableList<Token> = mutableListOf()

        var lastTokenIndex = 0

        for ((i) in fullString.withIndex()) {
            for (validToken in validTokens) {
                val lastIndex = checkToken(fullString, validToken.key, i)
                if (lastIndex != -1) {
                    addTokens(i, lastTokenIndex, tokens, fullString, fullString.substring(i, lastIndex + 1), validToken.value)

                    lastTokenIndex = lastIndex + 1
                    continue
                }
            }
        }

        tokens.add(Token(fullString.substring(lastTokenIndex), UnityLogTokenType.String))

        generateStyles(tokens)

        return tokens
    }

    private fun generateStyles(tokens: MutableList<Token>) {
        for ((i, token) in tokens.withIndex()) {
            if (token.type == UnityLogTokenType.Bold) {
                for (x in i until tokens.count()) {
                    if (tokens[x].type == UnityLogTokenType.BoldEnd && !tokens[x].used) {
                        token.used = true
                        tokens[x].used = true

                        for (y in i until x) {
                            tokens[y].bold = true
                        }
                        break
                    }
                }
            }
            else if (token.type == UnityLogTokenType.Italic) {
                for (x in i until tokens.count()) {
                    if (tokens[x].type == UnityLogTokenType.ItalicEnd && !tokens[x].used) {
                        token.used = true
                        tokens[x].used = true

                        for (y in i until x) {
                            tokens[y].italic = true
                        }
                        break
                    }
                }
            }
            else if (token.type == UnityLogTokenType.Color) {
                colorizeTokens(i, tokens, token)
            }
            else if (token.type == UnityLogTokenType.Quad) {
                token.used = true
            }
            else if (validTokens.containsValue(token.type)) {
                if (!startToEndMapping.containsKey(token.type))
                    continue

                val endToken = startToEndMapping[token.type]
                for (x in i until tokens.count()) {
                    if (tokens[x].type == endToken && !tokens[x].used) {
                        token.used = true
                        tokens[x].used = true
                    }
                }
            }
        }
    }

    private fun colorizeTokens(i: Int,
                               tokens: MutableList<Token>,
                               token: Token) {
        for (x in i+1 until tokens.count()) {
            if (tokens[x].type == UnityLogTokenType.Color && !tokens[x].used) {
                colorizeTokens(x, tokens, tokens[x])
            }

            if (tokens[x].type == UnityLogTokenType.ColorEnd && !tokens[x].used) {
                token.used = true
                tokens[x].used = true

                val color = this.parseColor(getTokenValue(token.token))
                for (y in i until x) {
                    if (tokens[y].color == null)
                        tokens[y].color = color
                }
                break
            }
        }
    }

    private fun addTokens(i: Int, lastTokenIndex: Int, tokens: MutableList<Token>, fullString: String, tokenString: String, type: UnityLogTokenType) {
        if (i > lastTokenIndex)
            tokens.add(Token(fullString.substring(lastTokenIndex, i), UnityLogTokenType.String))
        tokens.add(Token(tokenString, type))
    }

    private fun getTokenValue(tokenString: String) : String
    {
        if(!tokenString.contains('='))
            return ""

        val cleanedToken = tokenString.replace(">", "")
        return cleanedToken.substring(cleanedToken.indexOf('=') + 1).trim('"')
    }

    private fun checkToken(fullString: String, expectedToken: String, startIndex: Int): Int {
        var expectedTokenIndex = 0

        for (i in startIndex until fullString.length) {
            val currentChar = fullString[i].lowercaseChar()
            val expectedChar = expectedToken[expectedTokenIndex].lowercaseChar()

            if (expectedChar == '*') {
                if (currentChar == expectedToken[expectedTokenIndex + 1].lowercaseChar()) {
                    expectedTokenIndex++
                } else {
                    continue
                }
            } else if (currentChar != expectedChar) {
                return -1
            }

            if (expectedTokenIndex == expectedToken.length - 1) {
                return i
            }

            expectedTokenIndex++
        }

        return -1
    }

    private fun parseColor(color: String): Color? {
        try {
            when (color.lowercase(Locale.getDefault())) {
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
            return null
        }
    }

    data class Token(
        @NlsSafe
        val token: String,
        val type: UnityLogTokenType,
        var bold: Boolean = false,
        var italic: Boolean = false,
        var used: Boolean = false,
        var color: Color? = null
    )
}

