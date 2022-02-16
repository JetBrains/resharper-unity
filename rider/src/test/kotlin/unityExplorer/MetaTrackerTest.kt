package unityExplorer

import base.addNewItem
import base.dump
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.BaseTestWithSolutionBase
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.enums.ToolsetVersion
import com.jetbrains.rider.test.scriptingApi.TemplateType
import com.jetbrains.rider.test.scriptingApi.testProjectModel
import org.testng.annotations.Test

@TestEnvironment(toolset = ToolsetVersion.TOOLSET_17_CORE, coreVersion = CoreVersion.DOT_NET_6, allowMultipleBackends = true)
class MetaTrackerTest : BaseTestWithSolutionBase() {

    @Test //RIDER-70098 Rider adds Unity meta files in a non-Unity project
    fun testAddNewItem() {
        val params = OpenSolutionParams()
        params.waitForCaches = true
        params.forceOpenInNewFrame = true
        withSolution("EmptySolution", params) { _ ->
            withSolution("UnityProjectModelViewExtensionsTest", params) { project ->
                testProjectModel(testGoldFile, project, false) {
                    dump("Add files and classes", project, activeSolutionDirectory) {
                        addNewItem(project, arrayOf("Assets", "AsmdefResponse", "NewDirectory1"), TemplateType.CLASS, "AsmdefClass_added.cs")
                    }
                }
            }
        }
    }
}