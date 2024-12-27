package com.jetbrains.rider.unity.test.framework.base

import com.jetbrains.rd.util.lifetime.LifetimeDefinition
import com.jetbrains.rd.util.lifetime.isAlive
import com.jetbrains.rider.test.OpenSolutionParams
import com.jetbrains.rider.test.base.PerTestSolutionTestBase
import com.jetbrains.rider.test.scriptingApi.allowUnityPathVfsRootAccess
import com.jetbrains.rider.test.scriptingApi.createLibraryFolderIfNotExist
import com.jetbrains.rider.test.scriptingApi.killHangingUnityProcesses
import com.jetbrains.rider.unity.test.framework.api.activateRiderFrontendTest
import org.testng.annotations.AfterMethod
import org.testng.annotations.AfterSuite
import org.testng.annotations.BeforeMethod
import org.testng.annotations.BeforeSuite

abstract class IntegrationTestWithSolutionBase : PerTestSolutionTestBase() {
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

    @BeforeSuite(alwaysRun = true)
    fun cleanUpUnityProcessesBefore() {
        killHangingUnityProcesses()
    }

    @AfterSuite(alwaysRun = true)
    fun cleanUpUnityProcessesAfter() {
        killHangingUnityProcesses()
    }

    @AfterMethod(alwaysRun = true)
    fun terminateLifetimeDefinition() {
        if(::lifetimeDefinition.isInitialized && lifetimeDefinition.isAlive) {
            lifetimeDefinition.terminate()
        }
    }

    @BeforeMethod
    open fun setUpModelSettings() {
        activateRiderFrontendTest()
    }
}