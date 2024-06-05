package com.jetbrains.rider.unity.test.cases.integrationTests

import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.unity.test.framework.EngineVersion
import com.jetbrains.rider.test.annotations.*

@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("PlayMode Action for Unity")
@Severity(SeverityLevel.CRITICAL)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@Suppress("unused")
class PlayModeTest {
    class TestUnity2020 : PlayModeTestBase(EngineVersion.Unity2020)
    class TestUnity2022 : PlayModeTestBase(EngineVersion.Unity2022) {
        init {
            addMute(Mute("RIDER-105666"), ::checkPlayModeLogs)
        }
    }

    class TestUnity2023 : PlayModeTestBase(EngineVersion.Unity2023) {
        init {
          addMute(Mute("RIDER-105666"), ::checkPlayModeLogs)
        }
    }
    class TestUnity6 : PlayModeTestBase(EngineVersion.Unity6) {
       /* init {
            addMute(Mute("RIDER-105666"), ::checkPlayModeLogs)
        }*/
    }
    class TestTuanji2022 : PlayModeTestBase(EngineVersion.Tuanjie2022) {
        /* init {
             addMute(Mute("RIDER-105666"), ::checkPlayModeLogs)
         }*/
    }
}