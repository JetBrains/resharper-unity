package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.Disposable
import com.intellij.openapi.actionSystem.ActionManager
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.CommonDataKeys
import com.intellij.openapi.actionSystem.DefaultActionGroup
import com.intellij.openapi.actionSystem.impl.SimpleDataContext
import com.intellij.openapi.application.EDT
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.editor.markup.InspectionWidgetActionProvider
import com.intellij.openapi.project.Project
import com.intellij.openapi.rd.createLifetime
import com.intellij.ui.awt.RelativePoint
import com.jetbrains.rd.util.reactive.IProperty
import com.jetbrains.rd.util.reactive.Property
import com.jetbrains.rdclient.document.getDocumentId
import com.jetbrains.rider.editors.resolveContextWidget.RiderResolveContextWidget
import com.jetbrains.rider.editors.resolveContextWidget.WidgetAction
import com.jetbrains.rider.plugins.unity.UnityProjectLifetimeService
import com.jetbrains.rider.plugins.unity.model.frontendBackend.AutoShaderContextData
import com.jetbrains.rider.plugins.unity.model.frontendBackend.SelectShaderContextDataInteraction
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ShaderContextData
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ShaderContextDataBase
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle
import com.jetbrains.rider.projectView.solution
import icons.UnityIcons
import kotlinx.coroutines.CoroutineStart
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import org.jetbrains.annotations.Nls

internal class ShaderWidgetActionProvider : InspectionWidgetActionProvider {
    override fun createAction(editor: Editor): AnAction? {
        editor.project ?: return null
        return ActionManager.getInstance().getAction("ShaderWidgetAction")
    }
}

class ShaderWidgetAction : WidgetAction("ShaderWidgetProvider"){
    override fun updateInternal(e: AnActionEvent, widget: RiderResolveContextWidget) {
        val editor = e.getData(CommonDataKeys.EDITOR) ?: return
        val shaderWidget = widget as? ShaderWidget ?: return
        if (editor.isViewer) {
            e.presentation.isEnabledAndVisible = false
            return
        }

        e.presentation.text = UnityUIBundle.message("shader.inspection.widget.text", shaderWidget.text.value)    }
}


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
                text.set(UnityUIBundle.message("auto"))
                toolTipText = UnityUIBundle.message("default.file.and.symbol.context")
            }
            else {
                text.set(getContextPresentation(it))
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

    override fun showPopup(point: RelativePoint) {
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
                popup.show(point)

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