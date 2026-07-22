package com.jetbrains.rider.unity.test.framework.base

import com.jetbrains.rd.util.lifetime.LifetimeDefinition
import com.jetbrains.rd.util.lifetime.isAlive
import com.jetbrains.rider.test.OpenSolutionParams
import com.jetbrains.rider.test.scriptingApi.allowUnityPathVfsRootAccess
import com.jetbrains.rider.test.scriptingApi.createLibraryFolderIfNotExist
import com.jetbrains.rider.unity.test.framework.api.activateRiderFrontendTest
import org.junit.jupiter.api.AfterEach
import org.junit.jupiter.api.BeforeEach

abstract class IntegrationTestWithSolutionBase : BaseTestWithUnitySetup() {
    override fun modifyOpenSolutionParams(params: OpenSolutionParams) {
        super.modifyOpenSolutionParams(params)
        params.waitForCaches = true
        params.preprocessTempDirectory = {
            lifetimeDefinition = LifetimeDefinition()
            allowUnityPathVfsRootAccess(lifetimeDefinition)
            createLibraryFolderIfNotExist(it)
        }
    }

    private lateinit var lifetimeDefinition: LifetimeDefinition

    // JUnit5 has no @BeforeMethod(dependsOnMethods=...); the per-test setup chain is orchestrated by
    // overriding the single @BeforeEach `setUpTestCaseSolution` and calling super first, then the extra
    // steps in order. This guarantees ordering across the hierarchy without relying on JUnit5's
    // unspecified intra-class @BeforeEach order.
    @BeforeEach
    override fun setUpTestCaseSolution() {
        super.setUpTestCaseSolution()
        setUpModelSettings()
    }

    open fun setUpModelSettings() {
        activateRiderFrontendTest()
    }

    @AfterEach
    fun terminateLifetimeDefinition() {
        if(::lifetimeDefinition.isInitialized && lifetimeDefinition.isAlive) {
            lifetimeDefinition.terminate()
        }
    }
}