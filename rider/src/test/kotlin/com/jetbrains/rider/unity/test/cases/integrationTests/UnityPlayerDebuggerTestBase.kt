package com.jetbrains.rider.unity.test.cases.integrationTests

import com.jetbrains.rider.test.annotations.*
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.scriptingApi.*
import com.jetbrains.rider.test.unity.EngineVersion
import com.jetbrains.rider.test.unity.Unity
import com.jetbrains.rider.unity.test.framework.api.*
import org.testng.annotations.Test
import java.time.Duration
import java.util.concurrent.TimeUnit
import kotlin.test.assertNotNull

@Subsystem(SubsystemConstants.UNITY_DEBUG)
@Feature("Debug Unity Player")
@Severity(SeverityLevel.CRITICAL)
@Solution("UnityPlayerProjects/SimpleUnityGame")
@RiderTestTimeout(5, TimeUnit.MINUTES)
abstract class UnityPlayerDebuggerTestBase(engineVersion: EngineVersion)
    : UnityPlayerTestBase(engineVersion) {

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
    class TestUnityBuild2022 : UnityPlayerDebuggerTestBase(Unity.V2022){
        init {
            addMute(Mute("RIDER-123706", platforms = [PlatformType.MAC_OS_ALL]), ::checkBreakpoint)
        }
    }
    }
