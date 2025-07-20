package com.jetbrains.rider.unity.test.cases.uss

import com.jetbrains.rider.test.OpenSolutionParams
import com.jetbrains.rider.test.annotations.report.ChecklistItems
import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.PerTestSolutionTestBase
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.scriptingApi.getHighlighters
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import com.jetbrains.rider.unity.test.framework.api.prepareAssemblies
import org.testng.annotations.Test

@Mute("Test wasn't in te right package, so it never run")
@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@TestEnvironment(platform = [PlatformType.ALL], sdkVersion = SdkVersion.LATEST_STABLE)
@Solution("SimpleUnityProject")
class UssTest : PerTestSolutionTestBase() {
    override fun modifyOpenSolutionParams(params: OpenSolutionParams) {
        params.preprocessTempDirectory = { prepareAssemblies(it) }
    }

    @Test
    @ChecklistItems(["Highlightings/Tags for uss files"])
    fun highlighterTagsTest() {
        executeWithGold(testGoldFile) {
            withOpenedEditor("Assets/Uxml/Menu.uss") {
                // it doesn't really dump neither warning on margin-left, margin-right, nor CssUnknownTargetInspection (which is suppressed)
                // todo: find out a way to dump those
                // this test is still useful to find possible exceptions on opening uss file
                it.print(getHighlighters(this.project!!, this, checkInfos = true, checkWarnings = true, checkWeakWarnings = true))
            }
        }
    }
}