package com.jetbrains.rider.unity.test.cases.integrationTests

import com.intellij.openapi.rd.util.lifetime
import com.intellij.xdebugger.XDebuggerManager
import com.intellij.xdebugger.breakpoints.XLineBreakpoint
import com.jetbrains.rd.util.string.printToString
import com.jetbrains.rdclient.util.idea.pumpMessages
import com.jetbrains.rider.debugger.DotNetStackFrame
import com.jetbrains.rider.debugger.breakpoint.DotNetLineBreakpointProperties
import com.jetbrains.rider.plugins.unity.debugger.breakpoints.UnityPausepointBreakpointType
import com.jetbrains.rider.plugins.unity.debugger.breakpoints.convertToLineBreakpoint
import com.jetbrains.rider.plugins.unity.debugger.valueEvaluators.UnityTextureCustomComponentEvaluator
import com.jetbrains.rider.plugins.unity.debugger.valueEvaluators.UnityTextureCustomComponentEvaluator.Companion.getUnityTextureInfo
import com.jetbrains.rider.plugins.unity.model.debuggerWorker.UnityTextureInfo
import com.jetbrains.rider.test.allure.Subsystem
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.scriptingApi.*
import com.jetbrains.rider.unity.test.framework.UnityVersion
import com.jetbrains.rider.unity.test.framework.api.attachDebuggerToUnityEditorAndPlay
import com.jetbrains.rider.unity.test.framework.base.IntegrationTestWithUnityProjectBase
import io.qameta.allure.*
import kotlinx.coroutines.launch
import org.testng.annotations.AfterMethod
import org.testng.annotations.Test
import java.io.File
import kotlin.test.assertNotNull
import kotlin.test.fail

@Epic(Subsystem.UNITY_DEBUG)
@Feature("Debug Unity2022")
@Severity(SeverityLevel.CRITICAL)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
class DebuggerTest2022 : IntegrationTestWithUnityProjectBase() {
    override fun getSolutionDirectoryName() = "UnityDebugAndUnitTesting/Project"
    override val unityMajorVersion = UnityVersion.V2022

    override fun preprocessTempDirectory(tempDir: File) {
        super.preprocessTempDirectory(tempDir)

        val newBehaviourScript = "NewBehaviourScript.cs"
        val sourceScript = testCaseSourceDirectory.resolve(newBehaviourScript)
        if (sourceScript.exists()) {
            sourceScript.copyTo(tempDir.resolve("Assets").resolve(newBehaviourScript), true)
        }
    }

    @Test
    @Description("Check breakpoints with Unity2022")
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
    @Description("Check Texture debugging with Unity2022")
    fun checkTextureDebugging() {
        attachDebuggerToUnityEditorAndPlay(
            {
                toggleBreakpoint("TextureDebuggingScript.cs", 13)
            },
            {
                waitForPause()
                dumpFullCurrentData()
                try {
                    val stackFrame = (session.currentStackFrame  as DotNetStackFrame)

                    assertNotNull(stackFrame)
                    val value = stackFrame.getNamedValue("texture2D")
                    assertNotNull(value)
                    val texture2DPresentation = value.getPresentation()
                    val unityTextureCustomComponentEvaluator = texture2DPresentation.myFullValueEvaluator as UnityTextureCustomComponentEvaluator

                    assertNotNull(unityTextureCustomComponentEvaluator)


                    val lifetime = this.project.lifetime
                    var textureInfo: UnityTextureInfo? = null
                    val job = project.coroutineScope.launch {
                        textureInfo = getUnityTextureInfo(stackFrame, value.objectProxy.id, lifetime, 10000, null) {
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