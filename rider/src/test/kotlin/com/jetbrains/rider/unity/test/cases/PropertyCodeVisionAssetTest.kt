package com.jetbrains.rider.unity.test.cases
import com.intellij.openapi.editor.impl.EditorImpl
import com.intellij.openapi.rd.util.lifetime
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.allure.SubsystemConstants
import com.jetbrains.rider.test.annotations.*
import com.jetbrains.rider.test.base.CodeLensTestBase
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.framework.persistAllFilesOnDisk
import com.jetbrains.rider.test.scriptingApi.*
import com.jetbrains.rider.unity.test.framework.SettingsHelper
import com.jetbrains.rider.unity.test.framework.api.prepareAssemblies
import org.testng.annotations.DataProvider
import org.testng.annotations.Test
import java.io.File
import java.time.Duration

@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity code vision")
@Severity(SeverityLevel.CRITICAL)
@TestEnvironment(sdkVersion = SdkVersion.DOT_NET_6)
class PropertyCodeVisionAssetTest : CodeLensTestBase() {

    override fun preprocessTempDirectory(tempDir: File) {
        prepareAssemblies(activeSolutionDirectory)
        if (testMethod.name.contains("YamlOff")) {
            SettingsHelper.disableIsAssetIndexingEnabledSetting(activeSolution, activeSolutionDirectory)
        }
    }

    override val waitForCaches = true

    override fun getSolutionDirectoryName() = "CodeLensTestSolution"

    @DataProvider(name = "assetSettings")
    fun assetSettings() = arrayOf(
        arrayOf("Properties", "True"),
        arrayOf("NoProperties", "False")
    )

    @Test(description = "Unity base code vision test", dataProvider = "assetSettings")
    @TestEnvironment(solution = "FindUsages_05_2018")
    fun baseTest(caseName: String, showProperties: String) = doUnityTest(showProperties,
            "Assets/NewBehaviourScript.cs") { false }

    @Test(description = "Unity property code vision test", dataProvider = "assetSettings")
    @TestEnvironment(solution = "RiderSample")
    fun propertyCodeVision(caseName: String, showProperties: String) = doUnityTest(showProperties,
        "Assets/SampleScript.cs") { false }

    @Mute("RIDER-96147", specificParameters = ["NoProperties"])
    @Test(description = "Unity property code vision test with typing", dataProvider = "assetSettings")
    @TestEnvironment(solution = "RiderSample")
    fun propertyCodeVisionWithTyping(caseName: String, showProperties: String) = doUnityTest(showProperties,
        "Assets/SampleScript.cs") {
        typeFromOffset("1", 577)
        waitForNextLenses()
        true
    }

    @Test(description = "Unity base code vision  test with yaml off", dataProvider = "assetSettings")
    @TestEnvironment(solution = "FindUsages_05_2018")
    fun baseTestYamlOff(caseName: String, showProperties: String) = doUnityTest(showProperties,
        "Assets/NewBehaviourScript.cs") { false }

    @Test(description = "Unity property code vision test with yaml off", dataProvider = "assetSettings")
    @TestEnvironment(solution = "RiderSample")
    fun propertyCodeVisionYamlOff(caseName: String, showProperties: String) = doUnityTest(showProperties,
        "Assets/SampleScript.cs") { false }

    @Test(description = "Unity property code vision test with yaml off and typing", dataProvider = "assetSettings")
    @Mute("RIDER-96147", specificParameters = ["Properties", "NoProperties"])
    @TestEnvironment(solution = "RiderSample")
    fun propertyCodeVisionWithTypingYamlOff(caseName: String, showProperties: String) = doUnityTest(showProperties,
        "Assets/SampleScript.cs") {
        typeFromOffset("1", 577)
        true
    }

    @Test(description = "Unity property scriptable object code vision test", dataProvider = "assetSettings")
    @TestEnvironment(solution = "RiderSample")
    fun propertyCodeVisionScriptableObject(caseName: String, showProperties: String) = doUnityTest(showProperties,
        "Assets/TestScriptableObject.cs") {
        true
    }

    // I am not sure, how implement counter without estimated `+` sign
    // Tests for fixing current behaviour only
    @Mute("RIDER-96147", specificParameters = ["NoProperties"])
    @Test(description = "Unity prefab modification code vision test", dataProvider = "assetSettings")
    @TestEnvironment(solution = "PrefabModificationTestSolution")
    fun prefabModifications01(caseName: String, showProperties: String) = doUnityTest("True",
        "Assets/Script1.cs") {
        true
    }

    @Test(description = "Unity prefab modification code vision test", dataProvider = "assetSettings")
    @TestEnvironment(solution = "PrefabModificationTestSolution")
    fun prefabModifications02(caseName: String, showProperties: String) = doUnityTest("True",
        "Assets/Script2.cs") {
        true
    }

    @Test(description = "Unity prefab modification code vision test", dataProvider = "assetSettings")
    @TestEnvironment(solution = "PrefabModificationTestSolution")
    fun prefabModifications03(caseName: String, showProperties: String) = doUnityTest("True",
        "Assets/Script3.cs") {
        true
    }

    @Test(description = "Unity prefab modification code vision test", dataProvider = "assetSettings")
    @TestEnvironment(solution = "PrefabModificationTestSolution")
    @Mute("RIDER-96147", specificParameters = ["Properties", "NoProperties"])
    fun prefabModifications04(caseName: String, showProperties: String) = doUnityTest("True",
        "Assets/Script4.cs") {
        true
    }

    @Test(description = "Unity prefab modification code vision test", dataProvider = "assetSettings")
    @Mute("RIDER-96147", specificParameters = ["NoProperties", "Properties"])
    @TestEnvironment(solution = "PrefabModificationTestSolution")
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
            waitForLenses()
            executeWithGold(testGoldFile) {

                it.println("before change")
                it.print(dumpLenses())
                if (action()) {
                    persistAllFilesOnDisk()
                    waitForNextLenses()
                    it.println("after change")
                    it.print(dumpLenses())
                }
            }
        }
        closeEditor(editor)
    }
}