import com.intellij.openapi.editor.impl.EditorImpl
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.CodeLensBaseTest
import com.jetbrains.rider.test.framework.combine
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.framework.persistAllFilesOnDisk
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.BeforeSuite
import org.testng.annotations.DataProvider
import org.testng.annotations.Test
import java.io.File
import java.time.Duration

class PropertyCodeVisionAssetTest : CodeLensBaseTest() {

    private val disableYamlDotSettingsContents = """<wpf:ResourceDictionary xml:space="preserve" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:s="clr-namespace:System;assembly=mscorlib" xmlns:ss="urn:shemas-jetbrains-com:settings-storage-xaml" xmlns:wpf="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
	        <s:Boolean x:Key="/Default/CodeEditing/Unity/IsAssetIndexingEnabled/@EntryValue">False</s:Boolean>
            </wpf:ResourceDictionary>"""

    lateinit var unityDll: File

    @BeforeSuite(alwaysRun = true)
    fun getUnityDll() {
        unityDll = downloadUnityDll()
    }

    override fun preprocessTempDirectory(tempDir: File) {
        copyUnityDll(unityDll, activeSolutionDirectory)
        if (testMethod.name.contains("YamlOff")) {
            val dotSettingsFile = activeSolutionDirectory.combine("$activeSolution.sln.DotSettings.user")
            dotSettingsFile.writeText(disableYamlDotSettingsContents)
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
    fun baseTest(caseName: String, showProperties: String) = doUnityTest(showProperties,
            "Assets/NewBehaviourScript.cs") { false }

    @Test(dataProvider = "assetSettings")
    @TestEnvironment(solution = "RiderSample")
    fun propertyCodeVision(caseName: String, showProperties: String) = doUnityTest(showProperties,
        "Assets/SampleScript.cs") { false }

    @Test(dataProvider = "assetSettings")
    @TestEnvironment(solution = "RiderSample")
    fun propertyCodeVisionWithTyping(caseName: String, showProperties: String) = doUnityTest(showProperties,
        "Assets/SampleScript.cs") {
        typeFromOffset("1", 577)
        waitForNextLenses()
        true
    }

    @Test(dataProvider = "assetSettings")
    @TestEnvironment(solution = "FindUsages_05_2018")
    fun baseTestYamlOff(caseName: String, showProperties: String) = doUnityTest(showProperties,
        "Assets/NewBehaviourScript.cs") { false }

    @Test(dataProvider = "assetSettings")
    @TestEnvironment(solution = "RiderSample")
    fun propertyCodeVisionYamlOff(caseName: String, showProperties: String) = doUnityTest(showProperties,
        "Assets/SampleScript.cs") { false }

    @Test(dataProvider = "assetSettings")
    @TestEnvironment(solution = "RiderSample")
    fun propertyCodeVisionWithTypingYamlOff(caseName: String, showProperties: String) = doUnityTest(showProperties,
        "Assets/SampleScript.cs") {
        typeFromOffset("1", 577)
        true
    }

    @Test(dataProvider = "assetSettings")
    @TestEnvironment(solution = "RiderSample")
    fun propertyCodeVisionScriptableObject(caseName: String, showProperties: String) = doUnityTest(showProperties,
        "Assets/TestScriptableObject.cs") {
        true
    }

    fun doUnityTest(showProperties: String, file: String, action: EditorImpl.() -> Boolean) {
        setReSharperSetting("CodeEditing/Unity/EnableInspectorPropertiesEditor/@EntryValue", showProperties)
        waitAndPump(project.lifetime, { project.solution.rdUnityModel.isDeferredCachesCompletedOnce.valueOrDefault(false)}, Duration.ofSeconds(10), { "Deferred caches are not completed" })

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