package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.cg

import com.intellij.codeInsight.completion.CompletionParameters
import com.intellij.codeInsight.completion.CompletionResultSet
import com.intellij.codeInsight.completion.CompletionType
import com.intellij.codeInsight.completion.WordCompletionContributor
import com.jetbrains.rider.projectView.solution

class CgCompletionContributor : WordCompletionContributor() {

    override fun fillCompletionVariants(parameters: CompletionParameters, result: CompletionResultSet) {
        // TODO: this is temporary solution until we get proper completion up and running on backend
        val project = parameters.editor.project ?: return
        val isEnabled = project.solution.customData.data["UNITY_SETTINGS_EnableShaderLabHippieCompletion"] == "True"

        if (parameters.completionType == CompletionType.BASIC && isEnabled) {
            addWordCompletionVariants(result, parameters, emptySet<String>())
        }
    }
}