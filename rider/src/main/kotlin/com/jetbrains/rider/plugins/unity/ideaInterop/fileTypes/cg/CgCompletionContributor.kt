package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.cg

import com.intellij.codeInsight.completion.*
import com.intellij.codeInsight.lookup.LookupFocusDegree
import com.intellij.codeInsight.lookup.impl.LookupImpl
import com.intellij.codeInsight.lookup.impl.LookupManagerImpl
import com.intellij.openapi.Disposable
import com.intellij.openapi.util.Disposer
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.model.unity.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution

class CgCompletionContributor : WordCompletionContributor() {

    override fun fillCompletionVariants(parameters: CompletionParameters, result: CompletionResultSet) {
        // TODO: this is temporary solution until we get proper completion up and running on backend
        val project = parameters.editor.project ?: return
        val isEnabled = project.solution.frontendBackendModel.backendSettings.enableShaderLabHippieCompletion.valueOrDefault(false)
        if (!(isEnabled && (parameters.completionType == CompletionType.BASIC)))
            return

        val completion = CompletionService.getCompletionService().currentCompletion

        val indicatorDisposable = completion as? Disposable ?: return
        if (Disposer.isDisposed(indicatorDisposable)) return

        if (!completion.isAutopopupCompletion)
            return

        val lookup = LookupManagerImpl.getActiveLookup(parameters.editor) as? LookupImpl ?: return

        lookup.lookupFocusDegree = LookupFocusDegree.SEMI_FOCUSED

        addWordCompletionVariants(result, parameters, emptySet<String>())
    }
}