package com.jetbrains.rider.unity.test.framework.base

import com.jetbrains.rd.util.lifetime.LifetimeDefinition
import com.jetbrains.rd.util.lifetime.isAlive
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.scriptingApi.allowUnityPathVfsRootAccess
import com.jetbrains.rider.test.scriptingApi.createLibraryFolderIfNotExist
import com.jetbrains.rider.test.scriptingApi.killHangingUnityProcesses
import com.jetbrains.rider.unity.test.framework.api.*
import org.testng.annotations.AfterMethod
import org.testng.annotations.AfterSuite
import org.testng.annotations.BeforeMethod
import org.testng.annotations.BeforeSuite
import java.io.File

abstract class IntegrationTestWithSolutionBase : BaseTestWithSolution(), IntegrationTestWithFrontendBackendModel {
    override val waitForCaches = true

    override val frontendBackendModel: FrontendBackendModel
        get() = project.solution.frontendBackendModel

    private lateinit var lifetimeDefinition: LifetimeDefinition

    override fun preprocessTempDirectory(tempDir: File) {
        lifetimeDefinition = LifetimeDefinition()
        allowUnityPathVfsRootAccess(lifetimeDefinition)
        createLibraryFolderIfNotExist(tempDir)
    }

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