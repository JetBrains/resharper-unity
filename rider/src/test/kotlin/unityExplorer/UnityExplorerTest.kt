package unityExplorer

import base.addNewFolder2
import base.addNewItem2
import base.dump
import base.withUnityExplorerPane
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.BaseTestWithSolutionBase
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.enums.ToolsetVersion
import com.jetbrains.rider.test.scriptingApi.TemplateType
import com.jetbrains.rider.test.scriptingApi.prepareProjectView
import com.jetbrains.rider.test.scriptingApi.testProjectModel
import org.testng.annotations.Test

@TestEnvironment(toolset = ToolsetVersion.TOOLSET_17_CORE, coreVersion = CoreVersion.DOT_NET_6)
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
    // todo: RIDER-74815
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
                        // here is our NewFolder1~ folder, which is presented without tilde
                        addNewItem2(project, arrayOf("Assets", "NewFolder1"), TemplateType.CLASS, "Class1.cs")
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

    // todo: RIDER-74817 Dump UnityExplorer tooltips in tests
}