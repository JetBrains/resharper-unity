package com.jetbrains.rider.unity.test.cases.integrationTests

import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.annotations.Feature
import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.annotations.Severity
import com.jetbrains.rider.test.annotations.SeverityLevel
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.unity.test.framework.UnityVersion

@Subsystem(SubsystemConstants.UNITY_DEBUG)
@Feature("Debug Unity2020")
@Severity(SeverityLevel.CRITICAL)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@Suppress("unused")
class DebuggerTest {
    class TestUnity2020 : DebuggerTestBase(UnityVersion.V2020)  {
        init {
          addMute(Mute("RIDER-105466", platforms = arrayOf(PlatformType.WINDOWS_ALL)), ::checkUnityPausePoint)
        }
    }
    class TestUnity2022 : DebuggerTestBase(UnityVersion.V2022)
    class TestUnity2023 : DebuggerTestBase(UnityVersion.V2023)
}
