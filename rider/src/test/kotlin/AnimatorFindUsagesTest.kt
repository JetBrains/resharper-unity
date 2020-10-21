import base.FindUsagesAssetTestBase
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.scriptingApi.setGroupingEnabled
import org.testng.annotations.Test

@TestEnvironment(platform = [PlatformType.ALL])
open class AnimatorFindUsagesTest : FindUsagesAssetTestBase() {
    override fun getSolutionDirectoryName(): String {
        return "AnimatorFindUsages"
    }

    @Test(dataProvider = "findUsagesGrouping")
    fun animatorFindUsages(caseName: String, groups: List<String>?) {
        disableAllGroups()
        groups?.forEach { group -> setGroupingEnabled(group, true) }
        doTest(5, 17, "Behaviour.cs")
    }
}