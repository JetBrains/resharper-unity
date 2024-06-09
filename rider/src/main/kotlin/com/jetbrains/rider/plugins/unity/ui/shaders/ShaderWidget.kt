package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.Disposable
import com.intellij.openapi.actionSystem.DefaultActionGroup
import com.intellij.openapi.actionSystem.impl.SimpleDataContext
import com.intellij.openapi.application.EDT
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.project.Project
import com.intellij.openapi.rd.createLifetime
import com.intellij.ui.awt.RelativePoint
import com.jetbrains.rd.util.reactive.IProperty
import com.jetbrains.rd.util.reactive.Property
import com.jetbrains.rdclient.document.getDocumentId
import com.jetbrains.rider.editors.resolveContextWidget.RiderResolveContextWidget
import com.jetbrains.rider.plugins.unity.UnityProjectLifetimeService
import com.jetbrains.rider.plugins.unity.model.frontendBackend.*
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle
import com.jetbrains.rider.projectView.solution
import icons.UnityIcons
import kotlinx.coroutines.CoroutineStart
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import org.jetbrains.annotations.Nls
import java.awt.Point


class ShaderWidget(project: Project, editor: Editor) : AbstractShaderWidget(project, editor), RiderResolveContextWidget, Disposable {
    companion object {
        @Nls
        private fun getContextPresentation(data: ShaderContextData) = if (data.startLine > 0) "${data.name}:${data.startLine}" else data.name
    }

    private val widgetLifetime = this.createLifetime()
    private val currentContextData: IProperty<ShaderContextData?> = Property(null)

    init {
        label.apply {
            icon = UnityIcons.FileTypes.ShaderLab
            text = "..."
        }
        isVisible = false

        currentContextData.advise(UnityProjectLifetimeService.getLifetime(project)) {
            if (it == null) {
                label.text = UnityUIBundle.message("auto")
                toolTipText = UnityUIBundle.message("default.file.and.symbol.context")
            }
            else {
                label.text = getContextPresentation(it)
                toolTipText = UnityUIBundle.message("file.and.symbol.context.derived.from.include.at.context", getContextPresentation(it))
            }
        }
    }

    fun setData(data: ShaderContextDataBase?) {
        when (data) {
            is AutoShaderContextData -> currentContextData.value = null
            is ShaderContextData -> currentContextData.value = data
        }
        isVisible = data != null
    }

    override fun showPopup(pointOnComponent: Point) {
        UnityProjectLifetimeService.getScope(project).launch(Dispatchers.EDT, CoroutineStart.UNDISPATCHED) {
            val id = editor.document.getDocumentId(project) ?: return@launch
            val activity = ShaderVariantEventLogger.logShowShaderContextsPopupStarted(project)
            try {
                val model = project.solution.frontendBackendModel
                val interaction = model.createSelectShaderContextInteraction.startSuspending(widgetLifetime, id)
                val actions = createActions(interaction)
                val group = DefaultActionGroup().apply {
                    addAll(actions)
                }

                val popup = ShaderContextPopup(group, SimpleDataContext.getProjectContext(project), currentContextData)
                popup.show(RelativePoint(this@ShaderWidget, pointOnComponent))

                val count = actions.count()
                activity?.finished {
                    listOf(ShaderVariantEventLogger.CONTEXT_COUNT with count)
                }
            }
            catch (t: Throwable) {
                activity?.finished {
                    listOf(ShaderVariantEventLogger.CONTEXT_COUNT with -1)
                }
                throw t
            }
        }
    }

    private fun createActions(interaction: SelectShaderContextDataInteraction): List<AbstractShaderContextSwitchAction> {
        val result = mutableListOf<AbstractShaderContextSwitchAction>(ShaderAutoContextSwitchAction(interaction, currentContextData))
        for (index in 0 until interaction.items.size) {
            result.add(ShaderContextSwitchAction(interaction, index, currentContextData))
        }
        return result
    }

    override fun dispose() {}
}