package com.jetbrains.rider.unity.test.cases.integrationTests

import com.jetbrains.rider.test.annotations.*
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.unity.test.framework.Tuanjie
import com.jetbrains.rider.unity.test.framework.Unity

@Subsystem(SubsystemConstants.UNITY_UNIT_TESTING)
@Feature("Unit Testing in Unity solution with started Unity2022")
@Severity(SeverityLevel.CRITICAL)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@Suppress("unused")
class UnitTestingTests {
    class TestUnity2020 : UnitTestingTestBase(Unity.V2020)
    class TestUnity2022 : UnitTestingTestBase(Unity.V2022)
    class TestUnity2023 : UnitTestingTestBase(Unity.V2023)
    @Mute("RIDER-113191")
    class TestUnity6 : UnitTestingTestBase(Unity.V6)
    @Mute("RIDER-113191")
    class TestTuanjie2022 : UnitTestingTestBase(Tuanjie.V2022)
}