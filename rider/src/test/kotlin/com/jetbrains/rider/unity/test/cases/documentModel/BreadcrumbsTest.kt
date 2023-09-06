package com.jetbrains.rider.unity.test.cases.documentModel

import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.scriptingApi.checkCrumbs
import com.jetbrains.rider.test.scriptingApi.setCaretToPosition
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import org.testng.annotations.Test

@Test
class BreadcrumbsTest : BaseTestWithSolution() {
    override fun getSolutionDirectoryName() = "SimpleUnityProjectWithShaders"

    @Test
    fun simpleCheck() {
        withOpenedEditor(project, "Assets/Shaders/MyShader.shader") {
            setCaretToPosition(12, 20)

            checkCrumbs("Shader \"MyShader\"", "SubShader", "Pass", "CGPROGRAM", "hsv2rgb")
        }
    }
}