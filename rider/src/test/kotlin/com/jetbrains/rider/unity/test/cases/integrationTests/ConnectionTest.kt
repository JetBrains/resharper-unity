package com.jetbrains.rider.unity.test.cases.integrationTests

import com.intellij.openapi.rd.util.lifetime
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.projectView.solutionDirectory
import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.annotations.report.Severity
import com.jetbrains.rider.test.annotations.report.SeverityLevel
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import com.jetbrains.rider.test.scriptingApi.waitFirstScriptCompilation
import com.jetbrains.rider.unity.test.framework.api.attachDebuggerToUnityEditor
import com.jetbrains.rider.unity.test.framework.api.checkSweaInSolution
import com.jetbrains.rider.unity.test.framework.api.executeIntegrationTestMethod
import com.jetbrains.rider.unity.test.framework.api.executeScript
import com.jetbrains.rider.unity.test.framework.api.killUnity
import com.jetbrains.rider.unity.test.framework.api.printEditorLogEntry
import com.jetbrains.rider.unity.test.framework.api.unityDefaultTimeout
import com.jetbrains.rider.unity.test.framework.api.waitConnectionToUnityEditor
import com.jetbrains.rider.unity.test.framework.api.waitForEditorLogsAfterAction
import com.jetbrains.rider.unity.test.framework.api.withUnityProcess
import com.jetbrains.rider.unity.test.framework.base.IntegrationTestWithSolutionBase
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test

@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Connection with Unity Editor")
@Severity(SeverityLevel.CRITICAL)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL]) // todo: allow Linux
@Solution("SimpleUnityProjectWithoutPlugin")
@Tag(TeamCityTags.Plugins.Unity.Integration)
class ConnectionTest : IntegrationTestWithSolutionBase() {
    @Test // Check connection with Unity after Unity start with Simple Unity Project without plugin
    @Mute("RIDER-105806 Drop the EditorPlugin functionality for Unity versions prior to 2019.2")
    fun installAndCheckConnectionAfterUnityStart() {
        withUnityProcess {
            waitFirstScriptCompilation(project)
            waitConnectionToUnityEditor(project)
            checkSweaInSolution()
        }
    }

    @Test // Check connection with Unity before Unity start with Simple Unity Project without plugin
    @Mute("RIDER-105806 Drop the EditorPlugin functionality for Unity versions prior to 2019.2")
    fun installAndCheckConnectionBeforeUnityStart() {
        withUnityProcess {
            waitFirstScriptCompilation(project)
            waitConnectionToUnityEditor(project)
            checkSweaInSolution()
        }
    }

    @Test // Check external Editor in Unity settings
    @Mute("RIDER-105806 Drop the EditorPlugin functionality for Unity versions prior to 2019.2")
    fun checkExternalEditorWithExecutingMethod() = checkExternalEditor(false) {
        executeIntegrationTestMethod("DumpExternalEditor")
    }

    @Test // Check external Editor in Unity with Unity model refresh
    @Mute
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

    @Test // Check Unity Log
    @Mute("RIDER-105806 Drop the EditorPlugin functionality for Unity versions prior to 2019.2")
    fun checkLogWithExecutingMethod() = checkLog { executeIntegrationTestMethod("WriteToLog") }

    @Test // Check Unity Log with Unity model refresh
    @Mute("RIDER-105806 Drop the EditorPlugin functionality for Unity versions prior to 2019.2")
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
    @Test // Check debugger start after attach debugger. RIDER-52498
    @Mute
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
