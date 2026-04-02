package com.jetbrains.rider.unity.test.framework.api

import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.fileEditor.FileEditorManager
import com.intellij.openapi.fileEditor.TextEditor
import com.intellij.openapi.wm.ToolWindow
import com.intellij.openapi.wm.ToolWindowEP
import com.intellij.openapi.wm.ToolWindowManager
import com.intellij.toolWindow.ToolWindowHeadlessManagerImpl
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.plugins.unity.model.RunMethodData
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendProfilerModel
import com.jetbrains.rider.plugins.unity.profiler.lineMarkers.UnityProfilerActiveLineMarkerRenderer
import com.jetbrains.rider.plugins.unity.profiler.toolWindow.UnityProfilerToolWindowFactory
import com.jetbrains.rider.test.facades.solution.SolutionApiFacade
import com.jetbrains.rider.test.scriptingApi.markupAdapter
import com.jetbrains.rider.test.framework.frameworkLogger
import java.time.Duration

fun SolutionApiFacade.runProfilerAutomation() {
    frameworkLogger.info("Executing ProfilerAutomation.RunProfilerTestFromCommandLine in Unity")
    executeMethod(RunMethodData(
        "Assembly-CSharp-Editor",
        "ProfilerAutomation",
        "RunProfilerTestFromCommandLine"
    ))
    // Simulate the user double-clicking "UnoptimizedMonoBehaviour.Update" in the Unity Profiler
    // window, which fires navigateByQualifiedName and opens the file in Rider.
    // This triggers UnityProfilerLineMarkerModelSupport.createHandler(), initializing
    // UnityProfilerUsagesDaemon with null state (data not yet arrived). When profiler data arrives
    // ~10 seconds later, mainThreadTimingsAndThreads.advise fires ONE clean snapshot request.
    frameworkLogger.info("Simulating navigation from Unity Profiler to source (opens UnoptimizedMonoBehaviour.cs)")
    frontendBackendModel.frontendBackendProfilerModel.navigateByQualifiedName.fire("UnoptimizedMonoBehaviour.Update")
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

fun SolutionApiFacade.waitForProfilerIntegrationEnabled(timeout: Duration = Duration.ofSeconds(10)) {
    frameworkLogger.info("Waiting for profiler integration to be enabled")
    val profilerModel = frontendBackendModel.frontendBackendProfilerModel
    waitAndPump(timeout, { profilerModel.isIntegrationEnable.valueOrDefault(false) })
    { "Profiler integration was not enabled within timeout" }
    frameworkLogger.info("Profiler integration enabled")
}

fun SolutionApiFacade.enableProfilerIntegration() {
    frameworkLogger.info("Enabling profiler integration")
    frontendBackendModel.frontendBackendProfilerModel.isIntegrationEnable.set(true)
}

fun SolutionApiFacade.enableProfilerGutterMarks() {
    frameworkLogger.info("Enabling profiler gutter marks")
    frontendBackendModel.frontendBackendProfilerModel.isGutterMarksEnabled.set(true)
}

/**
 * Waits for [UnityProfilerActiveLineMarkerRenderer] highlighters to appear in [fileName].
 * Returns true when marks are found, false on timeout.
 */
fun SolutionApiFacade.waitForProfilerGutterMarks(fileName: String, timeout: Duration = Duration.ofSeconds(10)): Boolean {
    frameworkLogger.info("Waiting for profiler gutter marks in file: $fileName")
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

