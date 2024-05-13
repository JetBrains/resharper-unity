package com.jetbrains.rider.unity.test.cases.integrationTests

import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.annotations.Severity
import com.jetbrains.rider.test.annotations.SeverityLevel
import com.jetbrains.rider.test.annotations.Feature
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.unity.test.framework.UnityVersion

@Subsystem(SubsystemConstants.UNITY_UNIT_TESTING)
@Feature("Unit Testing in Unity solution with started Unity2022")
@Severity(SeverityLevel.CRITICAL)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@Suppress("unused")
class UnitTestingTests {
    class TestUnity2020 : UnitTestingTestBase(UnityVersion.V2020)
    class TestUnity2022 : UnitTestingTestBase(UnityVersion.V2022)
    class TestUnity2023 : UnitTestingTestBase(UnityVersion.V2023)
}