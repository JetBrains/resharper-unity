package com.jetbrains.rider.unity.test.cases.integrationTests

import com.intellij.openapi.components.ComponentManagerEx
import com.intellij.openapi.rd.util.lifetime
import com.intellij.xdebugger.XDebuggerManager
import com.intellij.xdebugger.breakpoints.XBreakpointProperties
import com.intellij.xdebugger.breakpoints.XLineBreakpoint
import com.jetbrains.rd.util.string.printToString
import com.jetbrains.rdclient.util.idea.pumpMessages
import com.jetbrains.rider.debugger.DotNetStackFrame
import com.jetbrains.rider.debugger.breakpoint.DotNetLineBreakpointProperties
import com.jetbrains.rider.plugins.unity.debugger.breakpoints.UnityPausepointBreakpointType
import com.jetbrains.rider.plugins.unity.debugger.breakpoints.convertToLineBreakpoint
import com.jetbrains.rider.plugins.unity.debugger.valueEvaluators.UnityTextureCustomComponentEvaluator
import com.jetbrains.rider.plugins.unity.model.debuggerWorker.UnityTextureInfo
import com.jetbrains.rider.test.annotations.*
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.scriptingApi.*
import com.jetbrains.rider.test.unity.EngineVersion
import com.jetbrains.rider.test.unity.Tuanjie
import com.jetbrains.rider.test.unity.Unity
import com.jetbrains.rider.unity.test.framework.api.*
import com.jetbrains.rider.unity.test.framework.base.IntegrationTestWithUnityProjectBase
import kotlinx.coroutines.launch
import org.testng.annotations.AfterMethod
import org.testng.annotations.Test
import kotlin.test.assertNotNull
import kotlin.test.fail

@Subsystem(SubsystemConstants.UNITY_DEBUG)
@Feature("Debug Unity Editor")
@Severity(SeverityLevel.CRITICAL)
@Solution("UnityDebugAndUnitTesting/Project")
abstract class DebuggerTest(engineVersion: EngineVersion) : IntegrationTestWithUnityProjectBase(engineVersion) {
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
    fun checkTextureDebugging() {
        attachDebuggerToUnityEditorAndPlay(
            beforeRun = {
                toggleBreakpoint("TextureDebuggingScript.cs", 13) //  Debug.Log(texture2D);
            },
            test = {
                waitForPause()
                dumpFullCurrentData()
                try {
                    val stackFrame = (session.currentStackFrame as DotNetStackFrame)

                    assertNotNull(stackFrame)
                    val value = stackFrame.getNamedValue("texture2D")
                    assertNotNull(value)
                    val texture2DPresentation = value.getPresentation()
                    val unityTextureCustomComponentEvaluator = texture2DPresentation.myFullValueEvaluator as UnityTextureCustomComponentEvaluator

                    assertNotNull(unityTextureCustomComponentEvaluator)


                    val lifetime = this.project.lifetime
                    var textureInfo: UnityTextureInfo? = null
                    val job = (project as ComponentManagerEx).getCoroutineScope().launch {
                        textureInfo = UnityTextureCustomComponentEvaluator.getUnityTextureInfo(stackFrame, value.objectProxy.id, lifetime,
                                                                                               10000, null) {
                            fail(it)
                        }
                    }

                    pumpMessages(DebugTestExecutionContext.waitForStopTimeout) {
                        job.isCompleted
                    }
                    assertNotNull(textureInfo)
                    printlnIndented(textureInfo.printToString())
                }
                finally {
                }
            }, goldFile = testGoldFile)
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

    @AfterMethod
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

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
class DebuggerTestUnity2020 : DebuggerTest(Unity.V2020)  {
    init {
        addMute(Mute("RIDER-105466", platforms = arrayOf(PlatformType.WINDOWS_ALL)), ::checkUnityPausePoint)
    }
}

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
class DebuggerTestUnity2022 : DebuggerTest(Unity.V2022)

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
class DebuggerTestUnity2023 : DebuggerTest(Unity.V2023) {
    init {
      addMute(Mute("RIDER-105466"), ::checkUnityPausePoint)
    }
}

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
class DebuggerTestUnity6 : DebuggerTest(Unity.V6) {
    init {
        addMute(Mute("RIDER-105466"), ::checkUnityPausePoint)
    }
}

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@Mute("RIDER-113191")
@Solution("TuanjieDebugAndUnitTesting/Project")
class DebuggerTestTuanjie2022 : DebuggerTest (Tuanjie.V2022)