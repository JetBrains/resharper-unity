package com.jetbrains.rider.unity.test.cases.integrationTests

import com.jetbrains.rider.test.annotations.Feature
import com.jetbrains.rider.test.annotations.Severity
import com.jetbrains.rider.test.annotations.SeverityLevel
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.scriptingApi.buildSolutionWithReSharperBuild
import com.jetbrains.rider.test.scriptingApi.withDcFacade
import com.jetbrains.rider.unity.test.framework.base.IntegrationTestWithGeneratedSolutionBase
import org.testng.annotations.Test

@Subsystem(SubsystemConstants.UNITY_UNIT_TESTING)
@Feature("DotCover in Unity")
@Severity(SeverityLevel.CRITICAL)
@Solution("SimpleUnityUnitTestingProject")
class DotCoverTest : IntegrationTestWithGeneratedSolutionBase() {
    override val withCoverage: Boolean
        get() = true

    @Test(description = "Check coverage of all tests from Unity solution", enabled = false) // Disabled until merge changes in "Start Unity with Coverage" action
    fun checkCoverAllTestsFromSolution() {
        buildSolutionWithReSharperBuild()
        withDcFacade(project) { ut, dc ->
            ut.waitForDiscovering()
            ut.coverAllTestsInSolution(5)
            dc.waitForTotal(22, goldFile = testGoldFile)
        }
    }
}