package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.util.NlsActions
import com.jetbrains.rd.util.reactive.IProperty
import com.jetbrains.rider.plugins.unity.model.frontendBackend.SelectShaderContextDataInteraction
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ShaderContextData
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle

abstract class AbstractShaderContextSwitchAction(private val interaction: SelectShaderContextDataInteraction, private val currentContext: IProperty<ShaderContextData?>, @NlsActions.ActionText name: String) : AnAction(name) {
    abstract val data: ShaderContextData?
    protected abstract val index: Int
    var onPerformed: Runnable? = null

    final override fun actionPerformed(event: AnActionEvent) {
        currentContext.value = data
        interaction.selectItem.fire(index)
        onPerformed?.run()
    }
}

class ShaderContextSwitchAction(interaction: SelectShaderContextDataInteraction, override val index: Int, currentContext: IProperty<ShaderContextData?>)
    : AbstractShaderContextSwitchAction(interaction, currentContext, interaction.items[index].name) {
    override val data = interaction.items[index]
}

class ShaderAutoContextSwitchAction(interaction: SelectShaderContextDataInteraction, currentContext: IProperty<ShaderContextData?>)
    : AbstractShaderContextSwitchAction(interaction, currentContext, UnityUIBundle.message("auto")) {
    override val data: ShaderContextData? = null
    override val index: Int = -1
}