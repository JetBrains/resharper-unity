package com.jetbrains.rider.plugins.unity.run

import com.intellij.execution.RunManager
import com.intellij.execution.configurations.ConfigurationTypeUtil
import com.intellij.execution.configurations.UnknownConfigurationType
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.jetbrains.rd.ide.model.Solution
import com.jetbrains.rd.platform.util.idea.LifetimedService
import com.jetbrains.rd.protocol.ProtocolExtListener
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.adviseNotNull
import com.jetbrains.rd.util.reactive.whenTrue
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.isUnityProject
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.run.configurations.*
import com.jetbrains.rider.plugins.unity.run.configurations.unityExe.UnityExeConfiguration
import com.jetbrains.rider.plugins.unity.run.configurations.unityExe.UnityExeConfigurationFactory
import com.jetbrains.rider.plugins.unity.run.configurations.unityExe.UnityExeConfigurationType
import com.jetbrains.rider.plugins.unity.util.*
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.projectView.solutionDirectory
import java.io.File
import java.nio.file.Paths

class DefaultRunConfigurationGenerator : LifetimedService() {

    companion object {
        val ATTACH_CONFIGURATION_NAME = UnityBundle.message("attach.to.unity.editor")
        val ATTACH_AND_PLAY_CONFIGURATION_NAME = UnityBundle.message("attach.to.unity.editor.and.play")
        val RUN_DEBUG_STANDALONE_CONFIGURATION_NAME = UnityBundle.message("standalone.player")
        val RUN_DEBUG_BATCH_MODE_UNITTESTS_CONFIGURATION_NAME = UnityBundle.message("unit.tests.batch.mode")
        val RUN_DEBUG_START_UNITY_CONFIGURATION_NAME = UnityBundle.message("start.unity")
    }

    class ProtocolListener : ProtocolExtListener<Solution, FrontendBackendModel> {
        private val logger = Logger.getInstance(DefaultRunConfigurationGenerator::class.java)

        override fun extensionCreated(lifetime: Lifetime, project: Project, parent: Solution, model: FrontendBackendModel) {
            model.hasUnityReference.whenTrue(lifetime) { lt ->
                val runManager = RunManager.getInstance(project)
                // Clean up the renamed "attach and play" configuration from 2018.2 EAP1-3
                // (Was changed from a separate configuration type to just another factory under "Attach to Unity")
                removeRunConfigurations(project) {
                    it.type is UnknownConfigurationType && it.name == ATTACH_AND_PLAY_CONFIGURATION_NAME
                }

                val previouslySelectedConfig = RunManager.getInstance(project).selectedConfiguration

                // Remove any additional "Attach to Unity Editor" configurations. The user can no longer create them
                // (the configuration type is marked as VirtualConfigurationType), but there might be old instances,
                // or renamed instances, or accidentally duplicated instances. Furthermore, they are not
                // user-configurable, so we only ever need two configurations. This also cleans up any leftover English
                // language configs when installing a language pack
                removeRunConfigurations(project) {
                    it.type is UnityEditorDebugConfigurationType && it.name != ATTACH_CONFIGURATION_NAME && it.name != ATTACH_AND_PLAY_CONFIGURATION_NAME
                }

                // Add "Attach Unity Editor" configurations, if they don't exist
                if (!runManager.allSettings.any { it.type is UnityEditorDebugConfigurationType && it.factory is UnityAttachToEditorFactory && it.name == ATTACH_CONFIGURATION_NAME }) {
                    val newConfig = createAttachToUnityEditorConfiguration(project, ATTACH_CONFIGURATION_NAME, false)

                    // If we deleted an "Attach to Unity Editor" config which was selected, select the new one
                    (previouslySelectedConfig?.configuration as? UnityAttachToEditorRunConfiguration)?.let {
                        if (!it.play) {
                            RunManager.getInstance(project).selectedConfiguration = newConfig
                        }
                    }
                }

                if (project.isUnityProject() && !runManager.allSettings.any { it.type is UnityEditorDebugConfigurationType && it.factory is UnityAttachToEditorAndPlayFactory && it.name == ATTACH_CONFIGURATION_NAME }) {
                    createAttachToUnityEditorConfiguration(project, ATTACH_AND_PLAY_CONFIGURATION_NAME, true)
                }

                project.solution.frontendBackendModel.unityApplicationData.adviseNotNull(lt) {
                    val exePath = UnityInstallationFinder.getOsSpecificPath(Paths.get(it.applicationPath))
                    if (exePath.toFile().isFile) {
                        createOrUpdateUnityExeRunConfiguration(
                            RUN_DEBUG_START_UNITY_CONFIGURATION_NAME,
                            exePath.toFile().canonicalPath,
                            project.solutionDirectory.canonicalPath,
                            mutableListOf<String>().withProjectPath(project).withDebugCodeOptimization().toProgramParameters(),
                            runManager)

                        createOrUpdateUnityExeRunConfiguration(
                            RUN_DEBUG_BATCH_MODE_UNITTESTS_CONFIGURATION_NAME,
                            exePath.toFile().canonicalPath,
                            project.solutionDirectory.canonicalPath,
                            mutableListOf<String>().withRunTests().withBatchMode()
                                .withProjectPath(project).withTestResults()
                                .withTestPlatform().withDebugCodeOptimization().toProgramParameters(),
                            runManager)
                    } else
                        logger.trace("exePath: $exePath is not a file.")
                }

                project.solution.frontendBackendModel.unityProjectSettings.buildLocation.adviseNotNull(lt) {
                    createOrUpdateUnityExeRunConfiguration(
                        RUN_DEBUG_STANDALONE_CONFIGURATION_NAME,
                        it,
                        File(it).parent!!,
                        mutableListOf<String>().toProgramParameters(),
                        runManager
                    )
                }

                // make Attach Unity Editor configuration selected if nothing is selected
                if (runManager.selectedConfiguration == null) {
                    val runConfiguration = runManager.findConfigurationByName(ATTACH_CONFIGURATION_NAME)
                    if (runConfiguration != null) {
                        runManager.selectedConfiguration = runConfiguration
                    }
                }
            }
        }

        private fun createOrUpdateUnityExeRunConfiguration(
            name: String,
            exePath: String,
            workingDirectory: String,
            programParameters: String,
            runManager: RunManager
        ) {
            val configs = runManager.allSettings.filter { s ->
                s.type is UnityExeConfigurationType
                && s.factory is UnityExeConfigurationFactory && s.name == name
            }

            if (configs.any()) {
                configs.forEach { config ->
                    (config.configuration as UnityExeConfiguration).parameters.exePath = exePath
                }
            } else {
                val configurationType = ConfigurationTypeUtil.findConfigurationType(UnityExeConfigurationType::class.java)
                val runConfiguration = runManager.createConfiguration(name, configurationType.factory)
                val unityExeConfiguration = runConfiguration.configuration as UnityExeConfiguration
                unityExeConfiguration.parameters.exePath = exePath
                unityExeConfiguration.parameters.workingDirectory = workingDirectory
                unityExeConfiguration.parameters.programParameters = programParameters
                runConfiguration.storeInLocalWorkspace()
                runManager.addConfiguration(runConfiguration)
            }
        }
    }
}
