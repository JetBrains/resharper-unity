package com.jetbrains.rider.plugins.unity.ui.vcs

import com.intellij.ide.BrowserUtil
import com.intellij.openapi.diagnostic.thisLogger
import com.intellij.openapi.project.DumbAware
import com.intellij.openapi.project.Project
import com.intellij.openapi.vcs.CheckinProjectPanel
import com.intellij.openapi.vcs.changes.CommitContext
import com.intellij.openapi.vcs.changes.ui.BooleanCommitOption.Companion.create
import com.intellij.openapi.vcs.checkin.CheckinHandler
import com.intellij.openapi.vcs.checkin.CheckinHandlerFactory
import com.intellij.openapi.vcs.checkin.CommitCheck
import com.intellij.openapi.vcs.checkin.CommitInfo
import com.intellij.openapi.vcs.checkin.CommitProblem
import com.intellij.openapi.vcs.checkin.CommitProblemWithDetails
import com.intellij.openapi.vcs.ui.RefreshableOnComponent
import com.jetbrains.rd.framework.impl.RpcTimeouts
import com.jetbrains.rider.plugins.unity.isUnityProject
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle
import com.jetbrains.rider.projectView.solution
import java.util.concurrent.CancellationException

/**
 * Checks if there are unsaved scenes in Unity.
 */
class UnsavedSceneCheckinHandlerFactory : CheckinHandlerFactory() {
    override fun createHandler(panel: CheckinProjectPanel, commitContext: CommitContext): CheckinHandler =
        UnresolvedMergeCheckHandler(panel.project)
}

private class UnresolvedMergeCheckHandler(private val project: Project) : CheckinHandler(), CommitCheck, DumbAware {
    override fun getExecutionOrder(): CommitCheck.ExecutionOrder = CommitCheck.ExecutionOrder.LATE
    private val settings = UnsavedCheckinState.getService(project)

    override fun isEnabled(): Boolean {
        return settings.checkUnsavedState && project.isUnityProject.value
    }

    override suspend fun runCheck(commitInfo: CommitInfo): CommitProblem? {
        if (settings.checkUnsavedState && project.isUnityProject.value) {
            var providerResult = false
            try {
                providerResult = project.solution.frontendBackendModel.hasUnsavedState
                    .sync(Unit, RpcTimeouts(200L, 200L))
            }
            catch (t: CancellationException) {
                thisLogger().info("Unable to fetch hasUnsavedScenes", t)
            }
            catch (t: Throwable) {
                thisLogger().warn("Unable to fetch hasUnsavedScenes", t)
            }

            if (providerResult) return UnsavedSceneCommitProblem(UnityUIBundle.message("dialog.unsaved.message.changes.in.unity.state.will.not.be.included.in.commit"))
        }
        return null
    }

    override fun getBeforeCheckinConfigurationPanel(): RefreshableOnComponent? {
        if (!project.isUnityProject.value)
            return null
        return create(project, this, false, UnityUIBundle.message("commitOption.check.unsaved.unity.state"),
                      settings::checkUnsavedState)
    }

    class UnsavedSceneCommitProblem(message: String) : CommitProblemWithDetails {
        override val showDetailsAction: String = UnityUIBundle.message("unity.before.check.in.show.details")

        override val text: String = message

        override fun showDetails(project: Project) {
            BrowserUtil.browse(UNSAVED_SCENE_HELP_LINK)
        }
    }

    companion object{
        private const val UNSAVED_SCENE_HELP_LINK = "https://github.com/JetBrains/resharper-unity/wiki/Pre%E2%80%90commit-checks-for-Unity#unsaved-changes-check"
    }
}
