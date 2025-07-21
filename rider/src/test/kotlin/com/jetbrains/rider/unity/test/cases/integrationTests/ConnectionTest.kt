package com.jetbrains.rider.unity.test.cases.integrationTests

import com.intellij.openapi.rd.util.lifetime
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.projectView.solutionDirectory
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.annotations.*
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.annotations.report.Severity
import com.jetbrains.rider.test.annotations.report.SeverityLevel
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.waitFirstScriptCompilation
import com.jetbrains.rider.unity.test.framework.api.*
import com.jetbrains.rider.unity.test.framework.base.IntegrationTestWithSolutionBase
import org.testng.annotations.Test

@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Connection with Unity Editor")
@Severity(SeverityLevel.CRITICAL)
@TestRequirements(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL]) // todo: allow Linux
@Solution("SimpleUnityProjectWithoutPlugin")
class ConnectionTest : IntegrationTestWithSolutionBase() {
    @Test(enabled = false, // RIDER-105806 Drop the EditorPlugin functionality for Unity versions prior to 2019.2
        description = "Check connection with Unity after Unity start with Simple Unity Project without plugin")
    fun installAndCheckConnectionAfterUnityStart() {
        withUnityProcess {
            waitFirstScriptCompilation(project)
            waitConnectionToUnityEditor(project)
            checkSweaInSolution()
        }
    }

    @Test(enabled = false, // RIDER-105806 Drop the EditorPlugin functionality for Unity versions prior to 2019.2
          description = "Check connection with Unity before Unity start with Simple Unity Project without plugin")
    fun installAndCheckConnectionBeforeUnityStart() {
        withUnityProcess {
            waitFirstScriptCompilation(project)
            waitConnectionToUnityEditor(project)
            checkSweaInSolution()
        }
    }

    @Test(enabled = false, // RIDER-105806 Drop the EditorPlugin functionality for Unity versions prior to 2019.2
          description = "Check external Editor in Unity settings")
    fun checkExternalEditorWithExecutingMethod() = checkExternalEditor(false) {
        executeIntegrationTestMethod("DumpExternalEditor")
    }

    @Test(description = "Check external Editor in Unity with Unity model refresh", enabled = false)
    fun checkExternalEditorWithUnityModelRefresh() = checkExternalEditor(true) { executeScript("DumpExternalEditor.cs") }

    private fun checkExternalEditor(resetEditorPrefs: Boolean, execute: () -> Unit) {
        withUnityProcess(resetEditorPrefs = resetEditorPrefs, useRiderTestPath = true) {
            waitFirstScriptCompilation(project)
            waitConnectionToUnityEditor(project)

            val externalEditorPath = project.solutionDirectory.resolve("Assets/ExternalEditor.txt")

            execute()
            waitAndPump(project.lifetime, { externalEditorPath.exists() }, unityDefaultTimeout)
            { "ExternalEditor.txt is not created" }
            waitAndPump(project.lifetime, { externalEditorPath.readText().isNotEmpty() }, unityDefaultTimeout)
            { "ExternalEditor.txt is empty" }

            executeWithGold(testGoldFile) {
                it.print(externalEditorPath.readText())
            }

            checkSweaInSolution()
        }
    }

    @Test(enabled = false, // RIDER-105806 Drop the EditorPlugin functionality for Unity versions prior to 2019.2
          description = "Check Unity Log")
    fun checkLogWithExecutingMethod() = checkLog { executeIntegrationTestMethod("WriteToLog") }

    @Test(enabled = false, // RIDER-105806 Drop the EditorPlugin functionality for Unity versions prior to 2019.2
          description = "Check Unity Log with Unity model refresh")
    fun checkLogWithUnityModelRefresh() = checkLog { executeScript("WriteToLog.cs") }

    private fun checkLog(execute: () -> Unit) {
        withUnityProcess {
            waitFirstScriptCompilation(project)
            waitConnectionToUnityEditor(project)

            val editorLogEntry = waitForEditorLogsAfterAction("#Test#") { execute() }.first()
            executeWithGold(testGoldFile) {
                printEditorLogEntry(it, editorLogEntry)
            }

            checkSweaInSolution()
        }
    }

    // TODO: test reproduce bug only with dialog with info about wrong unity version,
    //  but we can't terminate Unity Editor with UI before connection
    @Test(description = "Check debugger start after attach debugger. RIDER-52498", enabled = false)
    fun checkDebuggerStartsAfterAttachDebugger() {
        try {
            //            startUnity(false, false, false ,true)
            //            waitFirstScriptCompilation(project)
            //            waitConnectionToUnityEditor(project)
            attachDebuggerToUnityEditor(
                {
                    //    replaceUnityVersionOnCurrent(project)
                },
                {
                    waitConnectionToUnityEditor(project)
                }
            )
        }
        finally {
            killUnity(project)
            checkSweaInSolution()
        }
    }
}
