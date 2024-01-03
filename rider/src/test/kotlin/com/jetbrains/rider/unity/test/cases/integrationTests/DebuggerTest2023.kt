package com.jetbrains.rider.unity.test.cases.integrationTests

import com.jetbrains.rider.test.allure.Subsystem
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.unity.test.framework.UnityVersion
import io.qameta.allure.Epic
import io.qameta.allure.Feature
import io.qameta.allure.Severity
import io.qameta.allure.SeverityLevel

@Epic(Subsystem.UNITY_DEBUG)
@Feature("Debug Unity2023")
@Severity(SeverityLevel.CRITICAL)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
class DebuggerTest2023 : DebuggerTestBase() {
    override val unityMajorVersion = UnityVersion.V2023
}
