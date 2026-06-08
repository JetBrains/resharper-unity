package com.jetbrains.rider.unity.test.framework.api

import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.fileEditor.FileEditorManager
import com.intellij.openapi.fileEditor.TextEditor
import com.intellij.openapi.wm.ToolWindow
import com.intellij.openapi.wm.ToolWindowEP
import com.intellij.openapi.wm.ToolWindowManager
import com.intellij.toolWindow.ToolWindowHeadlessManagerImpl
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rdclient.client.frontendProjectSession
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.inTests.TestHost
import com.jetbrains.rider.plugins.unity.model.ProfilerSnapshotRequest
import com.jetbrains.rider.plugins.unity.model.RunMethodData
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FetchingMode
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ProfilerNavigationRequest
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendProfilerModel
import com.jetbrains.rider.plugins.unity.profiler.lineMarkers.UnityProfilerActiveLineMarkerRenderer
import com.jetbrains.rider.plugins.unity.profiler.toolWindow.UnityProfilerToolWindowFactory
import com.jetbrains.rider.test.facades.solution.SolutionApiFacade
import com.jetbrains.rider.test.scriptingApi.markupAdapter
import com.jetbrains.rider.test.framework.frameworkLogger
import java.time.Duration
import kotlin.math.max

private const val PROFILED_FILE_NAME = "UnoptimizedMonoBehaviour.cs"
private const val PROFILED_METHOD_QUALIFIED_NAME = "UnoptimizedMonoBehaviour.Update"

fun SolutionApiFacade.runProfilerAutomation() {
    frameworkLogger.info("Executing ProfilerAutomation.RunProfilerTestFromCommandLine in Unity")
    executeMethod(RunMethodData(
        "Assembly-CSharp-Editor",
        "ProfilerAutomation",
        "RunProfilerTestFromCommandLine"
    ))

    // executeMethod is synchronous: the profiler run in Unity is finished, but the C# symbol cache the
    // backend navigation relies on (ProfilerNavigationUtils → StackTraceParser) may still be warming up
    // after the ReSharper build. Wait for backend analysis to settle so navigateByQualifiedName can
    // resolve "UnoptimizedMonoBehaviour.Update" on the first fire instead of silently emitting a
    // navigation warning.
    waitForBackendAnalysisFinished()

    // Simulate the user double-clicking "UnoptimizedMonoBehaviour.Update" in the Unity Profiler window
    // (fires navigateByQualifiedName, opening the file in Rider). Opening the file BEFORE the profiler
    // snapshot data arrives is what lets UnityProfilerDaemon attach gutter marks: when the snapshot is
    // processed the backend calls InvalidateDaemon(), which only re-runs analysis on already-open files.
    // A single fire can be dropped if it races the symbol cache, so retry until the file is open.
    openProfiledFileViaNavigation()
}

/**
 * Blocks until backend code analysis is idle, ensuring the C# symbol cache used by profiler navigation
 * is warm. Mirrors the readiness pattern used elsewhere in the Unity tests (e.g. ProjectSettingsCompletionTest).
 */
fun SolutionApiFacade.waitForBackendAnalysisFinished() {
    frameworkLogger.info("Waiting for backend analysis to finish (warming symbol cache for profiler navigation)")
    TestHost.getInstance(project.frontendProjectSession.appSession).backendWaitForCaches("UnityProfilerIntegrationTest")
}

/**
 * Re-fires [ProfilerNavigationRequest] until [condition] holds, retrying on per-attempt timeout.
 *
 * The backend resolves the qualified name through the C# symbol cache; a single fire issued before the
 * cache is warm (or before the navigated file's daemon is ready) is silently dropped — it returns a
 * navigation warning instead of navigating. Re-firing makes the navigation-dependent tests robust to
 * symbol-cache / daemon readiness timing on CI.
 */
private fun SolutionApiFacade.fireNavigationUntil(
    qualifiedName: String,
    markerName: String?,
    description: String,
    totalTimeout: Duration,
    attemptTimeout: Duration,
    condition: () -> Boolean,
) {
    val profilerModel = frontendBackendModel.frontendBackendProfilerModel
    val attempts = max(1, (totalTimeout.toMillis() / attemptTimeout.toMillis()).toInt())
    for (attempt in 1..attempts) {
        frameworkLogger.info("Profiler navigation attempt $attempt/$attempts: $description")
        profilerModel.navigateByQualifiedName.fire(ProfilerNavigationRequest(qualifiedName, markerName))
        try {
            waitAndPump(attemptTimeout, condition) { "profiler navigation condition not satisfied yet" }
            frameworkLogger.info("Profiler navigation satisfied on attempt $attempt: $description")
            return
        } catch (e: Exception) {
            frameworkLogger.warn("Profiler navigation attempt $attempt/$attempts did not satisfy [$description], re-firing")
        }
    }
    error("Profiler navigation did not satisfy [$description] after $attempts attempts")
}

/**
 * Reliably opens [fileName] by simulating navigation from the Unity Profiler to
 * "UnoptimizedMonoBehaviour.Update", retrying until the file is open.
 */
fun SolutionApiFacade.openProfiledFileViaNavigation(
    fileName: String = PROFILED_FILE_NAME,
    totalTimeout: Duration = Duration.ofSeconds(60),
    attemptTimeout: Duration = Duration.ofSeconds(10),
) {
    val fem = FileEditorManager.getInstance(project)
    fireNavigationUntil(PROFILED_METHOD_QUALIFIED_NAME, null, "open $fileName", totalTimeout, attemptTimeout) {
        fem.openFiles.any { it.name == fileName }
    }
}

/**
 * Fires a profiler navigation request and waits until [fileName] is the selected editor with the caret on
 * [expectedLine] (1-based), retrying the fire on each attempt. Use this in navigation tests instead of a
 * one-shot fire + wait, which is dropped when it races the backend symbol cache.
 */
fun SolutionApiFacade.navigateAndAwaitCaret(
    qualifiedName: String,
    markerName: String?,
    fileName: String,
    expectedLine: Int,
    totalTimeout: Duration = Duration.ofSeconds(60),
    attemptTimeout: Duration = Duration.ofSeconds(10),
) {
    val fem = FileEditorManager.getInstance(project)
    fireNavigationUntil(
        qualifiedName, markerName,
        "$qualifiedName (marker=$markerName) → $fileName:$expectedLine",
        totalTimeout, attemptTimeout
    ) {
        val textEditor = fem.selectedEditor as? TextEditor
        textEditor?.file?.name == fileName
            && textEditor.editor.caretModel.logicalPosition.line + 1 == expectedLine
    }
}

/**
 * Re-fires the profiler snapshot request for the current selection. This drives the backend
 * ProcessSnapshotAsync → InvalidateDaemon() path, which re-runs [UnityProfilerDaemon] on the open editor
 * and (re)emits the gutter highlighters. Used to deterministically trigger gutter marks once the file is
 * open and a snapshot is available, instead of relying on the file happening to be open at the exact
 * moment the auto-selected snapshot was first processed.
 */
fun SolutionApiFacade.refreshProfilerSnapshotForCurrentSelection() {
    val profilerModel = frontendBackendModel.frontendBackendProfilerModel
    val selection = profilerModel.selectionState.value
    if (selection == null) {
        frameworkLogger.warn("No profiler selection state available; cannot refresh snapshot to re-trigger gutter daemon")
        return
    }
    frameworkLogger.info("Re-firing profiler snapshot request for frame ${selection.selectedFrameIndex} to re-trigger gutter daemon")
    profilerModel.updateUnityProfilerSnapshotData.fire(
        ProfilerSnapshotRequest(selection.selectedFrameIndex, selection.selectedThread)
    )
}

/**
 * Waits for the full profiler data chain to complete:
 * 1. Record info (first frame id / last frame id)
 * 2. Frame timings overview (`mainThreadTimingsAndThreads`)
 * 3. Current snapshot (method-level data for the auto-selected frame)
 *
 * Steps 1 and 2 arrive from Unity; step 3 follows asynchronously once the auto-selection chain
 * fires: `mainThreadTimingsAndThreads` → auto-select first frame → `updateUnityProfilerSnapshotData`
 * → C# responds → `frontendBackendProfilerModel.currentSnapshot` set. Each step waits up to
 * [timeout]; worst-case total is 3 × [timeout].
 */
fun SolutionApiFacade.waitForProfilerSnapshotTimings(timeout: Duration = Duration.ofSeconds(60)) {
    val profilerModel = frontendBackendModel.frontendBackendProfilerModel
    frameworkLogger.info("Waiting for profiler record info to be available")
    waitAndPump(timeout, {
        profilerModel.currentProfilerRecordInfo.value != null
    }) { "Profiler record info was not available within timeout" }
    frameworkLogger.info("Waiting for profiler snapshot timings to be loaded")
    waitAndPump(timeout, {
        val value = profilerModel.mainThreadTimingsAndThreads.value
        value != null && value.samples != null && value.samples!!.isNotEmpty()
    }) { "Profiler snapshot timings were not loaded within timeout" }
    frameworkLogger.info("Waiting for profiler current snapshot to be available")
    waitAndPump(timeout, {
        val snapshot = profilerModel.currentSnapshot.value
        snapshot != null && snapshot.samples.isNotEmpty()
    }) { "Profiler current snapshot was not available within timeout" }
    frameworkLogger.info("Profiler data loaded successfully")
}

fun SolutionApiFacade.setUpProfilerDefaults(timeout: Duration = Duration.ofSeconds(10)) {
    frameworkLogger.info("Setting up profiler defaults")
    val profilerModel = frontendBackendModel.frontendBackendProfilerModel
    
    profilerModel.isIntegrationEnable.set(true)
    profilerModel.fetchingMode.set(FetchingMode.Auto)
    profilerModel.isGutterMarksEnabled.set(true)
    
    waitAndPump(timeout, {
        profilerModel.isIntegrationEnable.valueOrDefault(false) 
            && profilerModel.fetchingMode.valueOrDefault(FetchingMode.Auto) == FetchingMode.Auto 
            && profilerModel.isGutterMarksEnabled.valueOrDefault(false)
    })
    { "Profiler integration was not enabled within timeout" }
    frameworkLogger.info("Profiler defaults configured")
}

/**
 * Waits for [UnityProfilerActiveLineMarkerRenderer] highlighters to appear in [fileName].
 * Returns true when marks are found, false on timeout.
 */
fun SolutionApiFacade.waitForProfilerGutterMarks(fileName: String, timeout: Duration = Duration.ofSeconds(10)): Boolean {
    frameworkLogger.info("Waiting for profiler gutter marks in file: $fileName")
    // Deterministically re-trigger a daemon pass while the file is open: re-firing the snapshot request
    // drives ProcessSnapshotAsync → InvalidateDaemon() on the backend, which re-runs UnityProfilerDaemon
    // on the open editor and emits the gutter highlighters. Without this, marks only appear if the file
    // happened to be open at the moment the auto-selected snapshot was first processed — the race behind
    // the flaky "gutter marks should appear" failures.
    refreshProfilerSnapshotForCurrentSelection()
    try {
        waitAndPump(timeout, {
            FileEditorManager.getInstance(project).allEditors
                .filterIsInstance<TextEditor>()
                .filter { it.file?.name == fileName }
                .any { textEditor ->
                    textEditor.editor.markupAdapter.allHighlighters.any { h ->
                        h.lineMarkerRenderer is UnityProfilerActiveLineMarkerRenderer
                    }
                }
        }) { "Profiler gutter marks did not appear in $fileName" }
        frameworkLogger.info("Profiler gutter marks found in $fileName")
        return true
    } catch (e: Exception) {
        frameworkLogger.warn("Failed waiting for profiler gutter marks in $fileName: ${e.message}")
        return false
    }
}

/**
 * Returns the Unity Profiler tool window, registering it first if needed.
 *
 * In headless test environments, tool windows declared in plugin.xml are not
 * auto-registered. This follows the same pattern as [com.jetbrains.rider.test.framework.AssemblyExplorerTestUtil].
 */
fun SolutionApiFacade.getProfilerToolWindow(): ToolWindow? {
    val toolWindowManager = ToolWindowManager.getInstance(project)
    var toolWindow = toolWindowManager.getToolWindow(UnityProfilerToolWindowFactory.TOOLWINDOW_ID)
    if (toolWindow == null && toolWindowManager is ToolWindowHeadlessManagerImpl) {
        for (bean in ToolWindowEP.EP_NAME.extensionList) {
            if (bean.id == UnityProfilerToolWindowFactory.TOOLWINDOW_ID) {
                toolWindow = toolWindowManager.doRegisterToolWindow(bean.id)
                frameworkLogger.info("Registered Unity Profiler tool window in headless test environment")
                break
            }
        }
    }
    return toolWindow
}

/**
 * Simulates the "Show in Unity Profiler Tool Window" action from a gutter mark popup.
 *
 * Finds the first [UnityProfilerActiveLineMarkerRenderer] in the open editor for [fileName],
 * reads the qualified name from the renderer's `sampleInfo` (the same data the real popup uses),
 * then calls [UnityProfilerToolWindowFactory.showAndNavigate] — identical to the anonymous action
 * in [com.jetbrains.rider.plugins.unity.profiler.lineMarkers.ProfilerLineMarkerPopupFactory].
 *
 * Returns the qualified name navigated to, or null if no gutter mark renderer was found.
 */
fun SolutionApiFacade.navigateFromGutterMarkToToolWindow(fileName: String): String? {
    for (textEditor in FileEditorManager.getInstance(project).allEditors
        .filterIsInstance<TextEditor>()
        .filter { it.file?.name == fileName }) {
        val renderer = textEditor.editor.markupAdapter.allHighlighters
            .firstNotNullOfOrNull { it.lineMarkerRenderer as? UnityProfilerActiveLineMarkerRenderer }
            ?: continue
        val qualifiedName = renderer.sampleInfo.qualifiedName
        frameworkLogger.info("Navigating from gutter mark '$qualifiedName' to Unity Profiler tool window")
        ApplicationManager.getApplication().invokeAndWait {
            UnityProfilerToolWindowFactory.showAndNavigate(project, qualifiedName)
        }
        return qualifiedName
    }
    frameworkLogger.info("No profiler gutter mark renderer found in $fileName")
    return null
}

