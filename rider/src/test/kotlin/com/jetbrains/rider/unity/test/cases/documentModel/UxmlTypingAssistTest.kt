package com.jetbrains.rider.unity.test.cases.documentModel

import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.annotations.report.Issue
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.facades.editor.PatchEngineEditorTestMode
import com.jetbrains.rider.test.junit5.base.PatchEngineEditorTestBase
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.scriptingApi.dumpOpenedEditorFacade
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test

@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Typing Assist in UXML files")
@Issue("RIDER-138353")
@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
@Solution("SimpleUnityProjectWithShaders")
@Tag(TeamCityTags.Plugins.Unity.General)
abstract class UxmlTypingAssistTestBase(mode: PatchEngineEditorTestMode) : PatchEngineEditorTestBase(mode) {
    @Test
    fun testTypeClosingAngleBracket() {
        dumpOpenedEditorFacade("Assets/UI/Menu.uxml", "Menu.uxml") {
            type(">")
            type("Start")
        }
    }

    @Test
    fun testTypeSlashAfterOpenAngleBracket() {
        dumpOpenedEditorFacade("Assets/UI/Menu.uxml", "Menu.uxml") {
            type("/")
        }
    }
}

// This scenario makes sure the backend doesn't generate changes
class UxmlTypingAssistSpeculativeRebaseProhibitedTest : UxmlTypingAssistTestBase(PatchEngineEditorTestMode.SpeculativeRebaseProhibited)
