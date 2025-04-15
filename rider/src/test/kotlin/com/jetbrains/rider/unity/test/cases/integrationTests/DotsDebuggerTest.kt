package com.jetbrains.rider.unity.test.cases.integrationTests

import com.intellij.xdebugger.XDebuggerManager
import com.intellij.xdebugger.breakpoints.XLineBreakpoint
import com.jetbrains.rider.debugger.breakpoint.DotNetLineBreakpointProperties
import com.jetbrains.rider.plugins.unity.debugger.breakpoints.UnityPausepointBreakpointType
import com.jetbrains.rider.plugins.unity.debugger.breakpoints.convertToLineBreakpoint
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.annotations.*
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.scriptingApi.*
import com.jetbrains.rider.test.unity.EngineVersion
import com.jetbrains.rider.test.unity.Unity
import com.jetbrains.rider.unity.test.framework.api.*
import com.jetbrains.rider.unity.test.framework.base.IntegrationTestWithUnityProjectBase
import org.testng.annotations.AfterMethod
import org.testng.annotations.BeforeMethod
import org.testng.annotations.Test
import java.util.concurrent.TimeUnit

@Subsystem(SubsystemConstants.UNITY_DEBUG)
@Feature("Debug Unity Dots")
@Severity(SeverityLevel.CRITICAL)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@Solution("UnityDotsDebug/Project")
@RiderTestTimeout(5, unit = TimeUnit.MINUTES)
abstract class DotsDebuggerTest(override val engineVersion: EngineVersion) : IntegrationTestWithUnityProjectBase(engineVersion) {
    @Test(description = "Check breakpoint for Unity DOTS code")
    @ChecklistItems(["Breakpoints/Breakpoint in DOTS"])
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

    @Mute("RIDER-118536", platforms = [PlatformType.MAC_OS_ALL])
    @Test(description = "Check Ref Presentation in DOTS code for simple app")
    @ChecklistItems(["Breakpoints/Ref Presentation in DOTS"])
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

    @Test(description = "Check Unity pause point in debugging for Unity DOTS")
    @ChecklistItems(["Breakpoints/Unity Pause Points in DOTS"])
    fun checkUnityPausePoint() {
        attachDebuggerToUnityEditorAndPlay(
            test = {
                waitForUnityEditorPlayMode()
                toggleUnityPausepoint(project, "ResetTransformSystem.cs", 24)
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

@RiderTestTimeout(5, unit = TimeUnit.MINUTES)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
class DotsDebuggerestUnity2020 : DotsDebuggerTest(Unity.V2020) {
    init {
        addMute(Mute("RIDER-105466"), ::checkUnityPausePoint)
        addMute(Mute("RIDER-105466"), ::removeAllUnityPausepoints)
        addMute(Mute("RIDER-105466"), ::setUpTestCaseSolution)
    }
}

@RiderTestTimeout(5, unit = TimeUnit.MINUTES)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
class DotsDebuggerTestUnity2022 : DotsDebuggerTest(Unity.V2022) {
    init {
        addMute(Mute("RIDER-105466"), ::checkUnityPausePoint)
        addMute(Mute("RIDER-118536", platforms = arrayOf(PlatformType.MAC_OS_ALL)), ::checkRefPresentationInDOTSCode)
        addMute(Mute("RIDER-119059"), ::checkBreakpointInDOTSCode)
    }
}

@RiderTestTimeout(5, unit = TimeUnit.MINUTES)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
class DotsDebuggerTestUnity2023 : DotsDebuggerTest(Unity.V2023) {
    init {
        addMute(Mute("RIDER-105466"), ::checkUnityPausePoint)
        addMute(Mute("RIDER-119059"), ::checkRefPresentationInDOTSCode)
        addMute(Mute("RIDER-119059"), ::checkBreakpointInDOTSCode)
    }
}

@RiderTestTimeout(5, unit = TimeUnit.MINUTES)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
class DotsDebuggerTestUnity6 : DotsDebuggerTest(Unity.V6) {
    init {
        addMute(Mute("RIDER-105466"), ::checkUnityPausePoint)
        addMute(Mute("RIDER-71776", platforms = arrayOf(PlatformType.MAC_OS_ALL)), ::checkRefPresentationInDOTSCode)
        addMute(Mute("RIDER-119059"), ::checkRefPresentationInDOTSCode)
        addMute(Mute("RIDER-118536", platforms = arrayOf(PlatformType.MAC_OS_ALL)), ::checkBreakpointInDOTSCode)
        addMute(Mute("RIDER-119059", platforms = arrayOf(PlatformType.WINDOWS_ALL)), ::checkBreakpointInDOTSCode)
    }
}


