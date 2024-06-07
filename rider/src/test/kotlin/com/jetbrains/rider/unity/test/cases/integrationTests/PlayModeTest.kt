package com.jetbrains.rider.unity.test.cases.integrationTests

import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.unity.test.framework.EngineVersion
import com.jetbrains.rider.test.annotations.*
import com.jetbrains.rider.unity.test.framework.Tuanjie
import com.jetbrains.rider.unity.test.framework.Unity

@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("PlayMode Action for Unity")
@Severity(SeverityLevel.CRITICAL)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@Suppress("unused")
class PlayModeTest {
    class TestUnity2020 : PlayModeTestBase(Unity.V2020)
    class TestUnity2022 : PlayModeTestBase(Unity.V2022) {
        init {
            addMute(Mute("RIDER-105666"), ::checkPlayModeLogs)
        }
    }

    class TestUnity2023 : PlayModeTestBase(Unity.V2023) {
        init {
          addMute(Mute("RIDER-105666"), ::checkPlayModeLogs)
        }
    }
    class TestUnity6 : PlayModeTestBase(Unity.V6) {
    }
    class TestTuanji2022 : PlayModeTestBase(Tuanjie.V2022) {
    }
}