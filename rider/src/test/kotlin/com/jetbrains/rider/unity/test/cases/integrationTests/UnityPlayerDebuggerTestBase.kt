package com.jetbrains.rider.unity.test.cases.integrationTests


import com.intellij.openapi.util.SystemInfo
import com.jetbrains.rdclient.util.idea.toIOFile
import com.jetbrains.rider.test.allure.SubsystemConstants
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.framework.combine
import com.jetbrains.rider.test.scriptingApi.*
import com.jetbrains.rider.unity.test.framework.UnityVersion
import com.jetbrains.rider.unity.test.framework.api.*
import io.qameta.allure.Severity
import io.qameta.allure.SeverityLevel
import org.testng.annotations.Test
import java.time.Duration
import kotlin.test.assertNotNull


abstract class UnityPlayerDebuggerTestBase(unityVersion: UnityVersion, buildNames: Map<String, String>)
    : UnityPlayerTestBase(unityVersion, buildNames) {

    override fun getSolutionDirectoryName() = "UnityPlayerProjects/SimpleUnityGame"

    private fun getExecutionFileName() = if (SystemInfo.isMac)
        "SimpleUnityGame.app"
    else if (SystemInfo.isWindows)
        "SimpleUnityGame.exe"
    else null

    @Test
    fun checkBreakpoint() {
        val gameFileName = getGameFileName()
        assertNotNull(gameFileName)

        val exeName = getExecutionFileName()
        assertNotNull(exeName)

        val gameFullPath = activeSolutionDirectory.combine(gameFileName.toIOFile().nameWithoutExtension).combine(exeName)

        runUnityPlayerAndAttachDebugger(gameFullPath, {
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
        const val macOS = "osx"
        const val winOS = "win"
    }
}

@Subsystem(SubsystemConstants.UNITY_DEBUG)
@Severity(SeverityLevel.CRITICAL)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
class UnityPlayerDebuggerTest {
    class TestUnityBuild2022 : UnityPlayerDebuggerTestBase(UnityVersion.V2020, mapOf(
        winOS to "UnityPlayerDebuggerTest_StandaloneWindows64_2022.3.17f1_2024-Feb-20.zip",
        macOS to "UnityPlayerDebuggerTest_StandaloneOSX_2022.3.17f1_2024-Feb-20.zip"))
}

