package com.jetbrains.rider.unity.test.cases.integrationTests

import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.annotations.Feature
import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.Severity
import com.jetbrains.rider.test.annotations.SeverityLevel
import com.jetbrains.rider.test.unity.Unity
import java.io.File

@Mute("RIDER-67296")
@Subsystem(SubsystemConstants.UNITY_DEBUG)
@Feature("Debug Unity OldMono")
@Severity(SeverityLevel.NORMAL)
class DebuggerTestOldMono : DebuggerTest(Unity.V2022) {
    override val testSolution = "SimpleUnityProjectWithoutPlugin"
    override fun preprocessTempDirectory(tempDir: File) {
        super.preprocessTempDirectory(tempDir)

        // enable old mono
        val projectSettings = tempDir.resolve("ProjectSettings/ProjectSettings.asset")
        projectSettings.writeText(projectSettings.readText().replace("scriptingRuntimeVersion: 1","scriptingRuntimeVersion: 0"))
    }

}