package com.jetbrains.rider.unity.test.cases.integrationTests

import com.intellij.openapi.rd.util.lifetime
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.projectView.solutionDirectory
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.annotations.UnityTestSettings
import com.jetbrains.rider.test.annotations.report.ChecklistItems
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.annotations.report.Severity
import com.jetbrains.rider.test.annotations.report.SeverityLevel
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.enums.UnityVersion
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.scriptingApi.absoluteCanonicalPath
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import com.jetbrains.rider.unity.test.framework.api.executeIntegrationTestMethod
import com.jetbrains.rider.unity.test.framework.api.executeScript
import com.jetbrains.rider.unity.test.framework.api.getRiderDevAppPath
import com.jetbrains.rider.unity.test.framework.api.printEditorLogEntry
import com.jetbrains.rider.unity.test.framework.api.unityDefaultTimeout
import com.jetbrains.rider.unity.test.framework.api.waitConnectionToUnityEditor
import com.jetbrains.rider.unity.test.framework.api.waitForEditorLogsAfterAction
import com.jetbrains.rider.unity.test.framework.base.IntegrationTestWithUnityProjectBase
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test

@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Connection with Unity Editor")
@Severity(SeverityLevel.CRITICAL)
@Solution("UnityDebugAndUnitTesting/Project")
@Tag(TeamCityTags.Plugins.Unity.Integration)
abstract class ConnectionTest : IntegrationTestWithUnityProjectBase() {
    @Test // Check connection with Unity Editor for a project with the Rider package
    @ChecklistItems(["Connection with Editor/Connection to Unity Editor"])
    fun checkConnectionToUnityEditor() {
        waitConnectionToUnityEditor(project)
    }

    @Test // Check external Editor in Unity settings
    @ChecklistItems(["Connection with Editor/External Editor setting"])
    fun checkExternalEditorWithExecutingMethod() = checkExternalEditor {
        executeIntegrationTestMethod("DumpExternalEditor", EDITOR_ASSEMBLY_NAME)
    }

    @Test // Check external Editor in Unity with Unity model refresh
    @ChecklistItems(["Connection with Editor/External Editor setting"])
    fun checkExternalEditorWithUnityModelRefresh() = checkExternalEditor { executeScript("DumpExternalEditor.cs") }

    private fun checkExternalEditor(execute: () -> Unit) {
        val externalEditorPath = project.solutionDirectory.resolve("Assets/ExternalEditor.txt")

        execute()
        waitAndPump(project.lifetime, { externalEditorPath.exists() }, unityDefaultTimeout)
        { "ExternalEditor.txt is not created" }
        waitAndPump(project.lifetime, { externalEditorPath.readText().isNotEmpty() }, unityDefaultTimeout)
        { "ExternalEditor.txt is empty" }

        executeWithGold(testGoldFile) {
            it.print(externalEditorPath.readText().replace(getRiderDevAppPath().absoluteCanonicalPath, "<riderPath>"))
        }
    }

    @Test // Check Unity Log
    @ChecklistItems(["Connection with Editor/Unity Editor log"])
    fun checkLogWithExecutingMethod() = checkLog { executeIntegrationTestMethod("WriteToLog", EDITOR_ASSEMBLY_NAME) }

    @Test // Check Unity Log with Unity model refresh
    @ChecklistItems(["Connection with Editor/Unity Editor log"])
    fun checkLogWithUnityModelRefresh() = checkLog { executeScript("WriteToLog.cs") }

    private fun checkLog(execute: () -> Unit) {
        val editorLogEntry = waitForEditorLogsAfterAction("#Test#") { execute() }.first()
        executeWithGold(testGoldFile) {
            printEditorLogEntry(it, editorLogEntry)
        }
    }

    // PlayModeTest and DebuggerTest already check that the debugger attaches to the Unity Editor
    // @Test // Check debugger start after attach debugger. RIDER-52498
    // fun checkDebuggerStartsAfterAttachDebugger() {
    //     attachDebuggerToUnityEditor({}, { waitConnectionToUnityEditor(project) })
    // }

    companion object {
        /** The assembly of `Assets/Editor`, which `Editor.asmdef` defines in the test project. */
        private const val EDITOR_ASSEMBLY_NAME = "Editor"
    }
}

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@UnityTestSettings(unityVersion = UnityVersion.V2022)
class ConnectionTestUnity2022 : ConnectionTest()

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@UnityTestSettings(unityVersion = UnityVersion.V6_3)
class ConnectionTestUnity6_3 : ConnectionTest()

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@UnityTestSettings(unityVersion = UnityVersion.V6_5)
class ConnectionTestUnity6_5 : ConnectionTest()
