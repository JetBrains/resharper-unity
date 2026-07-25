package com.jetbrains.rider.unity.test.cases.unityExplorer

import com.intellij.openapi.vfs.VfsUtil
import com.jetbrains.rider.projectView.solutionDirectoryPath
import com.jetbrains.rider.test.OpenSolutionParams
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.annotations.report.ChecklistItems
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.annotations.report.Issue
import com.jetbrains.rider.test.annotations.report.Issues
import com.jetbrains.rider.test.annotations.report.Severity
import com.jetbrains.rider.test.annotations.report.SeverityLevel
import com.jetbrains.rider.test.junit5.base.ProjectModelBaseTest
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.framework.advancedSettings.AdvancedSettingsList
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import com.jetbrains.rider.test.scriptingApi.TemplateType
import com.jetbrains.rider.test.scriptingApi.callUndo
import com.jetbrains.rider.test.scriptingApi.openFileInEditor
import com.jetbrains.rider.test.scriptingApi.testProjectModel
import com.jetbrains.rider.unity.test.framework.api.addNewItem2
import com.jetbrains.rider.unity.test.framework.api.cutItem2
import com.jetbrains.rider.unity.test.framework.api.deleteElement
import com.jetbrains.rider.unity.test.framework.api.doActionAndWait
import com.jetbrains.rider.unity.test.framework.api.dump
import com.jetbrains.rider.unity.test.framework.api.pasteItem2
import com.jetbrains.rider.unity.test.framework.api.renameItem
import org.junit.jupiter.api.Assertions
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test
import kotlin.io.path.exists
import kotlin.io.path.name
import kotlin.io.path.readText

@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity Project Model View Extensions")
@Severity(SeverityLevel.CRITICAL)
@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
@Solution("UnityProjectModelViewExtensionsTest")
@Tag(TeamCityTags.Plugins.Unity.General)
class UnityProjectModelViewExtensionsTest : ProjectModelBaseTest() {

    override val advancedSettings: AdvancedSettingsList
        get() = AdvancedSettingsList(boolSettings = mapOf(("repository.view.enabled" to false)))

    override fun modifyOpenSolutionParams(params: OpenSolutionParams) {
        super.modifyOpenSolutionParams(params)
        params.persistCaches = true
    }

    // todo: add test with solution, where one of the asmdef-s doesn't target Editor, this would cause only .Player project without normal one

    @Test // Add a new script to the project
    @ChecklistItems(["Unity explorer/Add new script"])
    fun testAddNewItem() {
        testProjectModel(testGoldFile, project, false) {
            //dump("Init", project, activeSolutionDirectory) {}
            dump("Add files and classes", project, activeSolutionDirectory) {
                // add file to Assets\AsmdefResponse\NewDirectory1 is ambig between 2 asmdef projects
                // add file to Assets\NewDirectory1 is ambig between predefined projects and asmdef
                // goes to Editor project

                addNewItem2(project, arrayOf("Assets", "AsmdefResponse", "NewDirectory1"), TemplateType.CLASS, "AsmdefClass_added.cs")
                addNewItem2(project, arrayOf("Assets", "NewDirectory1"), TemplateType.CLASS, "MainClass_added.cs")
                addNewItem2(project, arrayOf("Assets", "Scripts", "Editor", "NewDirectory1"), TemplateType.CLASS, "EditorClass_added.cs")
            }
        }
    }

    @Test // Rename an script in the project
    @ChecklistItems(["Unity explorer/Rename script"])
    fun testRenameFile() {
        testProjectModel(testGoldFile, project, false) {
            dump("Rename file", project, activeSolutionDirectory) {
                val metaFileContent = project.solutionDirectoryPath.resolve("Assets").resolve("AsmdefResponse").resolve("NewBehaviourScript.cs.meta").readText()

                doActionAndWait(project, {
                    renameItem(project, arrayOf("Assets", "AsmdefResponse", "NewBehaviourScript.cs"), "NewBehaviourScript_renamed.cs")
                }, true)

                val metaFile = project.solutionDirectoryPath.resolve("Assets").resolve("AsmdefResponse")
                    .resolve("NewBehaviourScript_renamed.cs.meta")
                Assertions.assertTrue(metaFile.exists(), "meta file $metaFile doesn't exist.")
                Assertions.assertEquals(metaFileContent, metaFile.readText())
            }
        }
    }

    @Test // Rename a folder in the project
    @ChecklistItems(["Unity explorer/Rename folder"])
    fun testRenameFolder() {
        testProjectModel(testGoldFile, project, false) {
            dump("Rename folder", project, activeSolutionDirectory) {
                val metaFileContent = project.solutionDirectoryPath.resolve("Assets").resolve("Dir1.meta").readText()

                doActionAndWait(project, {
                    renameItem(project, arrayOf("Assets", "Dir1"), "Dir1_renamed")
                }, true)

                val metaFile = project.solutionDirectoryPath.resolve("Assets").resolve("Dir1_renamed.meta")
                Assertions.assertTrue(metaFile.exists(), "meta file $metaFile doesn't exist.")
                Assertions.assertEquals(metaFileContent, metaFile.readText())
            }
        }
    }

    @Test // Rename a folder in the project
    @ChecklistItems(["Unity explorer/Rename folder"])
    fun testRenameFolder2() {
        testProjectModel(testGoldFile, project, false) {
            dump("Rename folder", project, activeSolutionDirectory) {
                doActionAndWait(project, {
                    // folder exists in multiple projects at once
                    renameItem(project, arrayOf("Assets", "AsmdefResponse", "NewDirectory1"), "NewDirectory1_renamed")
                }, true)
            }
        }
    }

    @Test // Rename a folder in the project
    @ChecklistItems(["Unity explorer/Rename folder"])
    fun testRenameFolder3() {
        testProjectModel(testGoldFile, project, false) {
            dump("Rename folder", project, activeSolutionDirectory) {
                doActionAndWait(project, {
                    // folder exists in multiple projects at once, it not empty
                    renameItem(project, arrayOf("Assets", "AsmdefResponse", "SS"), "SS_renamed")
                }, true)
            }
        }
    }

    @Test // Delete a script in the project
    @ChecklistItems(["Unity explorer/Delete script"])
    fun testDeleteFile() {
        val metaFile = project.solutionDirectoryPath.resolve("Assets/AsmdefResponse/NewBehaviourScript.cs.meta")
        Assertions.assertTrue(metaFile.exists(), "We expect meta file exists.")
        // helps Local History to capture the file content
        val vf = VfsUtil.findFile(project.solutionDirectoryPath.resolve("Assets/AsmdefResponse/NewBehaviourScript.cs"), true)!!
        openFileInEditor(vf)
        testProjectModel(testGoldFile, project, false) {
            dump("Delete element", project, activeSolutionDirectory) {
                deleteElement(project, arrayOf("Assets", "AsmdefResponse", "NewBehaviourScript.cs"))
            }
        }

        Assertions.assertFalse(metaFile.exists(), "We expect meta file removed.")
        callUndo(project)
        Assertions.assertTrue(metaFile.exists(), "We expect meta file restored.")

    }

    @Test // Move a script in the project
    @Issues([Issue("RIDER-41182"), Issue("RIDER-91321")])
    @ChecklistItems(["Unity explorer/Move script"])
    fun testMoveFile() {
        val originFile = project.solutionDirectoryPath.resolve("Assets").resolve("Class1.cs")
        val originMetaFile = originFile.resolveSibling(originFile.name + ".meta")
        val metaFileContent = originMetaFile.readText()
        val movedFile = project.solutionDirectoryPath.resolve("Assets").resolve("AsmdefResponse").resolve("NewDirectory1").resolve("Class1.cs")
        Assertions.assertTrue(originFile.exists(), "We expect file exists.")
        Assertions.assertTrue(originMetaFile.exists(), "We expect meta file exists.")

        testProjectModel(testGoldFile, project, false) {
            dump("Move file", project, activeSolutionDirectory) {
                cutItem2(project, arrayOf("Assets", "Class1.cs"))
                pasteItem2(project, arrayOf("Assets", "AsmdefResponse", "NewDirectory1"))
            }
        }

        Assertions.assertFalse(originFile.exists(), "We expect $originFile removed.")
        Assertions.assertFalse(originMetaFile.exists(), "We expect $originMetaFile file removed.")
        Assertions.assertTrue(movedFile.exists(), "$movedFile should have been moved.")
        val movedMetaFile = movedFile.resolveSibling(movedFile.name + ".meta")
        Assertions.assertTrue(movedMetaFile.exists(), "meta file $movedMetaFile doesn't exist.")
        Assertions.assertEquals(metaFileContent, movedMetaFile.readText())

        callUndo(project)
        Assertions.assertTrue(originFile.exists(), "We expect $originFile removed.")
        Assertions.assertTrue(originMetaFile.exists(), "We expect $originMetaFile file removed.")
        Assertions.assertFalse(movedFile.exists(), "$movedFile should have been moved.")
        Assertions.assertFalse(movedMetaFile.exists(), "meta file $movedMetaFile doesn't exist.")
        Assertions.assertEquals(metaFileContent, originMetaFile.readText())
    }

    @Test // Move a script in the project
    @Issue("RIDER-63575")
    @ChecklistItems(["Unity explorer/Move script"])
    fun testMoveFile2() {
        val originFile = project.solutionDirectoryPath.resolve("Assets/AsmdefResponse/SS/rrr.cs")
        val originMetaFile = originFile.resolveSibling(originFile.name + ".meta")
        val metaFileContent = originMetaFile.readText()
        val movedFile = project.solutionDirectoryPath.resolve("Assets/rrr.cs")
        Assertions.assertTrue(originFile.exists(), "We expect file exists.")
        Assertions.assertTrue(originMetaFile.exists(), "We expect meta file exists.")

        testProjectModel(testGoldFile, project, false) {
            dump("Move file", project, activeSolutionDirectory) {
                cutItem2(project, arrayOf("Assets", "AsmdefResponse", "SS", "rrr.cs"))
                pasteItem2(project, arrayOf("Assets"))
            }
        }

        Assertions.assertFalse(originFile.exists(), "We expect $originFile removed.")
        Assertions.assertFalse(originMetaFile.exists(), "We expect $originMetaFile file removed.")
        Assertions.assertTrue(movedFile.exists(), "$movedFile should have been moved.")
        val movedMetaFile = movedFile.resolveSibling(movedFile.name + ".meta")
        Assertions.assertTrue(movedMetaFile.exists(), "meta file $movedMetaFile doesn't exist.")
        Assertions.assertEquals(metaFileContent, movedMetaFile.readText())
    }
}
