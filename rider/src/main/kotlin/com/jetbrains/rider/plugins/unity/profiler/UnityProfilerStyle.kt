package com.jetbrains.rider.plugins.unity.profiler

import com.intellij.ui.Gray
import com.intellij.ui.JBColor
import java.awt.Color

object UnityProfilerStyle {
    // Gutter Line Markers
    val markerHotBackground: JBColor = JBColor(Color(255, 200, 200), Color(80, 30, 30))
    val markerHotBorder: JBColor = JBColor(Color(255, 100, 100), Color(160, 50, 50))
    val markerColdBackground: JBColor = JBColor(Gray._245, Color(55, 57, 59))
    val markerColdBorder: JBColor = JBColor(Gray._210, Color(85, 87, 89))

    // Chart
    val chartBackground: JBColor = JBColor(Color(235, 243, 255), Color(40, 45, 50))
    val chartLine: JBColor = JBColor(Color(64, 128, 255), Color(80, 140, 255))
    const val chartLineThickness: Float = 1f
    val chartFill: JBColor = JBColor(Color(64, 128, 255, 30), Color(80, 140, 255, 30))
    val chartGrid: JBColor = JBColor(Color(64, 128, 255, 25), Color(80, 140, 255, 25))
    val chartSelection: JBColor = JBColor(Color(250, 170 , 13 ,255), Color(250, 170 , 13 ,255))
    
    val gridLabelForeground: Color = JBColor.foreground()
}
