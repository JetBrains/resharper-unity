package com.jetbrains.rider.unity.test.cases.integrationTests

import com.jetbrains.rider.test.allure.Subsystem
import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.unity.test.framework.base.DebuggerTestBase
import io.qameta.allure.Epic
import io.qameta.allure.Feature
import io.qameta.allure.Severity
import io.qameta.allure.SeverityLevel
import java.io.File

@Mute("RIDER-67296")
@Epic(Subsystem.UNITY_DEBUG)
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