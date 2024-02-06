package com.jetbrains.rider.unity.test.cases.integrationTests

import com.jetbrains.rider.test.allure.SubsystemConstants
import com.jetbrains.rider.test.annotations.Feature
import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.unity.test.framework.base.DebuggerTestBase
import io.qameta.allure.Severity
import io.qameta.allure.SeverityLevel
import java.io.File

@Mute("RIDER-67296")
@Subsystem(SubsystemConstants.UNITY_DEBUG)
@Feature("Debug Unity OldMono")
@Severity(SeverityLevel.NORMAL)
class DebuggerTestOldMono : DebuggerTestBase() {
    override fun preprocessTempDirectory(tempDir: File) {
        super.preprocessTempDirectory(tempDir)

        // enable old mono
        val projectSettings = tempDir.resolve("ProjectSettings/ProjectSettings.asset")
        projectSettings.writeText(projectSettings.readText().replace("scriptingRuntimeVersion: 1","scriptingRuntimeVersion: 0"))
    }
}