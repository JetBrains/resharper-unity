package com.jetbrains.rider.unity.test.cases
import com.intellij.openapi.editor.impl.EditorImpl
import com.intellij.openapi.rd.util.lifetime
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rdclient.codeVision.frontendLensContextOrThrow
import com.jetbrains.rdclient.util.idea.pumpMessages
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.OpenSolutionParams
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.annotations.*
import com.jetbrains.rider.test.base.CodeLensTestBase
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.framework.*
import com.jetbrains.rider.test.scriptingApi.*
import com.jetbrains.rider.unity.test.framework.SettingsHelper
import com.jetbrains.rider.unity.test.framework.api.prepareAssemblies
import org.testng.annotations.DataProvider
import org.testng.annotations.Test
import java.time.Duration

@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity code vision")
@Severity(SeverityLevel.CRITICAL)
@TestEnvironment(sdkVersion = SdkVersion.LATEST_STABLE)
@Solution("CodeLensTestSolution")
class PropertyCodeVisionAssetTest : CodeLensTestBase() {
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

    @DataProvider(name = "assetSettings")
    fun assetSettings() = arrayOf(
        arrayOf("Properties", "True"),
        arrayOf("NoProperties", "False")
    )

    @Test(description = "Unity base code vision test", dataProvider = "assetSettings")
    @ChecklistItems(["Code vision/Base code vision"])
    @Solution("FindUsages_05_2018")
    fun baseTest(caseName: String, showProperties: String) = doUnityTest(showProperties,
            "Assets/NewBehaviourScript.cs") { false }

    @Test(description = "Unity property code vision test", dataProvider = "assetSettings")
    @Solution("RiderSample")
    @ChecklistItems(["Code vision/Property code vision"])
    fun propertyCodeVision(caseName: String, showProperties: String) = doUnityTest(showProperties,
        "Assets/SampleScript.cs") { false }

    @Test(description = "Unity property code vision test with typing", dataProvider = "assetSettings")
    @Solution("RiderSample")
    @ChecklistItems(["Code vision/Property code vision with typing"])
    fun propertyCodeVisionWithTyping(caseName: String, showProperties: String) = doUnityTest(showProperties,
        "Assets/SampleScript.cs") {
        typeFromOffset("1", 577)
        waitForNextLenses()
        true
    }

    @Test(description = "Unity base code vision  test with yaml off", dataProvider = "assetSettings")
    @Solution("FindUsages_05_2018")
    @ChecklistItems(["Code vision/Base code vision with yaml off"])
    fun baseTestYamlOff(caseName: String, showProperties: String) = doUnityTest(showProperties,
        "Assets/NewBehaviourScript.cs") { false }

    @Test(description = "Unity property code vision test with yaml off", dataProvider = "assetSettings")
    @Solution("RiderSample")
    @ChecklistItems(["Code vision/Property code vision with yaml off"])
    fun propertyCodeVisionYamlOff(caseName: String, showProperties: String) = doUnityTest(showProperties,
        "Assets/SampleScript.cs") { false }

    @Test(description = "Unity property code vision test with yaml off and typing", dataProvider = "assetSettings")
    @Solution("RiderSample")
    @ChecklistItems(["Code vision/Property code vision with yaml off and typing"])
    fun propertyCodeVisionWithTypingYamlOff(caseName: String, showProperties: String) = doUnityTest(showProperties,
        "Assets/SampleScript.cs") {
        typeFromOffset("1", 577)
        true
    }

    @Test(description = "Unity property scriptable object code vision test", dataProvider = "assetSettings")
    @Solution("RiderSample")
    @ChecklistItems(["Code vision/Property scriptable object code vision"])
    fun propertyCodeVisionScriptableObject(caseName: String, showProperties: String) = doUnityTest(showProperties,
        "Assets/TestScriptableObject.cs") {
        true
    }

    // I am not sure, how implement counter without estimated `+` sign
    // Tests for fixing current behaviour only
    @Test(description = "Unity prefab modification code vision test", dataProvider = "assetSettings")
    @Solution("PrefabModificationTestSolution")
    @ChecklistItems(["Code vision/Prefab modification code vision"])
    fun prefabModifications01(caseName: String, showProperties: String) = doUnityTest("True",
        "Assets/Script1.cs") {
        true
    }

    @Test(description = "Unity prefab modification code vision test", dataProvider = "assetSettings")
    @Solution("PrefabModificationTestSolution")
    @ChecklistItems(["Code vision/Prefab modification code vision"])
    fun prefabModifications02(caseName: String, showProperties: String) = doUnityTest("True",
        "Assets/Script2.cs") {
        true
    }

    @Test(description = "Unity prefab modification code vision test", dataProvider = "assetSettings")
    @Solution("PrefabModificationTestSolution")
    @ChecklistItems(["Code vision/Prefab modification code vision"])
    fun prefabModifications03(caseName: String, showProperties: String) = doUnityTest("True",
        "Assets/Script3.cs") {
        true
    }

    @Test(description = "Unity prefab modification code vision test", dataProvider = "assetSettings")
    @Solution("PrefabModificationTestSolution")
    @ChecklistItems(["Code vision/Prefab modification code vision"])
    fun prefabModifications04(caseName: String, showProperties: String) = doUnityTest("True",
        "Assets/Script4.cs") {
        true
    }

    @Test(description = "Unity prefab modification code vision test", dataProvider = "assetSettings")
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
                    frontendLensContextOrThrow.resubmitThings()
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
                        frontendLensContextOrThrow.resubmitThings()
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