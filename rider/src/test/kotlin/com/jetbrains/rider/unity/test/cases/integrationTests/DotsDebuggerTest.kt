package com.jetbrains.rider.unity.test.cases.integrationTests

import com.intellij.xdebugger.XDebuggerManager
import com.intellij.xdebugger.breakpoints.XLineBreakpoint
import com.jetbrains.rider.debugger.breakpoint.DotNetLineBreakpointProperties
import com.jetbrains.rider.plugins.unity.debugger.breakpoints.UnityPausepointBreakpointType
import com.jetbrains.rider.plugins.unity.debugger.breakpoints.convertToLineBreakpoint
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.annotations.*
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.framework.combine
import com.jetbrains.rider.test.scriptingApi.*
import com.jetbrains.rider.unity.test.framework.EngineVersion
import com.jetbrains.rider.unity.test.framework.Unity
import com.jetbrains.rider.unity.test.framework.api.*
import com.jetbrains.rider.unity.test.framework.base.IntegrationTestWithUnityProjectBase
import org.testng.annotations.AfterMethod
import org.testng.annotations.BeforeMethod
import org.testng.annotations.Test
import java.io.File

@Subsystem(SubsystemConstants.UNITY_DEBUG)
@Feature("Debug Unity Dots")
@Severity(SeverityLevel.CRITICAL)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
abstract class DotsDebuggerTest(override val engineVersion: EngineVersion) : IntegrationTestWithUnityProjectBase(engineVersion) {

    override val testSolution = "UnityDotsDebug/Project"

    override val testClassDataDirectory: File
        get() = super.testClassDataDirectory.parentFile.combine(DotsDebuggerTest::class.simpleName!!)
    override val testCaseSourceDirectory: File
        get() = testClassDataDirectory.combine(super.testProcessor.testMethod.name).combine("source")

    @Test
    fun checkBreakpointInDOTSCode() {
        attachDebuggerToUnityEditorAndPlay(
            {
                toggleBreakpoint("ResetTransformSystem.cs", 24) //set new breakpoint
            },
            {
                setCustomRegextToMask()

                waitForPause()
                dumpFullCurrentData()
                toggleBreakpoint("ResetTransformSystem.cs", 24) //disable breakpoint
                resumeSession()

                toggleBreakpoint("ResetTransformSystem.cs", 34)//set new breakpoint
                waitForPause()
                dumpFullCurrentData()
                resumeSession()

            }, testGoldFile)
    }

    @Test
    fun checkRefPresentationInDOTSCode() {
        attachDebuggerToUnityEditorAndPlay(
            {
                toggleBreakpoint("ResetTransformSystem.cs", 24) //set new breakpoint
            },
            {
                setCustomRegextToMask()

                waitForPause()
                dumpFullCurrentData(1)
                resumeSession()
            }, testGoldFile)
    }

    private fun DebugTestExecutionContext.setCustomRegextToMask() {
        dumpProfile.customRegexToMask["<id>"] = Regex("\\((\\d+:\\d+)\\)")
        dumpProfile.customRegexToMask["<float_value>"] = Regex("-?\\d+\\.*\\d*f")
        dumpProfile.customRegexToMask["<ResetTransformSystemBase_LambdaJob_Job>"] = Regex("ResetTransformSystemBase_.*_Job")
    }

    @Test
    fun checkUnityPausePoint() {
        attachDebuggerToUnityEditorAndPlay(
            test = {
                waitForUnityEditorPlayMode()
                toggleUnityPausepoint(project, "ResetTransformSystem.cs", 26)
                waitForUnityEditorPauseMode()
                removeAllUnityPausepoints()
                unpause()
            })
    }

    @AfterMethod
    fun removeAllUnityPausepoints() {
        XDebuggerManager.getInstance(project).breakpointManager.allBreakpoints.filter {
            it.type is UnityPausepointBreakpointType
        }.forEach {
            @Suppress("UNCHECKED_CAST")
            (convertToLineBreakpoint(project,
                                     it as XLineBreakpoint<DotNetLineBreakpointProperties>))
        }
        removeAllBreakpoints()
    }

    //TODO solution build throws error on code generation phase
    @BeforeMethod(dependsOnMethods = ["waitForUnityRunConfigurations"])
    override fun buildSolutionAfterUnityStarts() {
    }

    //TODO checkSwea hangs for unknown reason
    @AfterMethod
    override fun checkSwea() {
    }
}

class DotsDebuggerestUnity2020 : DotsDebuggerTest(Unity.V2020) {
    init {
        addMute(Mute("RIDER-105466"), ::checkUnityPausePoint)
        addMute(Mute("RIDER-105466"), ::removeAllUnityPausepoints)
        addMute(Mute("RIDER-105466"), ::setUpTestCaseSolution)
    }
}
class DotsDebuggerTestUnity2022 : DotsDebuggerTest(Unity.V2022) {
    init {
        addMute(Mute("RIDER-105466"), ::checkUnityPausePoint)
    }
}
class DotsDebuggerTestUnity2023 : DotsDebuggerTest(Unity.V2023) {
    init {
        addMute(Mute("RIDER-105466"), ::checkUnityPausePoint)
    }
}
class DotsDebuggerTestUnity6 : DotsDebuggerTest(Unity.V6) {
    init {
        addMute(Mute("RIDER-105466"), ::checkUnityPausePoint)
    }
}


