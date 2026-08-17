package com.jetbrains.rider.unity.test.cases
import com.intellij.codeInsight.codeVision.lensContextIfCreated
import com.intellij.openapi.editor.impl.EditorImpl
import com.intellij.openapi.rd.util.lifetime
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rdclient.util.idea.pumpMessages
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.OpenSolutionParams
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.annotations.report.ChecklistItems
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.annotations.report.Severity
import com.jetbrains.rider.test.annotations.report.SeverityLevel
import com.jetbrains.rider.test.junit5.base.CodeLensTestBase
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.framework.advancedSettings.AdvancedSettingsList
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.framework.getGoldFileText
import com.jetbrains.rider.test.framework.persistAllFilesOnDisk
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import com.jetbrains.rider.test.scriptingApi.closeEditor
import com.jetbrains.rider.test.scriptingApi.dumpLenses
import com.jetbrains.rider.test.scriptingApi.typeFromOffset
import com.jetbrains.rider.test.scriptingApi.waitForAllAnalysisFinished
import com.jetbrains.rider.test.scriptingApi.waitForLensInfos
import com.jetbrains.rider.test.scriptingApi.waitForLenses
import com.jetbrains.rider.test.scriptingApi.waitForNextLenses
import com.jetbrains.rider.unity.test.framework.SettingsHelper
import com.jetbrains.rider.unity.test.framework.api.prepareAssemblies
import org.junit.jupiter.params.ParameterizedTest
import org.junit.jupiter.params.provider.Arguments
import org.junit.jupiter.params.provider.MethodSource
import org.junit.jupiter.api.Tag
import java.time.Duration
import java.util.stream.Stream
import kotlin.io.path.name

@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity code vision")
@Severity(SeverityLevel.CRITICAL)
@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
@Solution("CodeLensTestSolution")
@Tag(TeamCityTags.Plugins.Unity.General)
class PropertyCodeVisionAssetTest : CodeLensTestBase() {

    override val advancedSettings: AdvancedSettingsList
        get() = AdvancedSettingsList(boolSettings = mapOf(("repository.view.enabled.v2" to false)))

    override fun modifyOpenSolutionParams(params: OpenSolutionParams) {
        super.modifyOpenSolutionParams(params)
        params.waitForCaches = true
        params.preprocessTempDirectory = {
            prepareAssemblies(it)
            if (testMethod.name.contains("YamlOff")) {
                SettingsHelper.disableIsAssetIndexingEnabledSetting(it.name, it)
            }
        }
    }
    fun assetSettings(): Stream<Arguments> = Stream.of(
        Arguments.of("Properties", "True"),
        Arguments.of("NoProperties", "False")
    )

    @ParameterizedTest(name = "{0}") // Unity base code vision test
    @MethodSource("assetSettings")
    @ChecklistItems(["Code vision/Base code vision"])
    @Solution("FindUsages_05_2018")
    fun baseTest(caseName: String, showProperties: String) = doUnityTest(showProperties,
            "Assets/NewBehaviourScript.cs") { false }

    @ParameterizedTest(name = "{0}") // Unity property code vision test
    @MethodSource("assetSettings")
    @Solution("RiderSample")
    @ChecklistItems(["Code vision/Property code vision"])
    fun propertyCodeVision(caseName: String, showProperties: String) = doUnityTest(showProperties,
        "Assets/SampleScript.cs") { false }

    @ParameterizedTest(name = "{0}") // Unity property code vision test with typing
    @MethodSource("assetSettings")
    @Solution("RiderSample")
    @ChecklistItems(["Code vision/Property code vision with typing"])
    fun propertyCodeVisionWithTyping(caseName: String, showProperties: String) = doUnityTest(showProperties,
        "Assets/SampleScript.cs") {
        typeFromOffset("1", 577)
        waitForNextLenses()
        true
    }

    @ParameterizedTest(name = "{0}") // Unity base code vision  test with yaml off
    @MethodSource("assetSettings")
    @Solution("FindUsages_05_2018")
    @ChecklistItems(["Code vision/Base code vision with yaml off"])
    fun baseTestYamlOff(caseName: String, showProperties: String) = doUnityTest(showProperties,
        "Assets/NewBehaviourScript.cs") { false }

    @ParameterizedTest(name = "{0}") // Unity property code vision test with yaml off
    @MethodSource("assetSettings")
    @Solution("RiderSample")
    @ChecklistItems(["Code vision/Property code vision with yaml off"])
    fun propertyCodeVisionYamlOff(caseName: String, showProperties: String) = doUnityTest(showProperties,
        "Assets/SampleScript.cs") { false }

    @ParameterizedTest(name = "{0}") // Unity property code vision test with yaml off and typing
    @MethodSource("assetSettings")
    @Solution("RiderSample")
    @ChecklistItems(["Code vision/Property code vision with yaml off and typing"])
    fun propertyCodeVisionWithTypingYamlOff(caseName: String, showProperties: String) = doUnityTest(showProperties,
        "Assets/SampleScript.cs") {
        typeFromOffset("1", 577)
        true
    }

    @ParameterizedTest(name = "{0}") // Unity property scriptable object code vision test
    @MethodSource("assetSettings")
    @Solution("RiderSample")
    @ChecklistItems(["Code vision/Property scriptable object code vision"])
    fun propertyCodeVisionScriptableObject(caseName: String, showProperties: String) = doUnityTest(showProperties,
        "Assets/TestScriptableObject.cs") {
        true
    }

    // I am not sure, how implement counter without estimated `+` sign
    // Tests for fixing current behaviour only
    @ParameterizedTest(name = "{0}") // Unity prefab modification code vision test
    @MethodSource("assetSettings")
    @Solution("PrefabModificationTestSolution")
    @ChecklistItems(["Code vision/Prefab modification code vision"])
    fun prefabModifications01(caseName: String, showProperties: String) = doUnityTest("True",
        "Assets/Script1.cs") {
        true
    }

    @ParameterizedTest(name = "{0}") // Unity prefab modification code vision test
    @MethodSource("assetSettings")
    @Solution("PrefabModificationTestSolution")
    @ChecklistItems(["Code vision/Prefab modification code vision"])
    fun prefabModifications02(caseName: String, showProperties: String) = doUnityTest("True",
        "Assets/Script2.cs") {
        true
    }

    @ParameterizedTest(name = "{0}") // Unity prefab modification code vision test
    @MethodSource("assetSettings")
    @Solution("PrefabModificationTestSolution")
    @ChecklistItems(["Code vision/Prefab modification code vision"])
    fun prefabModifications03(caseName: String, showProperties: String) = doUnityTest("True",
        "Assets/Script3.cs") {
        true
    }

    @ParameterizedTest(name = "{0}") // Unity prefab modification code vision test
    @MethodSource("assetSettings")
    @Solution("PrefabModificationTestSolution")
    @ChecklistItems(["Code vision/Prefab modification code vision"])
    fun prefabModifications04(caseName: String, showProperties: String) = doUnityTest("True",
        "Assets/Script4.cs") {
        true
    }

    @ParameterizedTest(name = "{0}") // Unity prefab modification code vision test
    @MethodSource("assetSettings")
    @Solution("PrefabModificationTestSolution")
    @ChecklistItems(["Code vision/Prefab modification code vision"])
    fun prefabModifications05(caseName: String, showProperties: String) = doUnityTest("True",
        "Assets/Script5.cs") {
        true
    }


    fun doUnityTest(showProperties: String, file: String, action: EditorImpl.() -> Boolean) {
        setReSharperSetting("CodeEditing/Unity/EnableInspectorPropertiesEditor/@EntryValue", showProperties)
        waitAndPump(project.lifetime, { project.solution.frontendBackendModel.isDeferredCachesCompletedOnce.valueOrDefault(false)}, Duration.ofSeconds(10), { "Deferred caches are not completed" })

        waitForLensInfos(project)
        waitForAllAnalysisFinished(project)
        val editor = withOpenedEditor(file) {
            executeWithGold(testGoldFile) {
                val expectedInlaysText = getGoldFileText(testGoldFile)
                val expectedTextBeforeAction = expectedInlaysText.substringBefore("after change")
                val currentBeforeActionInlaysTextBuilder: StringBuilder = StringBuilder(expectedInlaysText.length)
                val timeout = Duration.ofSeconds(60)
                waitForLenses()
                pumpMessages(timeout) {
                    lensContextIfCreated!!.resubmitThings()
                    currentBeforeActionInlaysTextBuilder.clear()
                    currentBeforeActionInlaysTextBuilder.appendLine("before change")
                    currentBeforeActionInlaysTextBuilder.append(dumpLenses())
                    return@pumpMessages currentBeforeActionInlaysTextBuilder.toString() == expectedTextBeforeAction
                }
                if (action()) {
                    persistAllFilesOnDisk()
                    val currentAfterActionInlaysTextBuilder = StringBuilder(expectedInlaysText.length)
                    waitForLenses()
                    pumpMessages(timeout) {
                        lensContextIfCreated!!.resubmitThings()
                        currentAfterActionInlaysTextBuilder.clear()
                        currentAfterActionInlaysTextBuilder.append(currentBeforeActionInlaysTextBuilder.toString())
                        currentAfterActionInlaysTextBuilder.appendLine("after change")
                        currentAfterActionInlaysTextBuilder.append(dumpLenses())
                        return@pumpMessages currentAfterActionInlaysTextBuilder.toString() == expectedInlaysText
                    }
                    it.print(currentAfterActionInlaysTextBuilder.toString())
                } else {
                    it.print(currentBeforeActionInlaysTextBuilder.toString())
                }
            }
        }
        closeEditor(editor)
    }
}