package com.jetbrains.rider.plugins.unity.settings.agents

import com.jetbrains.rider.plugins.unity.UnityBundle
import org.jetbrains.annotations.Nls

@Nls
internal fun unityCliFailureMessage(failure: UnityCliFailure?, exitCode: Int?): String = when (failure) {
    UnityCliFailure.DOWNLOAD -> UnityBundle.message("unity.agents.cli.failed.download")
    // No exit code, because nothing ran.
    UnityCliFailure.LAUNCH_FAILED -> UnityBundle.message("unity.agents.cli.failed.launch")
    UnityCliFailure.INSTALLER_EXIT_CODE ->
        UnityBundle.message("unity.agents.cli.failed.installer", exitCode ?: -1)
    UnityCliFailure.UPDATE_EXIT_CODE ->
        UnityBundle.message("unity.agents.cli.failed.update", exitCode ?: -1)
    UnityCliFailure.VERIFICATION -> UnityBundle.message("unity.agents.cli.failed.verification")
    null -> UnityBundle.message("unity.agents.cli.failed.unknown")
}
