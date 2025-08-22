package com.jetbrains.rider.unity.test.framework.api

import com.intellij.openapi.rd.util.lifetime
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.facades.solution.SolutionApiFacade
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.framework.testData.TestDataStorage
import com.jetbrains.rider.test.scriptingApi.requestFindUsages
import com.jetbrains.rider.test.scriptingApi.setCaretAfterWord
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import java.time.Duration

context(solutionApiFacade: SolutionApiFacade, testDataStorage: TestDataStorage)
fun doFindUsagesTest(relPath:String, word:String) {
    waitAndPump(solutionApiFacade.project.lifetime, { solutionApiFacade.project.solution.frontendBackendModel.isDeferredCachesCompletedOnce.valueOrDefault(false)}, Duration.ofSeconds(10), { "Deferred caches are not completed" })
    withOpenedEditor(relPath) {
        setCaretAfterWord(word)
        val text = requestFindUsages(solutionApiFacade.activeSolutionDirectory, true)
        executeWithGold(testDataStorage.testGoldFile) { printStream ->
            printStream.print(text)
        }
    }
}