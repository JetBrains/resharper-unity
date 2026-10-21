package com.jetbrains.rider.unity.test.cases.integrationTests

import com.intellij.openapi.util.registry.RegistryManager
import com.jetbrains.rd.platform.diagnostics.LogTraceScenario
import com.jetbrains.rider.diagnostics.LogTraceScenarios
import com.jetbrains.rider.unity.test.framework.api.removeAllUnityPausepoints
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
import com.jetbrains.rider.test.shared.constants.TeamCityTags
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
import org.junit.jupiter.api.AfterEach
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test
import java.util.concurrent.TimeUnit

@Subsystem(SubsystemConstants.UNITY_DEBUG)
@Feature("Debug Unity Dots")
@Severity(SeverityLevel.CRITICAL)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@Solution("UnityDotsDebug/Project")
@RiderTestTimeout(5, unit = TimeUnit.MINUTES)
@Tag(TeamCityTags.Plugins.Unity.Integration)
abstract class DotsDebuggerTest() : IntegrationTestWithUnityProjectBase() {
    override val traceScenarios: Set<LogTraceScenario>
        get() = super.traceScenarios + LogTraceScenarios.Debugger + LogTraceScenarios.MonoDebuggerConnection

    /** The breakpoint line in `ResetTransformSystemBase`: Unity 6.4+ uses the `SystemAPI.Query` variant at line 44. */
    private val systemBaseBreakpointLine: Int
        get() {
            val version = engineVersion.version.split('.').map { it.toInt() }
            val hasEntitiesForEach = !engineVersion.isUnity() || version[0] < 6000 || version.getOrElse(1) { 0 } < 4
            return if (hasEntitiesForEach) 24 else 44
        }
    
    @Test // Check breakpoint for Unity DOTS code
    @ChecklistItems(["Breakpoints/Breakpoint in DOTS"])
    fun checkBreakpointInDOTSCode() {
        attachDebuggerToUnityEditorAndPlay(
            {
                RegistryManager.getInstance().get("rider.debugger.softdebugger.enable.burst.compatibility").setValue(true)
                toggleBreakpoint("ResetTransformSystem.cs", systemBaseBreakpointLine) //set new breakpoint
            },
            {
                setCustomRegextToMask()

                waitForPause()
                dumpFullCurrentData()
                toggleBreakpoint("ResetTransformSystem.cs", systemBaseBreakpointLine) //disable breakpoint
                resumeSession()

                toggleBreakpoint("ResetTransformSystem.cs", 34)//set new breakpoint
                waitForPause()
                dumpFullCurrentData()
                resumeSession()

            }, testGoldFile)
    }

    @Test // Check Ref Presentation in DOTS code for simple app
    @ChecklistItems(["Breakpoints/Ref Presentation in DOTS"])
    fun checkRefPresentationInDOTSCode() {
        attachDebuggerToUnityEditorAndPlay(
            {
                RegistryManager.getInstance().get("rider.debugger.softdebugger.enable.burst.compatibility").setValue(true)
                toggleBreakpoint("ResetTransformSystem.cs", systemBaseBreakpointLine) //set new breakpoint
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
        dumpProfile.customRegexToMask["<scene_handle>"] = Regex("(?<=\\{SceneHandle} )\\d+")
    }

    @Test // Check Unity pause point in debugging for Unity DOTS
    @ChecklistItems(["Breakpoints/Unity Pause Points in DOTS"])
    fun checkUnityPausePoint() {
        attachDebuggerToUnityEditorAndPlay(
            test = {
                waitForUnityEditorPlayMode()
                toggleUnityPausepoint(project, "ResetTransformSystem.cs", systemBaseBreakpointLine)
                waitForUnityEditorPauseMode()
                removeAllUnityPausepoints()
                unpause()
            })
    }

    @AfterEach
    fun clearAllBreakpoints() {
        removeAllBreakpoints()
    }

    //TODO solution build throws error on code generation phase
    override fun buildSolutionAfterUnityStarts() {
    }

    //TODO checkSwea hangs for unknown reason
    override fun checkSwea() {
    }
}

@RiderTestTimeout(5, unit = TimeUnit.MINUTES)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@UnityTestSettings(unityVersion = UnityVersion.V2022)
class DotsDebuggerTestUnity2022 : DotsDebuggerTest()

@RiderTestTimeout(5, unit = TimeUnit.MINUTES)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@UnityTestSettings(unityVersion = UnityVersion.V6_3)
class DotsDebuggerTestUnity6_3 : DotsDebuggerTest()

@RiderTestTimeout(5, unit = TimeUnit.MINUTES)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@UnityTestSettings(unityVersion = UnityVersion.V6_5)
class DotsDebuggerTestUnity6_5 : DotsDebuggerTest()
