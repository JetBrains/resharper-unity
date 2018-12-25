import com.jetbrains.rider.test.TestCaseRunner
import com.jetbrains.rider.test.base.BaseTestWithSolutionBase
import com.jetbrains.rider.test.base.RiderTemplatesTestCoreBase
import com.jetbrains.rider.test.scriptingApi.checkSelectedRunConfigurationExecutionNotAllowed
import com.jetbrains.rider.test.scriptingApi.checkSwea
import org.testng.annotations.Test

class UnityClassLibTest : BaseTestWithSolutionBase() {

    var templateId = "JetBrains.Common.Unity.Library.CSharp"

    @Test
    fun testXamarinFormsClassLibraryTemplate() {
        val projectName = "ClassLibrary"

//        doCoreTest(templateId, projectName) { project ->
//            checkSwea(project)
//            checkSelectedRunConfigurationExecutionNotAllowed(project)
//        }
    }
}