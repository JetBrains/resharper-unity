package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.cg

import com.intellij.codeInsight.completion.*
import com.intellij.codeInsight.lookup.impl.LookupImpl
import com.intellij.codeInsight.lookup.impl.LookupManagerImpl
import com.intellij.openapi.Disposable
import com.intellij.openapi.util.Disposer
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.projectView.solution

class CgCompletionContributor : WordCompletionContributor() {

    override fun fillCompletionVariants(parameters: CompletionParameters, result: CompletionResultSet) {
        // TODO: this is temporary solution until we get proper completion up and running on backend
        val project = parameters.editor.project ?: return
        val isEnabled = project.solution.rdUnityModel.data["UNITY_SETTINGS_EnableShaderLabHippieCompletion"] == "True"
        if (!(isEnabled && (parameters.completionType == CompletionType.BASIC)))
            return

        val completion = CompletionService.getCompletionService().currentCompletion

        val indicatorDisposable = completion as? Disposable ?: return
        if (Disposer.isDisposed(indicatorDisposable)) return

        if (!completion.isAutopopupCompletion)
            return

        val lookup = LookupManagerImpl.getActiveLookup(parameters.editor) as? LookupImpl ?: return
        lookup.focusDegree = LookupImpl.FocusDegree.SEMI_FOCUSED

        addWordCompletionVariants(result, parameters, emptySet<String>())
    }
}