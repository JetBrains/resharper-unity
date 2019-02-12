import com.jetbrains.rider.test.base.BaseTestWithSolution
import org.testng.annotations.Test

class ConnectionTest : BaseTestWithSolution() {
    override fun getSolutionDirectoryName(): String {
        return "SimpleUnityProject"
    }

    var unityPackedUrl = "https://repo.labs.intellij.net/dotnet-rider-test-data/Unity_2018.3.4f1_stripped_v4.zip";

    @Test
    fun test() {

    }
}
