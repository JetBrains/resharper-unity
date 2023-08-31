package unityExplorer

import base.addNewFolder2
import base.addNewItem2
import base.dump
import base.integrationTests.prepareAssemblies
import base.withUnityExplorerPane
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.OpenSolutionParams
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.BaseTestWithSolutionBase
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.scriptingApi.TemplateType
import com.jetbrains.rider.test.scriptingApi.prepareProjectView
import com.jetbrains.rider.test.scriptingApi.testProjectModel
import io.qameta.allure.Issue
import org.testng.annotations.Test
import java.time.Duration

@TestEnvironment(sdkVersion = SdkVersion.DOT_NET_6)
class UnityExplorerTest : BaseTestWithSolutionBase() {

    @Test //RIDER-70098 Rider adds Unity meta files in a non-Unity project
    @TestEnvironment(allowMultipleBackends = true)
    fun testMultipleBackendsAddNewItem() {
        val params = OpenSolutionParams()
        params.waitForCaches = true
        params.forceOpenInNewFrame = true
        withSolution("EmptySolution", params) { _ ->
            withSolution("UnityProjectModelViewExtensionsTest", params) { project ->
                prepareProjectView(project)
                testProjectModel(testGoldFile, project, false) {
                    withUnityExplorerPane(project, showAllFiles = true) {
                        dump("Add files and classes", project, activeSolutionDirectory) {
                            addNewItem2(project, arrayOf("Assets", "AsmdefResponse", "NewDirectory1"),
                                TemplateType.CLASS,"AsmdefClass_added.cs"
                            )
                        }
                    }
                }
            }
        }
    }

    @Test
    fun testUnityExplorer01() {
        val params = OpenSolutionParams()
        withSolution("SimpleUnityProject", params) { project ->
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

    @Test
    @Issue("RIDER-92886")
    @TestEnvironment(sdkVersion = SdkVersion.DOT_NET_6, platform = [PlatformType.MAC_OS_ALL, PlatformType.LINUX_ALL])
    fun test_project_loading_with_special_folder() { // infinite loading caused by a "..\\" folder
        val params = OpenSolutionParams()
        withSolution("AnimImplicitUsageTest", params, preprocessTempDirectory = {
            prepareAssemblies(activeSolutionDirectory)
            val processBuilder = ProcessBuilder("mkdir", "..\\")
            processBuilder.directory(it.resolve("Assets"))
            processBuilder.start()
        }) { project ->
            prepareProjectView(project)
            waitAndPump(project.lifetime, { project.solution.frontendBackendModel.isDeferredCachesCompletedOnce.valueOrDefault(false)}, Duration.ofSeconds(10), { "Deferred caches are not completed" })
        }
    }

    // todo: RIDER-74817 Dump UnityExplorer tooltips in tests
}