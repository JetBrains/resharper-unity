package com.jetbrains.rider.unity.test.cases.documentModel

import com.jetbrains.rider.test.allure.SubsystemConstants
import com.jetbrains.rider.test.annotations.Feature
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.scriptingApi.checkCrumbs
import com.jetbrains.rider.test.scriptingApi.setCaretToPosition
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import io.qameta.allure.*
import org.testng.annotations.Test

@Test
@Subsystem(SubsystemConstants.UNITY_SHADERS)
@Feature("Breadcrumbs in Shader files")
@Severity(SeverityLevel.NORMAL)
class BreadcrumbsTest : BaseTestWithSolution() {
    override fun getSolutionDirectoryName() = "SimpleUnityProjectWithShaders"

    @Test(description="Test Breadcrumbs in .shader files")
    fun simpleCheck() {
        withOpenedEditor(project, "Assets/Shaders/MyShader.shader") {
            setCaretToPosition(12, 20)

            checkCrumbs("Shader \"MyShader\"", "SubShader", "Pass", "CGPROGRAM", "hsv2rgb")
        }
    }
}