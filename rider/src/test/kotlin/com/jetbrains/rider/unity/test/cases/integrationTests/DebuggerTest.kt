package com.jetbrains.rider.unity.test.cases.integrationTests

import com.intellij.xdebugger.XDebuggerManager
import com.intellij.xdebugger.XDebuggerUtil
import com.intellij.xdebugger.breakpoints.SuspendPolicy
import com.intellij.xdebugger.breakpoints.XBreakpointProperties
import com.intellij.xdebugger.breakpoints.XLineBreakpoint
import com.jetbrains.rider.debugger.breakpoint.DotNetLineBreakpointType
import com.jetbrains.rider.plugins.unity.debugger.breakpoints.UnityPausepointBreakpointType
import com.jetbrains.rider.plugins.unity.debugger.breakpoints.addUnityPausepoint
import com.jetbrains.rider.projectView.solutionDirectoryPath
import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.annotations.UnityTestSettings
import com.jetbrains.rider.test.annotations.report.ChecklistItems
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.annotations.report.Severity
import com.jetbrains.rider.test.annotations.report.SeverityLevel
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.enums.TuanjieVersion
import com.jetbrains.rider.test.enums.UnityVersion
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.scriptingApi.dumpFullCurrentData
import com.jetbrains.rider.test.scriptingApi.evaluateExpression
import com.jetbrains.rider.test.scriptingApi.getVirtualFileFromPath
import com.jetbrains.rider.test.scriptingApi.removeAllBreakpoints
import com.jetbrains.rider.test.scriptingApi.resumeSession
import com.jetbrains.rider.test.scriptingApi.stepInto
import com.jetbrains.rider.test.scriptingApi.stepOver
import com.jetbrains.rider.test.scriptingApi.toggleBreakpoint
import com.jetbrains.rider.test.scriptingApi.toggleSystemExceptionBreakpoint
import com.jetbrains.rider.test.scriptingApi.waitForPause
import com.jetbrains.rider.unity.test.framework.api.attachDebuggerToUnityEditorAndPlay
import com.jetbrains.rider.unity.test.framework.api.removeAllUnityPausepoints
import com.jetbrains.rider.unity.test.framework.api.restart
import com.jetbrains.rider.unity.test.framework.api.toggleUnityPausepoint
import com.jetbrains.rider.unity.test.framework.api.unpause
import com.jetbrains.rider.unity.test.framework.api.waitForUnityEditorPauseMode
import com.jetbrains.rider.unity.test.framework.api.waitForUnityEditorPlayMode
import com.jetbrains.rider.unity.test.framework.base.IntegrationTestWithUnityProjectBase
//import intellij.rider.plugins.unity.debugger.textureVisualizer.RiderTextureDataApi
//import intellij.rider.plugins.unity.debugger.textureVisualizer.UnityTextureInfo
//import intellij.rider.plugins.unity.debugger.textureVisualizer.frontend.UnityTextureHyperLink
//import intellij.rider.plugins.unity.debugger.textureVisualizer.frontend.UnityTextureLinkProvider
import org.testng.annotations.AfterMethod
import org.testng.annotations.Test
import kotlin.test.assertEquals
import kotlin.test.fail

@Subsystem(SubsystemConstants.UNITY_DEBUG)
@Feature("Debug Unity Editor")
@Severity(SeverityLevel.CRITICAL)
@Solution("UnityDebugAndUnitTesting/Project")
abstract class DebuggerTest() : IntegrationTestWithUnityProjectBase() {
    @Test(description = "Check 2 breakpoints in simple Unity App")
    @ChecklistItems(["Breakpoints/Simple breakpoint"])
    fun checkBreakpoint() {
        attachDebuggerToUnityEditorAndPlay(
            {
                toggleBreakpoint("NewBehaviourScript.cs", 8) //Debug.Log("Start");
                toggleBreakpoint("NewBehaviourScript.cs", 15) // int binaryNotation = 0b_0001_1110_1000_0100_1000_0000;
            },
            {
                waitForPause()
                dumpFullCurrentData()
                resumeSession()
                waitForPause()
                dumpFullCurrentData()
                resumeSession()
            }, testGoldFile)
    }

    @Test(description = "Check texture debugging in simple Unity App")
    @ChecklistItems(["Breakpoints/Texture breakpoint"])
    @Mute("RIDER-135697 Unity test for texture visualizer isn't compatible with the new split mode debugger yet")
    fun checkTextureDebugging() {
        fail("Not supported, see RIDER-135697")
//        attachDebuggerToUnityEditorAndPlay(
//            beforeRun = {
//                toggleBreakpoint("TextureDebuggingScript.cs", 13) //  Debug.Log(texture2D);
//            },
//            test = {
//                waitForPause()
//                dumpFullCurrentData()
//                try {
//                    val stackFrame = (session.currentStackFrame as DotNetStackFrame)
//
//                    assertNotNull(stackFrame)
//
//                    val valueNode = evaluateNode("texture2D", refreshIfNeeded = true)
//                    val EP_NAME = ExtensionPointName<XDebuggerNodeLinkActionProvider>("com.intellij.xdebugger.nodeLinkActionProvider")
//                    val unityTextureExtension = EP_NAME.extensionList.firstOrNull { it is UnityTextureLinkProvider }
//                    assertNotNull(unityTextureExtension)
//
//                    var textureInfo: UnityTextureInfo? = null
//                    val job = (project as ComponentManagerEx).getCoroutineScope().launch {
//                        val link = with (unityTextureExtension) {
//                            provideHyperlink(project, valueNode)
//                        } as? UnityTextureHyperLink
//                        assertNotNull(link)
//                        val result = RiderTextureDataApi.getInstance().evaluateTexture(link.accessorId, 10000)
//                        textureInfo = result.unityTextureInfo
//                    }
//
//                    pumpMessages(DebugTestExecutionContext.waitForStopTimeout) {
//                        job.isCompleted
//                    }
//                    assertNotNull(textureInfo)
//                    printlnIndented(textureInfo.printToString())
//                }
//                finally {
//                }
//            }, goldFile = testGoldFile)
    }

    @Test(description = "Check Unity pause point in debugging for simple Unity App")
    @ChecklistItems(["Breakpoints/Unity Pause Points"])
    fun checkUnityPausePoint() {
        attachDebuggerToUnityEditorAndPlay(
            test = {
                waitForUnityEditorPlayMode()
                toggleUnityPausepoint(project, "NewBehaviourScript.cs", 14) //  int binaryNotation = 0b_0001_1110_1000_0100_1000_0000;
                waitForUnityEditorPauseMode()
                removeAllUnityPausepoints()
                unpause()
            })
    }

    @Test(description = "Check exception breakpoint with 'Just My Code' for simple Unity App. RIDER-24651")
    @ChecklistItems(["Breakpoints/Exception Breakpoint"])
    fun checkExceptionBreakpointWithJustMyCode() {
        attachDebuggerToUnityEditorAndPlay(
            {
                toggleSystemExceptionBreakpoint(breakIfHandledByUserCode = true,
                                                breakIfHandledByOtherCode = false,
                                                breakIfThrownInExternalCode = false,
                                                breakIfThrownInUserCode = true)
            },
            {
                waitForPause()
                dumpFullCurrentData()
                resumeSession()
            }, testGoldFile)
    }

    @Test(description = "Check evaluation after restarting the game. RIDER-23087")
    @ChecklistItems(["Evaluation/Evaluation After Restart Game"])
    fun checkEvaluationAfterRestartGame() {
        var breakpoint: XLineBreakpoint<out XBreakpointProperties<*>>? = null
        attachDebuggerToUnityEditorAndPlay(
            {
                breakpoint = toggleBreakpoint(project, "NewBehaviourScript.cs", 15) //  Debug.Log(binaryNotation);
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
                resumeSession()
            }, testGoldFile)
    }

    @Test(description = "Simple Stepping test")
    @ChecklistItems(["Stepping/Simple Stepping"])
    fun checkSimpleStepping() {
        var breakpoint: XLineBreakpoint<out XBreakpointProperties<*>>? = null
        attachDebuggerToUnityEditorAndPlay(
            {
                breakpoint = toggleBreakpoint(project, "NewBehaviourScript.cs", 14) // int binaryNotation = 0b_0001_1110_1000_0100_1000_0000;
            },
            {
                val toEvaluate = "binaryNotation / 25"
                waitForPause()
                stepInto()
                printlnIndented("$toEvaluate = ${evaluateExpression(toEvaluate).result}")
                dumpFullCurrentData()
                stepOver()
                printlnIndented("$toEvaluate = ${evaluateExpression(toEvaluate).result}")
                dumpFullCurrentData()
                resumeSession()
            }, testGoldFile)
    }

    @Test(description = "Check simple evaluation")
    @ChecklistItems(["Evaluation/Simple evaluation"])
    fun checkSimpleEvaluation() {
        var breakpoint: XLineBreakpoint<out XBreakpointProperties<*>>? = null
        attachDebuggerToUnityEditorAndPlay(
            {
                breakpoint = toggleBreakpoint(project, "NewBehaviourScript.cs", 15) //  Debug.Log(binaryNotation);
            },
            {
                val toEvaluate = "binaryNotation / 25"

                waitForPause()
                printlnIndented("$toEvaluate = ${evaluateExpression(toEvaluate).result}")
                dumpFullCurrentData()
                resumeSession()
            }, testGoldFile)
    }
    
    @Test(description = "Regression: addUnityPausepoint creates a pausepoint and never a line breakpoint")
    @ChecklistItems(["Breakpoints/Unity Pause Points"])
    fun checkAddUnityPausepointNeverCreatesLineBreakpoint() {
        val breakpointManager = XDebuggerManager.getInstance(project).breakpointManager
        val pausepointType = XDebuggerUtil.getInstance()
            .findBreakpointType(UnityPausepointBreakpointType::class.java)
        val lineBreakpointType = XDebuggerUtil.getInstance()
            .findBreakpointType(DotNetLineBreakpointType::class.java)

        val pausepointsBefore = breakpointManager.getBreakpoints(pausepointType).size
        val lineBreakpointsBefore = breakpointManager.getBreakpoints(lineBreakpointType).size

        val testFile = getVirtualFileFromPath("NewBehaviourScript.cs", project.solutionDirectoryPath)
        val pausepoint = addUnityPausepoint(project, testFile.url, line = 0)

        assertEquals(pausepointsBefore + 1, breakpointManager.getBreakpoints(pausepointType).size)
        assertEquals(lineBreakpointsBefore, breakpointManager.getBreakpoints(lineBreakpointType).size)
        assertEquals(SuspendPolicy.NONE, pausepoint.suspendPolicy)

        removeAllUnityPausepoints()
        assertEquals(0, breakpointManager.getBreakpoints(pausepointType).size)
    }

    @AfterMethod
    fun clearAllBreakpoints() {
        removeAllBreakpoints()
    }
}

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@UnityTestSettings(unityVersion = UnityVersion.V2022)
class DebuggerTestUnity2022 : DebuggerTest()

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@UnityTestSettings(unityVersion = UnityVersion.V6)
class DebuggerTestUnity6 : DebuggerTest()

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@UnityTestSettings(unityVersion = UnityVersion.V6_2)
class DebuggerTestUnity6_2 : DebuggerTest()

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@UnityTestSettings(unityVersion = UnityVersion.V6_3)
class DebuggerTestUnity6_3 : DebuggerTest()

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@Mute("RIDER-113191")
@Solution("TuanjieDebugAndUnitTesting/Project")
@UnityTestSettings(tuanjieVersion = TuanjieVersion.V2022)
class DebuggerTestTuanjie2022 : DebuggerTest ()