package com.jetbrains.rider.unity.test.cases

import com.intellij.ide.projectView.ProjectView
import com.intellij.ide.projectView.impl.ProjectViewImpl
import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.project.Project
import com.intellij.openapi.rd.util.lifetime
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.openapi.wm.ToolWindowId
import com.intellij.openapi.wm.ex.ToolWindowManagerListener
import com.intellij.testFramework.ProjectViewTestUtil
import com.intellij.toolWindow.ToolWindowHeadlessManagerImpl
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.plugins.unity.UnityProjectDiscoverer
import com.jetbrains.rider.plugins.unity.explorer.UnityExplorer
import com.jetbrains.rider.plugins.unity.isUnityProject
import com.jetbrains.rider.plugins.unity.isUnityProjectFolder
import com.jetbrains.rider.projectView.views.fileSystemExplorer.FileSystemExplorerPane
import com.jetbrains.rider.test.OpenSolutionParams
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.annotations.report.ChecklistItems
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.annotations.report.Issue
import com.jetbrains.rider.test.annotations.report.Severity
import com.jetbrains.rider.test.annotations.report.SeverityLevel
import com.jetbrains.rider.test.base.PerTestSettingsTestBase
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.facades.solution.RiderSolutionApiFacade
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.scriptingApi.prepareProjectView
import com.jetbrains.rider.test.scriptingApi.withSolution
import org.testng.Assert.assertNull
import org.testng.Assert.assertTrue
import org.testng.annotations.Test
import java.time.Duration
import kotlin.test.assertNotNull

private const val SOLUTION = "UnityProjectDiscovererTestData"

@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity Project Discovery")
@Severity(SeverityLevel.CRITICAL)
@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
@Issue("RIDER-134819")
class UnityProjectDiscovererTest : PerTestSettingsTestBase() {

    // Panes loaded but PROJECT_VIEW tool window not initialised — reproduces the RIDER-134819 race.
    private fun facadeWithPaneButNoToolWindow() = object : RiderSolutionApiFacade() {
        override suspend fun openSolutionOrProject(
            projectToClose: Project?, solutionOrProjectFile: VirtualFile, params: OpenSolutionParams
        ): Project? {
            val project = super.openSolutionOrProject(projectToClose, solutionOrProjectFile, params)
            if (project != null) {
                val projectView = ProjectView.getInstance(project) as ProjectViewImpl
                projectView.addProjectPane(FileSystemExplorerPane(project))
            }
            return project
        }
    }

    // Full ProjectView setup — discoverer can call changeView immediately on the fast path.
    private fun facadeWithFullProjectView() = object : RiderSolutionApiFacade() {
        override suspend fun openSolutionOrProject(
            projectToClose: Project?, solutionOrProjectFile: VirtualFile, params: OpenSolutionParams
        ): Project? {
            val project = super.openSolutionOrProject(projectToClose, solutionOrProjectFile, params)
            if (project != null) {
                ApplicationManager.getApplication().invokeAndWait { prepareProjectView(project) }
            }
            return project
        }
    }

    @Test(description = "changeView should not NPE when ProjectView setupImpl hasn't been called")
    @ChecklistItems(["Unity project detection / first open with missing tool window"])
    fun testChangeViewDoesNotNpeWhenToolWindowMissing() {
        withSolution(SOLUTION, facadeWithPaneButNoToolWindow(), OpenSolutionParams(),
            testWorkDirectory, solutionSourceRootDirectory) {
            assertTrue(project.isUnityProject.value, "Project should be detected as Unity")
            assertTrue(project.isUnityProjectFolder.value, "Project folder should be detected as Unity")
        }
    }

    @Test(description = "changeView switches to UnityExplorer on first open")
    @ChecklistItems(["Unity project detection / first open switches to UnityExplorer"])
    fun testChangeViewSwitchesToUnityExplorerOnFirstOpen() {
        withSolution(SOLUTION, facadeWithFullProjectView(), OpenSolutionParams(),
            testWorkDirectory, solutionSourceRootDirectory) {
            assertTrue(project.isUnityProject.value, "Project should be detected as Unity")
            assertNotNull(ProjectView.getInstance(project).getProjectViewPaneById(UnityExplorer.ID),
                "UnityExplorer pane should be registered after first open")
        }
    }

    @Test(description = "deferred path adds UnityExplorer pane when no panes are loaded on startup")
    @ChecklistItems(["Unity project detection / deferred pane addition when ProjectView not initialized"])
    fun testDeferredPathAddsPaneWhenProjectViewNotInitialized() {
        UnityProjectDiscoverer.isSupportedInHeadlessEnv = true
        try {
            withSolution(SOLUTION, RiderSolutionApiFacade(), OpenSolutionParams(),
                testWorkDirectory, solutionSourceRootDirectory) {
                assertTrue(project.isUnityProject.value, "Project should be detected as Unity")

                val projectView = ProjectView.getInstance(project)
                assertNull(projectView.getProjectViewPaneById(UnityExplorer.ID),
                    "UnityExplorer pane should not be registered before tool window is shown")

                ProjectViewTestUtil.setupImpl(project, true)

                val toolWindow = object : ToolWindowHeadlessManagerImpl.MockToolWindow(project) {
                    override fun getId(): String = ToolWindowId.PROJECT_VIEW
                }
                project.messageBus.syncPublisher(ToolWindowManagerListener.TOPIC).toolWindowShown(toolWindow)

                waitAndPump(project.lifetime,
                    { projectView.currentProjectViewPane?.id == UnityExplorer.ID },
                    Duration.ofSeconds(5)
                ) { "Active pane should be UnityExplorer after deferred view switch, but was: ${projectView.currentProjectViewPane?.id}" }
            }
        } finally {
            UnityProjectDiscoverer.isSupportedInHeadlessEnv = false
        }
    }

    @Test(description = "deferred view switch resumes when tool window becomes available")
    @ChecklistItems(["Unity project detection / deferred view switch on toolWindowShown"])
    fun testDeferredViewSwitchWaitsForToolWindow() {
        UnityProjectDiscoverer.isSupportedInHeadlessEnv = true
        try {
            withSolution(SOLUTION, facadeWithPaneButNoToolWindow(), OpenSolutionParams(),
                testWorkDirectory, solutionSourceRootDirectory) {
                assertTrue(project.isUnityProject.value, "Project should be detected as Unity")

                val projectView = ProjectView.getInstance(project)
                projectView.getProjectViewPaneById(FileSystemExplorerPane.ID)?.let { projectView.removeProjectPane(it) }
                projectView.getProjectViewPaneById(UnityExplorer.ID)?.let { projectView.removeProjectPane(it) }

                ProjectViewTestUtil.setupImpl(project, true)

                projectView.changeView(FileSystemExplorerPane.ID)
                assertTrue(projectView.currentProjectViewPane?.id != UnityExplorer.ID,
                    "UnityExplorer should not be active before toolWindowShown fires")

                val toolWindow = object : ToolWindowHeadlessManagerImpl.MockToolWindow(project) {
                    override fun getId(): String = ToolWindowId.PROJECT_VIEW
                }
                project.messageBus.syncPublisher(ToolWindowManagerListener.TOPIC).toolWindowShown(toolWindow)

                waitAndPump(project.lifetime,
                    { projectView.currentProjectViewPane?.id == UnityExplorer.ID },
                    Duration.ofSeconds(5)
                ) { "Active pane should be UnityExplorer after deferred view switch, but was: ${projectView.currentProjectViewPane?.id}" }
            }
        } finally {
            UnityProjectDiscoverer.isSupportedInHeadlessEnv = false
        }
    }
}
