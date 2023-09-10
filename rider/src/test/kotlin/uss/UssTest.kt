import base.integrationTests.prepareAssemblies
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.getHighlighters
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import org.testng.annotations.Test
import java.io.File

@TestEnvironment(platform = [PlatformType.ALL], sdkVersion = SdkVersion.DOT_NET_6)
class UssTest : BaseTestWithSolution() {
    override fun getSolutionDirectoryName(): String {
        return "SimpleUnityProject"
    }

    override fun preprocessTempDirectory(tempDir: File) {
        prepareAssemblies(activeSolutionDirectory)
    }

    @Test
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