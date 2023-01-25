package base

import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.requestFindUsages
import com.jetbrains.rider.test.scriptingApi.setCaretAfterWord
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import java.time.Duration

fun BaseTestWithSolution.doFindUsagesTest(relPath:String, word:String) {
    waitAndPump(project.lifetime, { project.solution.frontendBackendModel.isDeferredCachesCompletedOnce.valueOrDefault(false)}, Duration.ofSeconds(10), { "Deferred caches are not completed" })
    withOpenedEditor(relPath) {
        setCaretAfterWord(word)
        val text = requestFindUsages(activeSolutionDirectory, true)
        executeWithGold(testGoldFile) { printStream ->
            printStream.print(text)
        }
    }
}