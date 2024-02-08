package com.jetbrains.rider.unity.test.cases.integrationTests


import com.intellij.openapi.util.SystemInfo
import com.jetbrains.rdclient.util.idea.toIOFile
import com.jetbrains.rider.test.allure.Subsystem
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.framework.combine
import com.jetbrains.rider.test.scriptingApi.*
import com.jetbrains.rider.unity.test.framework.UnityVersion
import com.jetbrains.rider.unity.test.framework.api.*
import io.qameta.allure.Epic
import io.qameta.allure.Severity
import io.qameta.allure.SeverityLevel
import org.testng.annotations.Test
import java.time.Duration
import kotlin.test.assertNotNull


abstract class UnityPlayerDebuggerTestBase(unityVersion: UnityVersion, private val buildNames: Map<String, String>)
    : UnityPlayerTestBase(unityVersion, buildNames) {

    override fun getSolutionDirectoryName() = "UnityPlayerDebug/Project"

    private fun getExecutionFileName() = if (SystemInfo.isMac)
        "Project.app"
    else if (SystemInfo.isWindows)
        "Project.exe"
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
            waitForPause()
            dumpFullCurrentData()
            resumeSession()
        }, testGoldFile)
    }


    companion object {
        val collectTimeout: Duration = Duration.ofSeconds(60)
        const val macOS = "osx"
        const val winOS = "win"
    }
}

@Epic(Subsystem.UNITY_DEBUG)
@Severity(SeverityLevel.CRITICAL)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
class UnityPlayerDebuggerTest {
    class TestUnity2020 : UnityPlayerDebuggerTestBase(UnityVersion.V2020, mapOf(
        winOS to "UnityPlayerDebuggerTest_StandaloneWindows64_2022.3.17f1_24-Feb-09.zip",
        macOS to "UnityPlayerDebuggerTest_StandaloneOSX_2022.3.17f1_24-Feb-09.zip"))
}

