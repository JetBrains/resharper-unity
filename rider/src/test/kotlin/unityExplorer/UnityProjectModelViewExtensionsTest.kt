package unityExplorer

import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.ProjectModelBaseTest
import com.jetbrains.rider.test.scriptingApi.TemplateType
import com.jetbrains.rider.test.scriptingApi.testProjectModel
import org.testng.Assert
import org.testng.annotations.Test
import java.nio.file.Paths

class UnityProjectModelViewExtensionsTest : ProjectModelBaseTest() {
    override fun getSolutionDirectoryName() = "com.unity.ide.rider"
    override val persistCaches: Boolean
        get() = true

    @Test
    @TestEnvironment(solution = "UnityProjectModelViewExtensionsTest")
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
    @TestEnvironment(solution = "UnityProjectModelViewExtensionsTest")
    fun testRenameFile() {
        testProjectModel(testGoldFile, project, false) {
            dump("Rename file", project, activeSolutionDirectory) {
                val metaFileContent = Paths.get(project.basePath!!).resolve("Assets").resolve("AsmdefResponse").resolve("NewBehaviourScript.cs.meta").toFile().readText()

                doActionAndWait(project, {
                    renameItem(project, arrayOf("Assets", "AsmdefResponse", "NewBehaviourScript.cs"), "NewBehaviourScript_renamed.cs")
                },true)

                val metaFile = Paths.get(project.basePath!!).resolve("Assets").resolve("AsmdefResponse").resolve("NewBehaviourScript_renamed.cs.meta").toFile()
                Assert.assertTrue(metaFile.exists(), "meta file $metaFile doesn't exist.")
                Assert.assertEquals(metaFileContent, metaFile.readText())
            }
        }
    }

    @Test
    @TestEnvironment(solution = "UnityProjectModelViewExtensionsTest")
    fun testRenameFolder() {
        testProjectModel(testGoldFile, project, false) {
            dump("Rename folder", project, activeSolutionDirectory) {
                val metaFileContent = Paths.get(project.basePath!!).resolve("Assets").resolve("Dir1.meta").toFile().readText()

                doActionAndWait(project, {
                    renameItem(project, arrayOf("Assets", "Dir1"), "Dir1_renamed")
                },true)

                val metaFile = Paths.get(project.basePath!!).resolve("Assets").resolve("Dir1_renamed.meta").toFile()
                Assert.assertTrue(metaFile.exists(), "meta file $metaFile doesn't exist.")
                Assert.assertEquals(metaFileContent, metaFile.readText())
            }
        }
    }

    @Test
    @TestEnvironment(solution = "UnityProjectModelViewExtensionsTest")
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
    @TestEnvironment(solution = "UnityProjectModelViewExtensionsTest")
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
    @TestEnvironment(solution = "UnityProjectModelViewExtensionsTest")
    fun testDeleteFile() {
        testProjectModel(testGoldFile, project, false) {
            dump("Rename folder", project, activeSolutionDirectory) {
                val metaFile = Paths.get(project.basePath!!).resolve("Assets").resolve("AsmdefResponse").resolve("NewBehaviourScript.cs.meta").toFile()
                Assert.assertTrue(metaFile.exists(), "We expect meta file exists.")

                doActionAndWait(project, {
                    deleteElement(project, arrayOf("Assets", "AsmdefResponse", "NewBehaviourScript.cs"))
                },true)

                Assert.assertFalse(metaFile.exists(), "We expect meta file removed.")
            }
        }
    }

    @Test
    @TestEnvironment(solution = "UnityProjectModelViewExtensionsTest") // RIDER-41182
    fun testMoveFile() {
        val metaFile = Paths.get(project.basePath!!).resolve("Assets").resolve("Class1.cs.meta").toFile()
        val metaFileContent = metaFile.readText()
        Assert.assertTrue(metaFile.exists(), "We expect meta file exists.")

        testProjectModel(testGoldFile, project, false) {
            dump("Move file", project, activeSolutionDirectory) {
                doActionAndWait(project, {
                    doActionAndWait(project, {
                        cutItem2(project, arrayOf("Assets", "Class1.cs"))
                        pasteItem2(project, arrayOf("Assets", "AsmdefResponse", "NewDirectory1"), "Class1.cs")
                    }, true)
                },true)

                Assert.assertFalse(metaFile.exists(), "We expect meta file removed.")
            }
        }

        val movedMetaFile = Paths.get(project.basePath!!).resolve("Assets").resolve("AsmdefResponse").resolve("NewDirectory1").resolve("Class1.cs.meta").toFile()
        Assert.assertTrue(movedMetaFile.exists(), "meta file $movedMetaFile doesn't exist.")
        Assert.assertEquals(metaFileContent, movedMetaFile.readText())
    }
}
