package com.jetbrains.rider.unity.test.cases.integrationTests

import com.jetbrains.rider.test.annotations.*
import com.jetbrains.rider.test.annotations.report.ChecklistItems
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.annotations.report.Severity
import com.jetbrains.rider.test.annotations.report.SeverityLevel
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.scriptingApi.EngineVersion
import com.jetbrains.rider.test.scriptingApi.Tuanjie
import com.jetbrains.rider.test.scriptingApi.Unity
import com.jetbrains.rider.test.scriptingApi.rebuildSolutionWithReSharperBuild
import com.jetbrains.rider.test.scriptingApi.replaceFileContent
import com.jetbrains.rider.unity.test.framework.api.*
import com.jetbrains.rider.unity.test.framework.base.IntegrationTestWithUnityProjectBase
import org.testng.annotations.Test

@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("PlayMode Action for Unity")
@Severity(SeverityLevel.CRITICAL)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@Solution("UnityDebugAndUnitTesting/Project")
abstract class PlayModeTest(engineVersion: EngineVersion) : IntegrationTestWithUnityProjectBase(engineVersion) {
    @Test(description="Check play, pause, step, unpause, stop actions for Unity")
    @ChecklistItems(["Play Mode/PlayMode actions (play, stop. etc.)"])
    fun checkPlayingPauseModesAndSteps() {
        play()
        pause()
        step()
        unpause()
        stopPlaying()
    }

    @Test(description="Check play, pause, step, unpause, stop actions for Unity with Attach to Unity Process")
    @ChecklistItems(["Play Mode/PlayMode actions (play, stop. etc.) while debugger attached"])
    fun checkAttachDebuggerToUnityEditor() {
        attachDebuggerToUnityEditor({},
                                    {
                                        play()
                                        pause()
                                        step()
                                        unpause()
                                        stopPlaying()
                                    })
    }

    @Test(description="Check play, pause, step, unpause, stop actions for Unity with Attach to Unity Process and Play")
    @ChecklistItems(["Play Mode/PlayMode actions (play, stop. etc.) while debugger attached and play"])
    fun checkAttachDebuggerToUnityEditorAndPlay() {
        attachDebuggerToUnityEditorAndPlay({},
                                           {
                                               waitForUnityEditorPlayMode()
                                               pause()
                                               step()
                                               unpause()
                                               stopPlaying()
                                           })
    }

    @Test(description="Check start, update, quit logs")
    @ChecklistItems(["Play Mode/PlayMode logs"])
    fun checkPlayModeLogs() {
        replaceFileContent(project, "NewBehaviourScript.cs",)
        rebuildSolutionWithReSharperBuild()
        refreshUnityModel()

        waitForEditorLogsAfterAction("Start", "StartFromBackgroundThread") { play() }
        pause()
        waitForEditorLogsAfterAction("Update", "UpdateFromBackgroundThread") { step(false) }
        unpause()
        waitForEditorLogsAfterAction("Quit", "QuitFromBackgroundThread") { stopPlaying() }
    }
}

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
class PlayModeTestUnity2020 : PlayModeTest(Unity.V2020) {
    init {
        addMute(Mute("RIDER-122954"), ::checkAttachDebuggerToUnityEditor)
        addMute(Mute("RIDER-122954"), ::checkAttachDebuggerToUnityEditorAndPlay)
    }
}

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
class PlayModeTestUnity2022 : PlayModeTest(Unity.V2022)

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
class PlayModeTestUnity6 : PlayModeTest(Unity.V6)

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
class PlayModeTestUnity6_2 : PlayModeTest(Unity.V6_2)

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@Mute("RIDER-113191")
@Solution("TuanjieDebugAndUnitTesting/Project")
class PlayModeTestTuanjie2022 : PlayModeTest(Tuanjie.V2022)