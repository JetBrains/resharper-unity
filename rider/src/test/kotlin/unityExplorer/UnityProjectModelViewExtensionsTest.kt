package unityExplorer

import base.*
import com.jetbrains.rider.projectView.solutionDirectory
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.ProjectModelBaseTest
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.enums.ToolsetVersion
import com.jetbrains.rider.test.scriptingApi.TemplateType
import com.jetbrains.rider.test.scriptingApi.testProjectModel
import org.testng.Assert
import org.testng.annotations.Test
import java.io.File

@TestEnvironment(toolset = ToolsetVersion.TOOLSET_17_CORE, coreVersion = CoreVersion.DOT_NET_6)
class UnityProjectModelViewExtensionsTest : ProjectModelBaseTest() {
    override fun getSolutionDirectoryName() = "UnityProjectModelViewExtensionsTest"
    override val persistCaches: Boolean
        get() = true

    // todo: add test with solution, where one of the asmdef-s doesn't target Editor, this would cause only .Player project without normal one

    @Test
    fun testAddNewItem() {
        testProjectModel(testGoldFile, project, false) {
            //dump("Init", project, activeSolutionDirectory) {}
            dump("Add files and classes", project, activeSolutionDirectory) {
                // add file to Assets\AsmdefResponse\NewDirectory1 is ambig between 2 asmdef projects
                // add file to Assets\NewDirectory1 is ambig between predefined projects and asmdef
                // goes to Editor project

                addNewItem(project, arrayOf("Assets", "AsmdefResponse", "NewDirectory1"), TemplateType.CLASS, "AsmdefClass_added.cs")
                addNewItem(project, arrayOf("Assets", "NewDirectory1"), TemplateType.CLASS, "MainClass_added.cs")
                addNewItem(project, arrayOf("Assets", "Scripts", "Editor", "NewDirectory1"), TemplateType.CLASS, "EditorClass_added.cs")
            }
        }
    }

    @Test
    fun testRenameFile() {
        testProjectModel(testGoldFile, project, false) {
            dump("Rename file", project, activeSolutionDirectory) {
                val metaFileContent = project.solutionDirectory.resolve("Assets").resolve("AsmdefResponse").resolve("NewBehaviourScript.cs.meta").readText()

                doActionAndWait(project, {
                    renameItem(project, arrayOf("Assets", "AsmdefResponse", "NewBehaviourScript.cs"), "NewBehaviourScript_renamed.cs")
                },true)

                val metaFile = project.solutionDirectory.resolve("Assets").resolve("AsmdefResponse").resolve("NewBehaviourScript_renamed.cs.meta")
                Assert.assertTrue(metaFile.exists(), "meta file $metaFile doesn't exist.")
                Assert.assertEquals(metaFileContent, metaFile.readText())
            }
        }
    }

    @Test
    @TestEnvironment
    fun testRenameFolder() {
        testProjectModel(testGoldFile, project, false) {
            dump("Rename folder", project, activeSolutionDirectory) {
                val metaFileContent = project.solutionDirectory.resolve("Assets").resolve("Dir1.meta").readText()

                doActionAndWait(project, {
                    renameItem(project, arrayOf("Assets", "Dir1"), "Dir1_renamed")
                },true)

                val metaFile = project.solutionDirectory.resolve("Assets").resolve("Dir1_renamed.meta")
                Assert.assertTrue(metaFile.exists(), "meta file $metaFile doesn't exist.")
                Assert.assertEquals(metaFileContent, metaFile.readText())
            }
        }
    }

    @Test
    @TestEnvironment
    fun testRenameFolder2() {
        testProjectModel(testGoldFile, project, false) {
            dump("Rename folder", project, activeSolutionDirectory) {
                doActionAndWait(project, {
                    // folder exists in multiple projects at once
                    renameItem(project, arrayOf("Assets", "AsmdefResponse", "NewDirectory1"), "NewDirectory1_renamed")
                },true)
            }
        }
    }

    @Test
    @TestEnvironment
    fun testRenameFolder3() {
        testProjectModel(testGoldFile, project, false) {
            dump("Rename folder", project, activeSolutionDirectory) {
                doActionAndWait(project, {
                    // folder exists in multiple projects at once, it not empty
                    renameItem(project, arrayOf("Assets", "AsmdefResponse", "SS"), "SS_renamed")
                },true)
            }
        }
    }

    @Test
    fun testDeleteFile() {
        val metaFile = project.solutionDirectory.resolve("Assets/AsmdefResponse/NewBehaviourScript.cs.meta")
        Assert.assertTrue(metaFile.exists(), "We expect meta file exists.")
        testProjectModel(testGoldFile, project, false) {
            dump("Delete element", project, activeSolutionDirectory) {
                deleteElement(project, arrayOf("Assets", "AsmdefResponse", "NewBehaviourScript.cs"))
            }
        }

        Assert.assertFalse(metaFile.exists(), "We expect meta file removed.")
    }

    @Test // RIDER-41182
    fun testMoveFile() {
        val originFile = project.solutionDirectory.resolve("Assets").resolve("Class1.cs")
        val originMetaFile = File(originFile.absolutePath+".meta")
        val metaFileContent = originMetaFile.readText()
        val movedFile = project.solutionDirectory.resolve("Assets").resolve("AsmdefResponse").resolve("NewDirectory1").resolve("Class1.cs")
        Assert.assertTrue(originFile.exists(), "We expect file exists.")
        Assert.assertTrue(originMetaFile.exists(), "We expect meta file exists.")

        testProjectModel(testGoldFile, project, false) {
            dump("Move file", project, activeSolutionDirectory) {
                cutItem2(project, arrayOf("Assets", "Class1.cs"))
                pasteItem2(project, arrayOf("Assets", "AsmdefResponse", "NewDirectory1"))
            }
        }

        Assert.assertFalse(originFile.exists(), "We expect $originFile removed.")
        Assert.assertFalse(originMetaFile.exists(), "We expect $originMetaFile file removed.")
        Assert.assertTrue(movedFile.exists(), "$movedFile should have been moved.")
        val movedMetaFile = File(movedFile.absolutePath+".meta")
        Assert.assertTrue(movedMetaFile.exists(), "meta file $movedMetaFile doesn't exist.")
        Assert.assertEquals(metaFileContent, movedMetaFile.readText())
    }

    @Test // RIDER-63575
    fun testMoveFile2() {
        val originFile = project.solutionDirectory.resolve("Assets/AsmdefResponse/SS/rrr.cs")
        val originMetaFile = File(originFile.absolutePath+".meta")
        val metaFileContent = originMetaFile.readText()
        val movedFile = project.solutionDirectory.resolve("Assets/rrr.cs")
        Assert.assertTrue(originFile.exists(), "We expect file exists.")
        Assert.assertTrue(originMetaFile.exists(), "We expect meta file exists.")

        testProjectModel(testGoldFile, project, false) {
            dump("Move file", project, activeSolutionDirectory) {
                cutItem2(project, arrayOf("Assets", "AsmdefResponse", "SS", "rrr.cs"))
                pasteItem2(project, arrayOf("Assets"))
            }
        }

        Assert.assertFalse(originFile.exists(), "We expect $originFile removed.")
        Assert.assertFalse(originMetaFile.exists(), "We expect $originMetaFile file removed.")
        Assert.assertTrue(movedFile.exists(), "$movedFile should have been moved.")
        val movedMetaFile = File(movedFile.absolutePath+".meta")
        Assert.assertTrue(movedMetaFile.exists(), "meta file $movedMetaFile doesn't exist.")
        Assert.assertEquals(metaFileContent, movedMetaFile.readText())
    }
}
