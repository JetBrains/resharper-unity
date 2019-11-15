import com.intellij.openapi.editor.impl.EditorImpl
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.CodeLensBaseTest
import com.jetbrains.rider.test.framework.combine
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.framework.persistAllFilesOnDisk
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.BeforeMethod
import org.testng.annotations.BeforeSuite
import org.testng.annotations.DataProvider
import org.testng.annotations.Test
import java.io.File

class PropertyCodeVisionAssetTest : CodeLensBaseTest() {

    private val disableYamlDotSettingsContents = """<wpf:ResourceDictionary xml:space="preserve" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:s="clr-namespace:System;assembly=mscorlib" xmlns:ss="urn:shemas-jetbrains-com:settings-storage-xaml" xmlns:wpf="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
	        <s:Boolean x:Key="/Default/CodeEditing/Unity/IsYamlParsingEnabled/@EntryValue">False</s:Boolean>
            </wpf:ResourceDictionary>"""

    lateinit var unityDll: File

    @BeforeSuite(alwaysRun = true)
    fun getUnityDll() {
        unityDll = downloadUnityDll()
    }

    @BeforeMethod
    fun InitializeEnvironement() {
        copyUnityDll(unityDll, project, activeSolutionDirectory)
        enableAllLensProviders()
    }

    companion object {
        const val assetUsagesProvider = "Unity Assets Usage"
        const val unityFieldProvider = "Unity serialized field"
        const val impicitUsagesProvider = "Unity implicit usage"
    }

    override val waitForCaches = true

    override fun getSolutionDirectoryName() = "CodeLensTestSolution"

    override fun preprocessTempDirectory(tempDir: File) {
        if (testMethod.name.contains("YamlOff")) {
            val dotSettingsFile = activeSolutionDirectory.combine("$activeSolution.sln.DotSettings.user")
            dotSettingsFile.writeText(disableYamlDotSettingsContents)
        }
    }

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

    fun doUnityTest(showProperties: String, file: String, action: EditorImpl.() -> Boolean) {
        setReSharperSetting("CodeEditing/Unity/EnableInspectorPropertiesEditor/@EntryValue", showProperties)

        waitForLensInfos(project)
        waitForAllAnalysisFinished(project)
        val editor = withOpenedEditor(file) {
            waitForLenses()
            executeWithGold(testGoldFile) {

                it.println("before change")
                it.print(dumpLenses())
                if (action()) {
                    persistAllFilesOnDisk(project)
                    waitForNextLenses()
                    it.println("after change")
                    it.print(dumpLenses())
                }
            }
        }
        closeEditor(editor)
    }
}