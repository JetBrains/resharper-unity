package com.jetbrains.rider.unity.test.cases.unityExplorer

import com.intellij.openapi.rd.util.lifetime
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.OpenSolutionParams
import com.jetbrains.rider.test.annotations.*
import com.jetbrains.rider.test.annotations.report.ChecklistItems
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.annotations.report.Issue
import com.jetbrains.rider.test.annotations.report.Severity
import com.jetbrains.rider.test.annotations.report.SeverityLevel
import com.jetbrains.rider.test.base.PerTestSettingsTestBase
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.scriptingApi.TemplateType
import com.jetbrains.rider.test.scriptingApi.prepareProjectView
import com.jetbrains.rider.test.scriptingApi.testProjectModel
import com.jetbrains.rider.test.scriptingApi.withSolution
import com.jetbrains.rider.unity.test.framework.api.*
import org.testng.annotations.Test
import java.time.Duration

@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity Explorer")
@Severity(SeverityLevel.CRITICAL)
@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE)
class UnityExplorerTest : PerTestSettingsTestBase() {

    @Test(description = "Add a new item with multiple backends")
    @Mute("RIDER-101228")
    @Issue("RIDER-70098 Rider adds Unity meta files in a non-Unity project")
    @TestSettings(allowMultipleBackends = true)
    @ChecklistItems(["Unity explorer/Add script with multiple backends"])
    fun testMultipleBackendsAddNewItem() {
        val params = OpenSolutionParams()
        params.waitForCaches = true
        params.forceOpenInNewFrame = true
        withSolution("EmptySolution", params) {
            withSolution("UnityProjectModelViewExtensionsTest", params) second@{
                prepareProjectView(this@second.project)
                testProjectModel(testGoldFile, this@second.project, false) {
                    withUnityExplorerPane(this@second.project, showAllFiles = true) {
                        dump("Add files and classes", this@second.project, activeSolutionDirectory) {
                            addNewItem2(this@second.project, arrayOf("Assets", "AsmdefResponse", "NewDirectory1"),
                                        TemplateType.CLASS,"AsmdefClass_added.cs"
                            )
                        }
                    }
                }
            }
        }
    }

    @Test(description="Add a new folder and script to the project")
    @ChecklistItems(["Unity explorer/Add folder and script"])
    fun testUnityExplorer01() {
        val params = OpenSolutionParams()
        withSolution("SimpleUnityProject", params) {
            prepareProjectView(project)
            testProjectModel(testGoldFile, project, false) {
                withUnityExplorerPane(project, showTildeFolders = false) {
                    dump("Add folders", project, activeSolutionDirectory) {
                        addNewFolder2(project, arrayOf("Assets"), "NewFolder1~")
                        addNewFolder2(project, arrayOf("Assets"), ".NewFolder1")
                    }
                }
                withUnityExplorerPane(project, true) {
                    dump("Show tilde folders", project, activeSolutionDirectory) {
                        addNewItem2(project, arrayOf("Assets", "NewFolder1~"), TemplateType.CLASS, "Class1.cs")
                    }
                }
                withUnityExplorerPane(project, showAllFiles = true) {
                    dump("Show All Files", project, activeSolutionDirectory) {
                        addNewItem2(project, arrayOf("Assets", "NewFolder1~"), TemplateType.CLASS, "Class2.cs")
                        addNewItem2(project, arrayOf("Assets", ".NewFolder1"), TemplateType.CLASS, "Class1.cs")
                    }
                }
                withUnityExplorerPane(project, showTildeFolders = false) {
                    dump("Hide all", project, activeSolutionDirectory) {
                        addNewItem2(project, arrayOf("Assets"), TemplateType.CLASS, "Class1.cs")
                    }
                }
            }
        }
    }

    @Test(description = "Test project loading with a special folder")
    @Issue("RIDER-92886")
    @TestSettings(sdkVersion = SdkVersion.LATEST_STABLE)
    @TestEnvironment(platform = [PlatformType.MAC_OS_ALL, PlatformType.LINUX_ALL])
    @ChecklistItems(["Unity explorer/Loading with a special folder"])
    fun test_project_loading_with_special_folder() { // infinite loading caused by a "..\\" folder
        withSolution("AnimImplicitUsageTest", OpenSolutionParams().apply {
            preprocessTempDirectory = {
                prepareAssemblies(it)
                val processBuilder = ProcessBuilder("mkdir", "..\\")
                processBuilder.directory(it.resolve("Assets"))
                processBuilder.start()
            }
        }) {
            prepareProjectView(project)
            waitAndPump(project.lifetime, { project.solution.frontendBackendModel.isDeferredCachesCompletedOnce.valueOrDefault(false) },
                        Duration.ofSeconds(10), { "Deferred caches are not completed" })
        }
    }

    // todo: RIDER-74817 Dump UnityExplorer tooltips in tests
}