package com.jetbrains.rider.unity.test.cases.integrationTests

import com.jetbrains.rider.test.allure.Subsystem
import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.unity.test.framework.UnityVersion
import io.qameta.allure.Epic
import io.qameta.allure.Feature
import io.qameta.allure.Severity
import io.qameta.allure.SeverityLevel

@Epic(Subsystem.UNITY_PLUGIN)
@Feature("PlayMode Action for Unity")
@Severity(SeverityLevel.CRITICAL)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
class PlayModeTest {
    class TestUnity2020 : PlayModeTestBase(UnityVersion.V2020) {}
    class TestUnity2022 : PlayModeTestBase(UnityVersion.V2022) {}
    class TestUnity2023 : PlayModeTestBase(UnityVersion.V2023) {
        init {
          addMute(Mute("RIDER-105666"), ::checkPlayModeLogs)
        }
    }
}