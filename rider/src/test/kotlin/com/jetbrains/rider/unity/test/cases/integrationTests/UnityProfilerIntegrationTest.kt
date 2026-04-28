package com.jetbrains.rider.unity.test.cases.integrationTests

import com.intellij.openapi.fileEditor.FileEditorManager
import com.intellij.openapi.fileEditor.TextEditor
import com.intellij.openapi.rd.util.lifetime
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.plugins.unity.model.ProfilerSnapshotRequest
import com.jetbrains.rider.plugins.unity.model.SelectionState
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ProfilerNavigationRequest
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendProfilerModel
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
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.unity.test.framework.api.frontendBackendModel
import com.jetbrains.rider.unity.test.framework.api.getProfilerToolWindow
import com.jetbrains.rider.unity.test.framework.api.navigateFromGutterMarkToToolWindow
import com.jetbrains.rider.unity.test.framework.api.runProfilerAutomation
import com.jetbrains.rider.unity.test.framework.api.setUpProfilerDefaults
import com.jetbrains.rider.unity.test.framework.api.waitForProfilerGutterMarks
import com.jetbrains.rider.unity.test.framework.api.waitForProfilerSnapshotTimings
import com.jetbrains.rider.unity.test.framework.base.IntegrationTestWithUnityProjectBase
import org.testng.annotations.BeforeMethod
import org.testng.annotations.Test
import java.time.Duration
import java.util.concurrent.atomic.AtomicReference
import kotlin.test.assertEquals
import kotlin.test.assertTrue

@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity Profiler Integration")
@Severity(SeverityLevel.CRITICAL)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@Solution("UnityProfilerTestsProject/SimpleUnityGame")
abstract class UnityProfilerIntegrationTest : IntegrationTestWithUnityProjectBase() {

    @BeforeMethod(dependsOnMethods = ["waitForUnityRunConfigurations"])
    open fun setUpProfilerIntegration() {
        setUpProfilerDefaults()
    }

    @Test(description = "Check profiler timings data streaming from Unity")
    @ChecklistItems(["Profiler/Timings Loading"])
    fun checkProfilerTimingsLoading() {
        // Run profiler automation in Unity (starts play mode, profiles, stops)
        runProfilerAutomation()

        // Wait for profiler record info and timing data to be received in Rider
        waitForProfilerSnapshotTimings()

        // Verify profiler data was received
        val profilerModel = frontendBackendModel.frontendBackendProfilerModel
        val recordInfo = profilerModel.currentProfilerRecordInfo.value
        assertTrue(recordInfo != null, "Profiler record info should not be null")
        assertTrue(recordInfo.lastFrameId > recordInfo.firstFrameId,
            "Record should contain multiple frames: first=${recordInfo.firstFrameId}, last=${recordInfo.lastFrameId}")

        val timings = profilerModel.mainThreadTimingsAndThreads.value
        assertTrue(timings != null, "Profiler timings should not be null")
        assertTrue(timings.samples != null && timings.samples!!.isNotEmpty(), "Profiler timings should contain samples")
        // Verify the snapshot contains real Unity profiler data (frames with actual timing)
        assertTrue(timings.samples!!.any { it.ms > 0f },
            "Profiler timing samples should contain frames with positive duration (actual Unity profiler data)")
    }

    @Test(description = "Check navigation from Unity profiler to source code")
    @ChecklistItems(["Profiler/Navigation from Unity Profiler"])
    fun checkNavigationFromUnityProfiler() {
        // Verify no file is open before profiler automation — proves that navigateByQualifiedName
        // inside runProfilerAutomation() is what opens the file.
        val fem = FileEditorManager.getInstance(project)
        assertTrue(fem.openFiles.none { it.name == "UnoptimizedMonoBehaviour.cs" },
            "UnoptimizedMonoBehaviour.cs should NOT be open before profiler automation")

        // runProfilerAutomation() fires navigateByQualifiedName("UnoptimizedMonoBehaviour.Update")
        // which should open the file and navigate to the Update method.
        runProfilerAutomation()

        // Wait for snapshot
        waitForProfilerSnapshotTimings()
        waitAndPump(Duration.ofSeconds(10), {
            fem.openFiles.any { it.name == "UnoptimizedMonoBehaviour.cs" }
        }) { "UnoptimizedMonoBehaviour.cs should be opened after profiler navigation" }

        // Verify navigation positioned the caret at the correct line (Update() is on line 12)
        waitAndPump(Duration.ofSeconds(10), {
            val textEditor = fem.selectedEditor as? TextEditor
            textEditor?.file?.name == "UnoptimizedMonoBehaviour.cs"
                && textEditor.editor.caretModel.logicalPosition.line + 1 == 12
        }) { "Caret should be at line 12 (Update method) in UnoptimizedMonoBehaviour.cs after profiler navigation" }
    }

    @Test(description = "Check navigation from Unity profiler to exact Profiler.BeginSample call")
    @ChecklistItems(["Profiler/Navigation to BeginSample"])
    fun checkNavigationToBeginSample() {
        // runProfilerAutomation() opens UnoptimizedMonoBehaviour.cs via navigateByQualifiedName
        runProfilerAutomation()
        waitForProfilerSnapshotTimings()

        val fem = FileEditorManager.getInstance(project)
        waitAndPump(Duration.ofSeconds(10), {
            fem.openFiles.any { it.name == "UnoptimizedMonoBehaviour.cs" }
        }) { "UnoptimizedMonoBehaviour.cs should be opened after profiler automation" }

        // Navigate to a specific BeginSample marker within Update()
        val profilerModel = frontendBackendModel.frontendBackendProfilerModel
        profilerModel.navigateByQualifiedName.fire(
            ProfilerNavigationRequest("UnoptimizedMonoBehaviour.Update", "MemoryAndSearch")
        )

        // Profiler.BeginSample("MemoryAndSearch") is on line 21, far from Update() on line 12
        waitAndPump(Duration.ofSeconds(10), {
            val textEditor = fem.selectedEditor as? TextEditor
            textEditor?.file?.name == "UnoptimizedMonoBehaviour.cs"
                && textEditor.editor.caretModel.logicalPosition.line + 1 == 21
        }) { "Caret should be at line 21 (Profiler.BeginSample(\"MemoryAndSearch\")) in UnoptimizedMonoBehaviour.cs" }
    }

    @Test(description = "Check navigation falls back to method when BeginSample marker not found")
    @ChecklistItems(["Profiler/Navigation BeginSample Fallback"])
    fun checkNavigationToBeginSampleFallback() {
        runProfilerAutomation()
        waitForProfilerSnapshotTimings()

        val fem = FileEditorManager.getInstance(project)
        waitAndPump(Duration.ofSeconds(10), {
            fem.openFiles.any { it.name == "UnoptimizedMonoBehaviour.cs" }
        }) { "UnoptimizedMonoBehaviour.cs should be opened after profiler automation" }

        // Navigate with a marker name that does not exist in Update()
        val profilerModel = frontendBackendModel.frontendBackendProfilerModel
        profilerModel.navigateByQualifiedName.fire(
            ProfilerNavigationRequest("UnoptimizedMonoBehaviour.Update", "NonExistentMarker")
        )

        // Should fall back to the Update() method declaration at line 12
        waitAndPump(Duration.ofSeconds(10), {
            val textEditor = fem.selectedEditor as? TextEditor
            textEditor?.file?.name == "UnoptimizedMonoBehaviour.cs"
                && textEditor.editor.caretModel.logicalPosition.line + 1 == 12
        }) { "Caret should fall back to line 12 (Update method) when BeginSample marker not found" }
    }

    @Test(description = "Check navigation to first BeginSample when label is duplicated")
    @ChecklistItems(["Profiler/Navigation to First Duplicate BeginSample"])
    fun checkNavigationToDuplicateBeginSample() {
        runProfilerAutomation()
        waitForProfilerSnapshotTimings()

        val fem = FileEditorManager.getInstance(project)
        waitAndPump(Duration.ofSeconds(10), {
            fem.openFiles.any { it.name == "UnoptimizedMonoBehaviour.cs" }
        }) { "UnoptimizedMonoBehaviour.cs should be opened after profiler automation" }

        // "StringOps" appears twice in Update(): first on line 14, duplicate on line 37.
        // Navigation must land on the first occurrence.
        val profilerModel = frontendBackendModel.frontendBackendProfilerModel
        profilerModel.navigateByQualifiedName.fire(
            ProfilerNavigationRequest("UnoptimizedMonoBehaviour.Update", "StringOps")
        )

        waitAndPump(Duration.ofSeconds(10), {
            val textEditor = fem.selectedEditor as? TextEditor
            textEditor?.file?.name == "UnoptimizedMonoBehaviour.cs"
                && textEditor.editor.caretModel.logicalPosition.line + 1 == 14
        }) { "Caret should be at line 14 (first Profiler.BeginSample(\"StringOps\")) not the duplicate at line 37" }
    }

    @Test(description = "Check navigationWarning fires when target is unresolvable")
    @ChecklistItems(["Profiler/Navigation Warning"])
    fun checkNavigationWarningOnUnresolvableTarget() {
        runProfilerAutomation()
        waitForProfilerSnapshotTimings()

        val profilerModel = frontendBackendModel.frontendBackendProfilerModel
        val receivedWarning = AtomicReference<String?>(null)
        profilerModel.navigationWarning.advise(project.lifetime) { warning ->
            receivedWarning.set(warning)
        }

        // Fire navigation with a completely unresolvable qualified name
        profilerModel.navigateByQualifiedName.fire(
            ProfilerNavigationRequest("NonExistent.ClassName.Method", null)
        )

        waitAndPump(Duration.ofSeconds(10), {
            receivedWarning.get() != null
        }) { "navigationWarning should fire for unresolvable target" }
        assertTrue(receivedWarning.get()!!.contains("NonExistent.ClassName.Method"),
            "Warning message should contain the unresolvable qualified name")
    }

    @Test(description = "Check profiler gutter marks appear in editor")
    @ChecklistItems(["Profiler/Gutter Marks"])
    fun checkProfilerGutterMarks() {
        
        // runProfilerAutomation() also simulates a double-click in the Unity Profiler (fires
        // navigateByQualifiedName), which opens UnoptimizedMonoBehaviour.cs in Rider and
        // initializes UnityProfilerUsagesDaemon with null state before data arrives.
        runProfilerAutomation()

        // Wait for snapshot
        waitForProfilerSnapshotTimings()

        // Verify profiler data is available
        val profilerModel = frontendBackendModel.frontendBackendProfilerModel
        val timings = profilerModel.mainThreadTimingsAndThreads.value
        assertTrue(timings != null, "Profiler timings should not be null")
        assertTrue(timings.samples != null && timings.samples!!.isNotEmpty(),
            "Profiler timings should contain samples")

        // Verify gutter marks setting is enabled
        assertEquals(true, profilerModel.isGutterMarksEnabled.valueOrNull,
            "Gutter marks should be enabled")

        // Verify that gutter marks actually appear in the editor
        assertTrue(waitForProfilerGutterMarks("UnoptimizedMonoBehaviour.cs", Duration.ofSeconds(30)),
            "Profiler gutter marks should appear in UnoptimizedMonoBehaviour.cs")
    }

    @Test(description = "Check frame selection updates profiler data")
    @ChecklistItems(["Profiler/Frame Selection"])
    fun checkFrameSelection() {
        // Run profiler automation
        runProfilerAutomation()

        // Wait for snapshot
        waitForProfilerSnapshotTimings()

        val profilerModel = frontendBackendModel.frontendBackendProfilerModel

        // Verify record info is available with valid frame range
        val recordInfo = profilerModel.currentProfilerRecordInfo.value
        assertTrue(recordInfo != null, "Record info should not be null")
        assertTrue(recordInfo.lastFrameId > recordInfo.firstFrameId,
            "Should have multiple frames: first=${recordInfo.firstFrameId}, last=${recordInfo.lastFrameId}")

        // Verify timings contain data for multiple frames
        val timings = profilerModel.mainThreadTimingsAndThreads.value
        assertTrue(timings != null, "Timings should not be null")
        assertTrue(timings.samples != null && timings.samples!!.size > 1,
            "Timings should contain multiple frame samples")

        // Verify threads information is available
        val threads = requireNotNull(timings.threads) { "Timings should contain thread information" }
        assertTrue(threads.isNotEmpty(), "Timings should contain thread information")

        // Actually select a different frame and verify the snapshot updates
        val targetFrame = recordInfo.firstFrameId + 1
        val mainThread = threads.find { it.name.contains("Main", ignoreCase = true) } ?: threads.first()
        profilerModel.selectionState.set(SelectionState(targetFrame, mainThread))
        // Explicitly fire the snapshot request: in production this is done by UnityProfilerSnapshotModel
        // via selectionState.adviseNotNull → updateUnityProfilerSnapshotData. The daemon IS initialized
        // here (runProfilerAutomation fires navigateByQualifiedName which opens UnoptimizedMonoBehaviour.cs),
        // but we still fire directly to avoid timing races between selectionState.set and the advise handler.
        profilerModel.updateUnityProfilerSnapshotData.fire(ProfilerSnapshotRequest(targetFrame, mainThread))

        // Wait for currentSnapshot to reflect the selected frame's method-level data.
        // mainThreadTimingsAndThreads holds the full frame list and doesn't change on selection.
        waitAndPump(Duration.ofSeconds(30), {
            profilerModel.currentSnapshot.value?.selectionState?.selectedFrameIndex == targetFrame
                && profilerModel.currentSnapshot.value?.samples?.isNotEmpty() == true
        }) { "Profiler snapshot should be updated for frame $targetFrame" }
    }

    @Test(description = "Check profiler tool window availability")
    @ChecklistItems(["Profiler/Tool Window"])
    fun checkProfilerToolWindow() {
        runProfilerAutomation()
        waitForProfilerSnapshotTimings()

        // In headless test mode, ToolWindowHeadlessManagerImpl uses MockToolWindow where
        // show() is a no-op and isVisible() always returns false. Verify the tool window
        // is registered and retrievable — that's the extent of what headless mode supports.
        val toolWindow = getProfilerToolWindow()
        assertTrue(toolWindow != null, "Unity Profiler tool window should be registered")
    }

    @Test(description = "Check navigation from gutter to profiler tool window (filter)")
    @ChecklistItems(["Profiler/Navigation from Gutter to Tool Window"])
    fun checkNavigationFromGutterToToolWindow() {

        // runProfilerAutomation() fires navigateByQualifiedName, opening UnoptimizedMonoBehaviour.cs
        // and initializing UnityProfilerUsagesDaemon with null state before profiler data arrives.
        runProfilerAutomation()

        // Wait for snapshot
        waitForProfilerSnapshotTimings()

        // Verify profiler data is available for the navigation path
        val profilerModel = frontendBackendModel.frontendBackendProfilerModel
        val timings = profilerModel.mainThreadTimingsAndThreads.value
        assertTrue(timings != null && timings.samples != null && timings.samples!!.isNotEmpty(),
            "Profiler timings should contain samples")

        // Verify gutter marks are enabled
        assertEquals(true, profilerModel.isGutterMarksEnabled.valueOrNull,
            "Gutter marks should be enabled")

        // Simulate clicking a gutter mark to navigate to the Profiler Tool Window
        assertTrue(waitForProfilerGutterMarks("UnoptimizedMonoBehaviour.cs", Duration.ofSeconds(30)),
            "Profiler gutter marks should appear before navigating to tool window")

        // Register the tool window before navigation — in headless mode, tool windows from plugin.xml
        // are not auto-registered, so showAndNavigate's internal getToolWindow() would return null.
        val toolWindow = getProfilerToolWindow()
        assertTrue(toolWindow != null, "Unity Profiler tool window should be registered")

        // Find the actual gutter mark renderer and invoke its "Show in Unity Profiler Tool Window"
        // action — same logic as ProfilerLineMarkerPopupFactory's anonymous action (line 97).
        // Using the renderer's sampleInfo.qualifiedName verifies the mark carries the right data.
        val navigatedName = navigateFromGutterMarkToToolWindow("UnoptimizedMonoBehaviour.cs")
        assertTrue(navigatedName != null, "Should have found a gutter mark renderer to navigate from")
    }
}

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@UnityTestSettings(unityVersion = UnityVersion.V2022)
class ProfilerIntegrationUnity2022Test : UnityProfilerIntegrationTest()

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@UnityTestSettings(unityVersion = UnityVersion.V6)
class ProfilerIntegrationUnity60Test : UnityProfilerIntegrationTest()

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@UnityTestSettings(unityVersion = UnityVersion.V6_2)
class ProfilerIntegrationUnity62Test : UnityProfilerIntegrationTest()

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@UnityTestSettings(unityVersion = UnityVersion.V6_3)
class ProfilerIntegrationUnity63Test: UnityProfilerIntegrationTest()
