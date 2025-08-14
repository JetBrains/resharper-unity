package com.jetbrains.rider.unity.test.cases

import com.jetbrains.rdclient.editors.FrontendTextControlHost
import com.jetbrains.rider.test.annotations.report.ChecklistItems
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.annotations.report.Severity
import com.jetbrains.rider.test.annotations.report.SeverityLevel
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.base.RefactoringsTestBase
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.framework.combine
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.*
import com.jetbrains.rider.unity.test.framework.api.prepareAssemblies
import org.testng.annotations.BeforeMethod
import org.testng.annotations.Test
import java.io.File

@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity quick fix project settings")
@Severity(SeverityLevel.NORMAL)
@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
@Solution("ProjectSettingsTestData")
class QuickFixProjectSettingsTest : RefactoringsTestBase() {
    @Test(description="Quick fix for adding to build settings")
    @ChecklistItems(["Quick Fix Project Settings/Adding to build settings"])
    fun testAddToBuildSettings() {
        withOpenedEditor(File("Assets").resolve("NewBehaviourScript.cs").path, "SceneCompletionTest.cs") {
            FrontendTextControlHost.getInstance(project!!)
            waitBackendDocumentChange(project!!, arrayListOf(this.virtualFile!!))
            callAltEnterMenu {
                executeItemByPrefix("Add 'Scenes/PossibleShortName' to build settings")
            }
        }

        writeProjectSettingsToGold()
    }

    @Test(description="Quick fix for enabling scene at build settings")
    @ChecklistItems(["Quick Fix Project Settings/Enabling scene at build settings"])
    fun testEnableSceneAtBuildSettings() {
        withOpenedEditor(File("Assets").resolve("NewBehaviourScript.cs").path, "SceneCompletionTest.cs") {
            FrontendTextControlHost.getInstance(project!!)
            waitBackendDocumentChange(project!!, arrayListOf(this.virtualFile!!))
            callAltEnterMenu {
                executeItemByPrefix("Enable scene")
            }
        }

        writeProjectSettingsToGold()
    }

    @Test(description="Quick fix for specifying full scene name")
    @ChecklistItems(["Quick Fix Project Settings/Specifying full scene name"])
    fun testSpecifyFullSceneName() {
        doTestWithDumpDocument {
            withOpenedEditor(File("Assets").resolve("NewBehaviourScript.cs").path, "SceneCompletionTest.cs") {
                FrontendTextControlHost.getInstance(project!!)
                waitBackendDocumentChange(project!!, arrayListOf(this.virtualFile!!))
                callAltEnterMenu {
                    executeItemByPrefix("Change scene name to 'Scenes/Folder/ImpossibleShortName'")
                }
            }
        }
    }

    private fun getProjectSettingsFromSolution(solutionFolder : File) : File {
        return solutionFolder.combine("ProjectSettings", "EditorBuildSettings.asset")
            .combine()
    }

    private fun writeProjectSettingsToGold() {
        executeWithGold(testGoldFile) {
            it.print(getProjectSettingsFromSolution(activeSolutionDirectory).readText())
        }
    }

    @BeforeMethod
    fun initializeEnvironment() {
        prepareAssemblies(project, activeSolutionDirectory)
    }
}