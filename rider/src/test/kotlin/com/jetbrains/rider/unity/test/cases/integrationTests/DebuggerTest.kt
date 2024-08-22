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
import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.framework.combine
import com.jetbrains.rider.test.scriptingApi.*
import com.jetbrains.rider.unity.test.framework.EngineVersion
import com.jetbrains.rider.unity.test.framework.Tuanjie
import com.jetbrains.rider.unity.test.framework.Unity
import com.jetbrains.rider.unity.test.framework.api.*
import com.jetbrains.rider.unity.test.framework.base.IntegrationTestWithUnityProjectBase
import kotlinx.coroutines.launch
import org.testng.annotations.AfterMethod
import org.testng.annotations.Test
import java.io.File
import kotlin.test.assertNotNull
import kotlin.test.fail

abstract class DebuggerTest(engineVersion: EngineVersion) : IntegrationTestWithUnityProjectBase(engineVersion) {

    override val testSolution: String = 
        if (engineVersion.isTuanjie())
            "TuanjieDebugAndUnitTesting/Project"
        else 
            "UnityDebugAndUnitTesting/Project"

    override val testClassDataDirectory: File
        get() = super.testClassDataDirectory.parentFile.combine(DebuggerTest::class.simpleName!!)
    override val testCaseSourceDirectory: File
        get() = testClassDataDirectory.combine(super.testStorage.testMethod.name).combine("source")

    override fun preprocessTempDirectory(tempDir: File) {
        super.preprocessTempDirectory(tempDir)

        val newBehaviourScript = "NewBehaviourScript.cs"
        val sourceScript = testCaseSourceDirectory.resolve(newBehaviourScript)
        if (sourceScript.exists()) {
            sourceScript.copyTo(tempDir.resolve("Assets").resolve(newBehaviourScript), true)
        }
    }

    @Test
    fun checkBreakpoint() {
        attachDebuggerToUnityEditorAndPlay(
            {
                toggleBreakpoint("NewBehaviourScript.cs", 8)
                toggleBreakpoint("NewBehaviourScript.cs", 15)
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

    @Test
    fun checkTextureDebugging() {
        attachDebuggerToUnityEditorAndPlay(
            beforeRun = {
                toggleBreakpoint("TextureDebuggingScript.cs", 13)
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

    @Test
    fun checkUnityPausePoint() {
        attachDebuggerToUnityEditorAndPlay(
            test = {
                waitForUnityEditorPlayMode()
                toggleUnityPausepoint(project, "NewBehaviourScript.cs", 14)
                waitForUnityEditorPauseMode()
                removeAllUnityPausepoints()
                unpause()
            })
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
                breakpoint = toggleBreakpoint(project, "NewBehaviourScript.cs", 8)
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

class DebuggerTestUnity2020 : DebuggerTest(Unity.V2020)  {
    init {
        addMute(Mute("RIDER-105466", platforms = arrayOf(PlatformType.WINDOWS_ALL)), ::checkUnityPausePoint)
    }
}
class DebuggerTestUnity2022 : DebuggerTest(Unity.V2022)
class DebuggerTestUnity2023 : DebuggerTest(Unity.V2023)
class DebuggerTestUnity6 : DebuggerTest(Unity.V6)
@Mute("RIDER-113191")
class TestTuanjie2022 : DebuggerTest (Tuanjie.V2022)