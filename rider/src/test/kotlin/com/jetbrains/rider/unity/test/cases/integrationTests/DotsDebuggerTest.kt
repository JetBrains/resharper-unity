package com.jetbrains.rider.unity.test.cases.integrationTests

import com.intellij.xdebugger.XDebuggerManager
import com.intellij.xdebugger.breakpoints.XLineBreakpoint
import com.jetbrains.rd.platform.diagnostics.LogTraceScenario
import com.jetbrains.rider.debugger.breakpoint.DotNetLineBreakpointProperties
import com.jetbrains.rider.diagnostics.LogTraceScenarios
import com.jetbrains.rider.plugins.unity.debugger.breakpoints.UnityPausepointBreakpointType
import com.jetbrains.rider.plugins.unity.debugger.breakpoints.convertToLineBreakpoint
import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.RiderTestTimeout
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.annotations.UnityTestSettings
import com.jetbrains.rider.test.annotations.report.ChecklistItems
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.annotations.report.Severity
import com.jetbrains.rider.test.annotations.report.SeverityLevel
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.enums.UnityVersion
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.scriptingApi.DebugTestExecutionContext
import com.jetbrains.rider.test.scriptingApi.dumpFullCurrentData
import com.jetbrains.rider.test.scriptingApi.removeAllBreakpoints
import com.jetbrains.rider.test.scriptingApi.resumeSession
import com.jetbrains.rider.test.scriptingApi.toggleBreakpoint
import com.jetbrains.rider.test.scriptingApi.waitForPause
import com.jetbrains.rider.unity.test.framework.api.attachDebuggerToUnityEditorAndPlay
import com.jetbrains.rider.unity.test.framework.api.toggleUnityPausepoint
import com.jetbrains.rider.unity.test.framework.api.unpause
import com.jetbrains.rider.unity.test.framework.api.waitForUnityEditorPauseMode
import com.jetbrains.rider.unity.test.framework.api.waitForUnityEditorPlayMode
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
abstract class DotsDebuggerTest() : IntegrationTestWithUnityProjectBase() {
    override val traceScenarios: Set<LogTraceScenario>
        get() = super.traceScenarios + LogTraceScenarios.Debugger + LogTraceScenarios.MonoDebuggerConnection
    
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
@UnityTestSettings(unityVersion = UnityVersion.V2022)
class DotsDebuggerTestUnity2022 : DotsDebuggerTest() {
    init {
        addMute(Mute("RIDER-125876"), ::checkUnityPausePoint)
    }
}

@RiderTestTimeout(5, unit = TimeUnit.MINUTES)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@UnityTestSettings(unityVersion = UnityVersion.V6)
class DotsDebuggerTestUnity6 : DotsDebuggerTest() {
    init {
        addMute(Mute("RIDER-125876"), ::checkUnityPausePoint)
    }
}

@RiderTestTimeout(5, unit = TimeUnit.MINUTES)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@UnityTestSettings(unityVersion = UnityVersion.V6_2)
class DotsDebuggerTestUnity6_2 : DotsDebuggerTest() {
    init {
        addMute(Mute("RIDER-125876"), ::checkUnityPausePoint)
    }
}



