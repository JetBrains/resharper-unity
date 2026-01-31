package com.jetbrains.rider.unity.test.cases.documentModel

import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.report.ChecklistItems
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.annotations.report.Severity
import com.jetbrains.rider.test.annotations.report.SeverityLevel
import com.jetbrains.rider.test.base.PerTestSolutionTestBase
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.scriptingApi.checkCrumbs
import com.jetbrains.rider.test.scriptingApi.setCaretToPosition
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import org.testng.annotations.Test

@Subsystem(SubsystemConstants.UNITY_SHADERS)
@Feature("Breadcrumbs in Shader files")
@ChecklistItems(["Breadcrumbs in .shader"])
@Severity(SeverityLevel.NORMAL)
@Solution("SimpleUnityProjectWithShaders")
class BreadcrumbsTest : PerTestSolutionTestBase() {
    @Test(description = "Test Breadcrumbs in .shader files")
    fun simpleBreadcrumbsCheck() {
        withOpenedEditor("Assets/Shaders/MyShader.shader") {
            setCaretToPosition(12, 20)

            checkCrumbs("Shader \"MyShader\"", "SubShader", "Pass", "CGPROGRAM", "hsv2rgb")
        }
    }
}