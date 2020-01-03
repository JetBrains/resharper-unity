package unityExplorer

import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.ProjectModelBaseTest
import com.jetbrains.rider.test.scriptingApi.TemplateType
import com.jetbrains.rider.test.scriptingApi.testProjectModel
import org.testng.annotations.Test

class UnityProjectModelViewExtensionsTest : ProjectModelBaseTest() {
    override fun getSolutionDirectoryName() = "com.unity.ide.rider"
    override val persistCaches: Boolean
        get() = true

    @Test
    @TestEnvironment(solution = "com.unity.ide.rider")
    fun testAddNewItem() {
        testProjectModel(testGoldFile, project, false) {
            //dump("Init", project, activeSolutionDirectory) {}
            dump("Add files and classes", project, activeSolutionDirectory) {
                // add file to Assets\AsmdefResponse\NewDirectory1 is ambig between 2 asmdef projects
                // add file to Assets\NewDirectory1 is ambig between predefined projects and asmdef
                // goes to Editor project

                addNewItem(project, arrayOf("Assets", "AsmdefResponse", "NewDirectory1"), TemplateType.CLASS, "AsmdefClass.cs")
                addNewItem(project, arrayOf("Assets", "NewDirectory1"), TemplateType.CLASS, "MainClass.cs")
                addNewItem(project, arrayOf("Assets", "Scripts", "Editor", "NewDirectory1"), TemplateType.CLASS, "EditorClass.cs")
            }
        }
    }
}
