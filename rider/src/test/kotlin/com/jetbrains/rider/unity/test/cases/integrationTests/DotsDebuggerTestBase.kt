package com.jetbrains.rider.unity.test.cases.integrationTests

import com.intellij.xdebugger.XDebuggerManager
import com.intellij.xdebugger.breakpoints.XLineBreakpoint
import com.jetbrains.rider.debugger.breakpoint.DotNetLineBreakpointProperties
import com.jetbrains.rider.plugins.unity.debugger.breakpoints.UnityPausepointBreakpointType
import com.jetbrains.rider.plugins.unity.debugger.breakpoints.convertToLineBreakpoint
import com.jetbrains.rider.test.allure.Subsystem
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.framework.combine
import com.jetbrains.rider.test.scriptingApi.*
import com.jetbrains.rider.unity.test.framework.UnityVersion
import com.jetbrains.rider.unity.test.framework.api.*
import com.jetbrains.rider.unity.test.framework.base.IntegrationTestWithUnityProjectBase
import io.qameta.allure.Epic
import io.qameta.allure.Feature
import io.qameta.allure.Severity
import io.qameta.allure.SeverityLevel
import org.testng.annotations.AfterMethod
import org.testng.annotations.BeforeMethod
import org.testng.annotations.Test
import java.io.File

abstract class DotsDebuggerTestBase(private val unityVersion: UnityVersion) : IntegrationTestWithUnityProjectBase() {

    override fun getSolutionDirectoryName() = "UnityDotsDebug/Project"
    override val unityMajorVersion = this.unityVersion

    override val testClassDataDirectory: File
        get() = super.testClassDataDirectory.parentFile.combine(DotsDebuggerTestBase::class.simpleName!!)
    override val testCaseSourceDirectory: File
        get() = testClassDataDirectory.combine(super.testStorage.testMethod.name).combine("source")

    @Test
    fun checkBreakpointInDOTSCode() {
        attachDebuggerToUnityEditorAndPlay(
            {
                toggleBreakpoint("ResetTransformSystem.cs", 26)
            },
            {
                waitForPause()
                dumpFullCurrentData()
                resumeSession()
            }, testGoldFile)
    }

    @Test
    fun checkUnityPausePoint() {
        attachDebuggerToUnityEditorAndPlay(
            test = {
                waitForUnityEditorPlayMode()
                toggleUnityPausepoint(project, "ResetTransformSystem.cs", 26)
                waitForUnityEditorPauseMode()
                removeAllUnityPausepoints()
                unpause()
            })
    }

    @AfterMethod(alwaysRun = true)
    fun removeAllUnityPausepoints() {
        XDebuggerManager.getInstance(project).breakpointManager.allBreakpoints.filter {
            it.type is UnityPausepointBreakpointType
        }.forEach {
            @Suppress("UNCHECKED_CAST")
            (convertToLineBreakpoint(project,
                                     it as XLineBreakpoint<DotNetLineBreakpointProperties>))
        }
        removeAllBreakpoints()
    }

    //TODO solution build throws error on code generation phase
    @BeforeMethod(dependsOnMethods = ["waitForUnityRunConfigurations"])
    override fun buildSolutionAfterUnityStarts() {
    }

    //TODO checkSwea hangs for unknown reason
    @AfterMethod
    override fun checkSwea() {
    }
}

@Epic(Subsystem.UNITY_DEBUG)
@Feature("Debug Unity2022")
@Severity(SeverityLevel.CRITICAL)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
class DotsDebuggerTest2022 : DotsDebuggerTestBase(UnityVersion.V2022) {
}
