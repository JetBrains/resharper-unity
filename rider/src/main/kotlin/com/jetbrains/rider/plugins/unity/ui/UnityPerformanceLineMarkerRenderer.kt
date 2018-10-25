package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.editor.Editor
import com.intellij.openapi.editor.markup.LineMarkerRendererEx
import java.awt.Color
import java.awt.Graphics
import java.awt.Rectangle

class UnityPerformanceLineMarkerRenderer(private val color: Color,
                                    private val thickness: Int = 1,
                                    private val _position: LineMarkerRendererEx.Position = LineMarkerRendererEx.Position.RIGHT) : LineMarkerRendererEx {
    override fun getPosition() = _position

    override fun paint(editor: Editor, g: Graphics, r: Rectangle) {
        val height = r.height
        g.color = color
        g.fillRect(r.x, r.y, thickness, height)
    }
}