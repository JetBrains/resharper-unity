package com.jetbrains.rider.plugins.unity.run.configurations

import com.jetbrains.rider.run.configurations.unity.UnityAttachConfigurationParameters
import java.nio.file.Path

class UnityAttachConfigurationParametersImpl(override val unityEditorPid: Int?,
                                             override val unityEditorPathByHeuristic: Path?,
                                             override val args: List<String>,
                                             override val unityVersion: String?) : UnityAttachConfigurationParameters {
}