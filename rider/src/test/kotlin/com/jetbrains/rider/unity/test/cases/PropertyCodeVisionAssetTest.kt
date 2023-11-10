package com.jetbrains.rider.unity.test.cases
import com.jetbrains.rider.unity.test.framework.SettingsHelper
import com.jetbrains.rider.unity.test.framework.api.prepareAssemblies
import com.intellij.openapi.editor.impl.EditorImpl
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.allure.Subsystem
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.CodeLensTestBase
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.framework.persistAllFilesOnDisk
import com.jetbrains.rider.test.scriptingApi.*
import io.qameta.allure.Description
import io.qameta.allure.Epic
import io.qameta.allure.Feature
import io.qameta.allure.Severity
import io.qameta.allure.SeverityLevel
import org.testng.annotations.DataProvider
import org.testng.annotations.Test
import java.io.File
import java.time.Duration

@Epic(Subsystem.UNITY_PLUGIN)
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

    @Test(dataProvider = "assetSettings")
    @TestEnvironment(solution = "FindUsages_05_2018")
    @Description("Unity base code vision test")
    fun baseTest(caseName: String, showProperties: String) = doUnityTest(showProperties,
            "Assets/NewBehaviourScript.cs") { false }

    @Test(dataProvider = "assetSettings")
    @TestEnvironment(solution = "RiderSample")
    @Description("Unity property code vision test")
    fun propertyCodeVision(caseName: String, showProperties: String) = doUnityTest(showProperties,
        "Assets/SampleScript.cs") { false }

    @Test(dataProvider = "assetSettings")
    @TestEnvironment(solution = "RiderSample")
    @Description("Unity property code vision test with typing")
    fun propertyCodeVisionWithTyping(caseName: String, showProperties: String) = doUnityTest(showProperties,
        "Assets/SampleScript.cs") {
        typeFromOffset("1", 577)
        waitForNextLenses()
        true
    }

    @Test(dataProvider = "assetSettings")
    @TestEnvironment(solution = "FindUsages_05_2018")
    @Description("Unity base code vision  test with yaml off")
    fun baseTestYamlOff(caseName: String, showProperties: String) = doUnityTest(showProperties,
        "Assets/NewBehaviourScript.cs") { false }

    @Test(dataProvider = "assetSettings")
    @TestEnvironment(solution = "RiderSample")
    @Description("Unity property code vision test with yaml off")
    fun propertyCodeVisionYamlOff(caseName: String, showProperties: String) = doUnityTest(showProperties,
        "Assets/SampleScript.cs") { false }

    @Test(dataProvider = "assetSettings")
    @TestEnvironment(solution = "RiderSample")
    @Description("Unity property code vision test with yaml off and typing")
    fun propertyCodeVisionWithTypingYamlOff(caseName: String, showProperties: String) = doUnityTest(showProperties,
        "Assets/SampleScript.cs") {
        typeFromOffset("1", 577)
        true
    }

    @Test(dataProvider = "assetSettings")
    @TestEnvironment(solution = "RiderSample")
    @Description("Unity property scriptable object code vision test")
    fun propertyCodeVisionScriptableObject(caseName: String, showProperties: String) = doUnityTest(showProperties,
        "Assets/TestScriptableObject.cs") {
        true
    }

    // I am not sure, how implement counter without estimated `+` sign
    // Tests for fixing current behaviour only
    @Test(dataProvider = "assetSettings")
    @TestEnvironment(solution = "PrefabModificationTestSolution")
    @Description("Unity prefab modification code vision test")
    fun prefabModifications01(caseName: String, showProperties: String) = doUnityTest("True",
        "Assets/Script1.cs") {
        true
    }

    @Test(dataProvider = "assetSettings")
    @TestEnvironment(solution = "PrefabModificationTestSolution")
    @Description("Unity prefab modification code vision test")
    fun prefabModifications02(caseName: String, showProperties: String) = doUnityTest("True",
        "Assets/Script2.cs") {
        true
    }

    @Test(dataProvider = "assetSettings")
    @TestEnvironment(solution = "PrefabModificationTestSolution")
    @Description("Unity prefab modification code vision test")
    fun prefabModifications03(caseName: String, showProperties: String) = doUnityTest("True",
        "Assets/Script3.cs") {
        true
    }

    @Test(dataProvider = "assetSettings")
    @TestEnvironment(solution = "PrefabModificationTestSolution")
    @Description("Unity prefab modification code vision test")
    fun prefabModifications04(caseName: String, showProperties: String) = doUnityTest("True",
        "Assets/Script4.cs") {
        true
    }

    @Test(dataProvider = "assetSettings")
    @TestEnvironment(solution = "PrefabModificationTestSolution")
    @Description("Unity prefab modification code vision test")
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