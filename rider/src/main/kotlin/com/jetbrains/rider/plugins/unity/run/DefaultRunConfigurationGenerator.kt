package com.jetbrains.rider.plugins.unity.run

import com.intellij.execution.RunManager
import com.intellij.execution.RunnerAndConfigurationSettings
import com.intellij.execution.configurations.ConfigurationTypeUtil
import com.intellij.execution.impl.RunManagerImpl
import com.intellij.openapi.client.ClientProjectSession
import com.intellij.openapi.components.Service
import com.intellij.openapi.diagnostic.thisLogger
import com.intellij.openapi.project.Project
import com.jetbrains.rd.protocol.SolutionExtListener
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.adviseNotNull
import com.jetbrains.rd.util.reactive.whenTrue
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.isUnityProject
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.run.configurations.UnityEditorDebugConfigurationType
import com.jetbrains.rider.plugins.unity.run.configurations.devices.UnityDevicePlayerDebugConfigurationType
import com.jetbrains.rider.plugins.unity.run.configurations.removeRunConfigurations
import com.jetbrains.rider.plugins.unity.run.configurations.unityExe.UnityExeConfiguration
import com.jetbrains.rider.plugins.unity.run.configurations.unityExe.UnityExeConfigurationFactory
import com.jetbrains.rider.plugins.unity.run.configurations.unityExe.UnityExeConfigurationType
import com.jetbrains.rider.plugins.unity.util.*
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.projectView.solutionDirectory
import java.io.File
import java.nio.file.Paths

@Service(Service.Level.PROJECT)
class DefaultRunConfigurationGenerator {

    companion object {
        val ATTACH_CONFIGURATION_NAME = UnityBundle.message("attach.to.unity.editor")
        val ATTACH_AND_PLAY_CONFIGURATION_NAME = UnityBundle.message("attach.to.unity.editor.and.play")
        val RUN_DEBUG_STANDALONE_CONFIGURATION_NAME = UnityBundle.message("standalone.player")
        val RUN_DEBUG_BATCH_MODE_UNITTESTS_CONFIGURATION_NAME = UnityBundle.message("unit.tests.batch.mode")
        val RUN_DEBUG_START_UNITY_CONFIGURATION_NAME = UnityBundle.message("start.unity")
        val OLD_RUN_DEBUG_ATTACH_UNITY_CONFIGURATION_NAME =UnityBundle.message("unity.smart.attach.player.old")
        val RUN_DEBUG_ATTACH_UNITY_CONFIGURATION_NAME = UnityBundle.message("unity.smart.attach.player")
    }

    class ProtocolListener : SolutionExtListener<FrontendBackendModel> {
        override fun extensionCreated(lifetime: Lifetime, session: ClientProjectSession, model: FrontendBackendModel) {
            model.hasUnityReference.whenTrue(lifetime) { lt ->
                val runManager = RunManager.getInstance(session.project)

                // Clean up the renamed configuration from 2025.2
                runManager.allSettings.filter { it.type is UnityDevicePlayerDebugConfigurationType
                                                && it.name == OLD_RUN_DEBUG_ATTACH_UNITY_CONFIGURATION_NAME }
                    .forEach { runManager.removeConfiguration(it) }

                // todo: consider using it later to better guess what run config to select
                val previouslySelectedConfig = RunManager.getInstance(session.project).selectedConfiguration

                // Remove any "Attach to Unity Editor" configurations. Those were discontinued in favor of "Attach to"
                removeRunConfigurations(session.project) {
                    it.type is UnityEditorDebugConfigurationType
                }

                if (session.project.isUnityProject.value
                    && !runManager.allSettings.any { it.type is UnityDevicePlayerDebugConfigurationType }) {
                    val configurationType = ConfigurationTypeUtil.findConfigurationType(UnityDevicePlayerDebugConfigurationType::class.java)
                    val runConfiguration = runManager.createConfiguration(RUN_DEBUG_ATTACH_UNITY_CONFIGURATION_NAME, configurationType.factory)
                    runConfiguration.storeInLocalWorkspace()
                    runManager.addConfiguration(runConfiguration)
                }

                session.project.solution.frontendBackendModel.unityApplicationData.adviseNotNull(lt) {
                    val exePath = UnityInstallationFinder.getOsSpecificPath(Paths.get(it.applicationPath))
                    if (exePath.toFile().isFile) {
                        createOrUpdateUnityExeRunConfiguration(
                            RUN_DEBUG_START_UNITY_CONFIGURATION_NAME,
                            exePath.toFile().canonicalPath,
                            session.project.solutionDirectory.canonicalPath,
                            mutableListOf<String>().withProjectPath(session.project).withDebugCodeOptimization().toProgramParameters(),
                            runManager)

                        createOrUpdateUnityExeRunConfiguration(
                            RUN_DEBUG_BATCH_MODE_UNITTESTS_CONFIGURATION_NAME,
                            exePath.toFile().canonicalPath,
                            session.project.solutionDirectory.canonicalPath,
                            mutableListOf<String>().withRunTests().withBatchMode()
                                .withProjectPath(session.project).withTestResults()
                                .withEditorLog()
                                .withTestPlatform().withDebugCodeOptimization().toProgramParameters(),
                            runManager)
                    } else
                        thisLogger().trace("exePath: $exePath is not a file.")

                    reorderRunConfigurations(session.project)
                }

                session.project.solution.frontendBackendModel.unityProjectSettings.buildLocation.adviseNotNull(lt) {
                    createOrUpdateUnityExeRunConfiguration(
                        RUN_DEBUG_STANDALONE_CONFIGURATION_NAME,
                        it,
                        File(it).parent!!,
                        mutableListOf<String>().toProgramParameters(),
                        runManager
                    )

                    reorderRunConfigurations(session.project)
                }

                reorderRunConfigurations(session.project)

                // make "Attach to" configuration selected if nothing is selected
                if (runManager.selectedConfiguration == null) {
                    val runConfiguration = runManager.findConfigurationByName(RUN_DEBUG_ATTACH_UNITY_CONFIGURATION_NAME)
                    if (runConfiguration != null) {
                        runManager.selectedConfiguration = runConfiguration
                    }
                }
            }
        }

        private fun reorderRunConfigurations(project: Project) {
            // Reorder configurations to put "Attach to" first
            val runManagerImpl = RunManagerImpl.getInstanceImpl(project)
            runManagerImpl.setOrder(compareBy { it.name != RUN_DEBUG_ATTACH_UNITY_CONFIGURATION_NAME },
                                    isApplyAdditionalSortByTypeAndGroup = false)
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
