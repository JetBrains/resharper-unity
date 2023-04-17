import base.integrationTests.prepareAssemblies
import com.intellij.codeInsight.daemon.impl.IntentionsUI
import com.intellij.codeInsight.daemon.impl.ShowIntentionsPass
import com.intellij.codeInspection.LocalInspectionTool
import com.intellij.codeInspection.actions.RunInspectionAction.runInspection
import com.intellij.codeInspection.ex.InspectionToolRegistrar
import com.intellij.openapi.editor.ex.MarkupModelEx
import com.intellij.openapi.editor.impl.DocumentMarkupModel
import com.intellij.openapi.editor.impl.EditorImpl
import com.intellij.openapi.project.Project
import com.intellij.psi.PsiDocumentManager
import com.intellij.testFramework.InspectionTestUtil
import com.intellij.testFramework.enableInspectionTools
import com.intellij.testFramework.fixtures.impl.CodeInsightTestFixtureImpl
import com.intellij.util.ArrayUtilRt
import com.jetbrains.rider.editors.getPsiFile
import com.jetbrains.rider.intentions.altEnter.BulbOnGutterMarginUI
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.Test
import java.io.File

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