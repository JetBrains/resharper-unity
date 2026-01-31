package com.jetbrains.rider.unity.test.cases.integrationTests

import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.annotations.UnityTestSettings
import com.jetbrains.rider.test.annotations.report.ChecklistItems
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.annotations.report.Severity
import com.jetbrains.rider.test.annotations.report.SeverityLevel
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.enums.TuanjieVersion
import com.jetbrains.rider.test.enums.UnityVersion
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.scriptingApi.rebuildSolutionWithReSharperBuild
import com.jetbrains.rider.test.scriptingApi.replaceFileContent
import com.jetbrains.rider.unity.test.framework.api.attachDebuggerToUnityEditor
import com.jetbrains.rider.unity.test.framework.api.attachDebuggerToUnityEditorAndPlay
import com.jetbrains.rider.unity.test.framework.api.pause
import com.jetbrains.rider.unity.test.framework.api.play
import com.jetbrains.rider.unity.test.framework.api.refreshUnityModel
import com.jetbrains.rider.unity.test.framework.api.step
import com.jetbrains.rider.unity.test.framework.api.stopPlaying
import com.jetbrains.rider.unity.test.framework.api.unpause
import com.jetbrains.rider.unity.test.framework.api.waitForEditorLogsAfterAction
import com.jetbrains.rider.unity.test.framework.api.waitForUnityEditorPlayMode
import com.jetbrains.rider.unity.test.framework.base.IntegrationTestWithUnityProjectBase
import org.testng.annotations.Test

@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("PlayMode Action for Unity")
@Severity(SeverityLevel.CRITICAL)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@Solution("UnityDebugAndUnitTesting/Project")
abstract class PlayModeTest() : IntegrationTestWithUnityProjectBase() {
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
@UnityTestSettings(unityVersion = UnityVersion.V2022)
class PlayModeTestUnity2022 : PlayModeTest(){
    init {
        addMute(Mute("RIDER-105666"), ::checkPlayModeLogs)
    }
}

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@UnityTestSettings(unityVersion = UnityVersion.V6)
class PlayModeTestUnity6 : PlayModeTest(){
    init {
        addMute(Mute("RIDER-105666"), ::checkPlayModeLogs)
    }
}

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@UnityTestSettings(unityVersion = UnityVersion.V6_2)
class PlayModeTestUnity6_2 : PlayModeTest(){
    init {
        addMute(Mute("RIDER-105666"), ::checkPlayModeLogs)
    }
}

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@Mute("RIDER-113191")
@Solution("TuanjieDebugAndUnitTesting/Project")
@UnityTestSettings(tuanjieVersion = TuanjieVersion.V2022)
class PlayModeTestTuanjie2022 : PlayModeTest(){
    init {
        addMute(Mute("RIDER-105666"), ::checkPlayModeLogs)
    }
}