package com.jetbrains.rider.unity.test.cases.integrationTests

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
import com.jetbrains.rider.test.enums.UnityBackend
import com.jetbrains.rider.test.enums.UnityVersion
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.scriptingApi.dumpFullCurrentData
import com.jetbrains.rider.test.scriptingApi.resumeSession
import com.jetbrains.rider.test.scriptingApi.toggleBreakpoint
import com.jetbrains.rider.test.scriptingApi.waitForPause
import com.jetbrains.rider.unity.test.framework.api.runUnityPlayerAndAttachDebugger
import org.testng.annotations.Test
import java.time.Duration
import java.util.concurrent.TimeUnit
import kotlin.test.assertNotNull

@Subsystem(SubsystemConstants.UNITY_DEBUG)
@Feature("Debug Unity Player")
@Severity(SeverityLevel.CRITICAL)
@Solution("UnityPlayerProjects/SimpleUnityGame")
@RiderTestTimeout(5, TimeUnit.MINUTES)
abstract class UnityPlayerDebuggerTestBase() : UnityPlayerTestBase(){

    @Test(description = "Check breakpoint for prebuilt Player)")
    @ChecklistItems(["Debug prebuilt Unity Player"])
    fun checkBreakpoint() {
        val playerFile = getPlayerFile()
        assertNotNull(playerFile, "Player file was not found or does not exist")

        runUnityPlayerAndAttachDebugger(playerFile, {
            toggleBreakpoint(project, "UpdateBreakpointScript.cs", 8)
            /*
            This is a workaround due to an existing constraint in our Debugger:
            We cannot execute dumpFullCurrentData() immediately after we got FIRST pause event.
            Instead, we need to allow for the first break event to be triggered and then canceled.
            For extra details, check SoftRuntimeEventsManager located at:
            Debugger\Libs\Mono.Debugging.Soft\RuntimeEvents\SoftRuntimeEventsManager.cs:209
            ``` // fix RIDER-54774 (hack #1)```

            And Debugger\Libs\Mono.Debugging.Soft\RuntimeEvents\SoftRuntimeEventsManager.cs:166
            ``` // counter hack for hack #1 ```
            */
            waitForPause()
            resumeSession()

            // And now we can run dumpFullCurrentData()
            waitForPause()
            dumpFullCurrentData()
            toggleBreakpoint(project, "UpdateBreakpointScript.cs", 8)
            resumeSession()
        }, testGoldFile)
    }


    companion object {
        val collectTimeout: Duration = Duration.ofSeconds(60)
    }
}

@Subsystem(SubsystemConstants.UNITY_DEBUG)
@Severity(SeverityLevel.CRITICAL)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
class UnityPlayerDebuggerTest {
    @UnityTestSettings(unityVersion = UnityVersion.V2022, unityBackend = UnityBackend.Mono)
    class TestMonoUnityBuild2022 : UnityPlayerDebuggerTestBase(){
        init {
            addMute(Mute("RIDER-127915", platforms = arrayOf(PlatformType.MAC_OS_ALL)), ::checkBreakpoint)
        }
    }

    @UnityTestSettings(unityVersion = UnityVersion.V6, unityBackend = UnityBackend.Mono)
    class TestMonoUnityBuild6 : UnityPlayerDebuggerTestBase(){
        init {
            addMute(Mute("RIDER-127915", platforms = arrayOf(PlatformType.MAC_OS_ALL)), ::checkBreakpoint)
        }
    }

    @UnityTestSettings(unityVersion = UnityVersion.V6_2, unityBackend = UnityBackend.Mono)
    class TestMonoUnityBuild6_2 : UnityPlayerDebuggerTestBase() {
        init {
            addMute(Mute("RIDER-127915", platforms = arrayOf(PlatformType.MAC_OS_ALL)), ::checkBreakpoint)
        }
    }

    @UnityTestSettings(unityVersion = UnityVersion.V2022, unityBackend = UnityBackend.Il2CPP)
    class TestIL2CPPUnityBuild2022 : UnityPlayerDebuggerTestBase() {
        init {
            addMute(Mute("RIDER-127915", platforms = arrayOf(PlatformType.MAC_OS_ALL)), ::checkBreakpoint)
        }
    }

    @UnityTestSettings(unityVersion = UnityVersion.V6, unityBackend = UnityBackend.Il2CPP)
    class TestIL2CPPUnityBuild6 : UnityPlayerDebuggerTestBase() {
        init {
            addMute(Mute("RIDER-127915", platforms = arrayOf(PlatformType.MAC_OS_ALL)), ::checkBreakpoint)
        }
    }

    @UnityTestSettings(unityVersion = UnityVersion.V6_2, unityBackend = UnityBackend.Il2CPP)
    class TestIL2CPPUnityBuild6_2 : UnityPlayerDebuggerTestBase() {
        init {
            addMute(Mute("RIDER-127915", platforms = arrayOf(PlatformType.MAC_OS_ALL)), ::checkBreakpoint)
        }
    }
}
