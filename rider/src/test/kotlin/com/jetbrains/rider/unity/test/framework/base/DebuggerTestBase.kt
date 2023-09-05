package com.jetbrains.rider.unity.test.framework.base

import com.intellij.xdebugger.XDebuggerManager
import com.intellij.xdebugger.breakpoints.XBreakpointProperties
import com.intellij.xdebugger.breakpoints.XLineBreakpoint
import com.jetbrains.rider.debugger.breakpoint.DotNetLineBreakpointProperties
import com.jetbrains.rider.plugins.unity.debugger.breakpoints.UnityPausepointBreakpointType
import com.jetbrains.rider.plugins.unity.debugger.breakpoints.convertToLineBreakpoint
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.framework.combine
import com.jetbrains.rider.test.scriptingApi.*
import com.jetbrains.rider.unity.test.framework.api.*
import org.testng.annotations.AfterMethod
import org.testng.annotations.Test
import java.io.File

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
abstract class DebuggerTestBase : IntegrationTestWithGeneratedSolutionBase() {
    override fun getSolutionDirectoryName() = "SimpleUnityProjectWithoutPlugin"

    override fun preprocessTempDirectory(tempDir: File) {
        super.preprocessTempDirectory(tempDir)

        val newBehaviourScript = "NewBehaviourScript.cs"
        val sourceScript = testCaseSourceDirectory.resolve(newBehaviourScript)
        if (sourceScript.exists()) {
            sourceScript.copyTo(tempDir.resolve("Assets").resolve(newBehaviourScript), true)
        }
    }

    @Test(enabled = false)
    fun checkUnityPausePoint() {
        val pauseFile = activeSolutionDirectory.combine("Assets", ".pause").absoluteFile
        attachDebuggerToUnityEditorAndPlay(
            {
                toggleUnityPausepoint(project, "NewBehaviourScript.cs", 9, "System.IO.File.Exists(\"${pauseFile.path}\")")
            },
            {
                waitForUnityEditorPlayMode()
                pauseFile.createNewFile()
                waitForUnityEditorPauseMode()
                removeAllUnityPausepoints()
                unpause()
            })
    }

    @Test
    fun checkBreakpoint() {
        attachDebuggerToUnityEditorAndPlay(
            {
                toggleBreakpoint("NewBehaviourScript.cs", 9)
            },
            {
                waitForPause()
                dumpFullCurrentData()
                resumeSession()
            }, testGoldFile)
    }

    @Test(description = "RIDER-24651")
    fun checkExceptionBreakpointWithJustMyCode() {
        attachDebuggerToUnityEditorAndPlay(
            {
                toggleExceptionBreakpoint("System.Exception").justMyCode = true
            },
            {
                waitForPause()
                dumpFullCurrentData()
                resumeSession()
            }, testGoldFile)
    }

    @Test(description = "RIDER-23087", enabled = false)
    fun checkEvaluationAfterRestartGame() {
        var breakpoint: XLineBreakpoint<out XBreakpointProperties<*>>? = null
        attachDebuggerToUnityEditorAndPlay(
            {
                breakpoint = toggleBreakpoint(project, "NewBehaviourScript.cs", 9)
            },
            {
                val toEvaluate = "binaryNotation / 25"

                waitForPause()
                printlnIndented("$toEvaluate = ${evaluateExpression(toEvaluate).result}")
                dumpFullCurrentData()

                breakpoint?.isEnabled = false
                resumeSession()
                waitForUnityEditorPlayMode()
                restart()
                breakpoint?.isEnabled = true

                waitForPause()
                printlnIndented("$toEvaluate = ${evaluateExpression(toEvaluate).result}")
                dumpFullCurrentData()
            }, testGoldFile)
    }

    @AfterMethod(alwaysRun = true)
    fun removeAllUnityPausepoints() {
        XDebuggerManager.getInstance(project).breakpointManager.allBreakpoints.filter {
            it.type is UnityPausepointBreakpointType
        }.forEach {
            @Suppress("UNCHECKED_CAST")
            convertToLineBreakpoint(project, it as XLineBreakpoint<DotNetLineBreakpointProperties>)
        }
        removeAllBreakpoints()
    }
}