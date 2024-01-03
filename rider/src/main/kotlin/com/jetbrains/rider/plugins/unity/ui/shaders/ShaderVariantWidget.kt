package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.Disposable
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.project.Project
import com.intellij.openapi.rd.createLifetime
import com.intellij.ui.GotItTooltip
import com.intellij.ui.awt.RelativePoint
import com.jetbrains.rd.ide.editor.getExtensionSafe
import com.jetbrains.rdclient.document.textControlModel
import com.jetbrains.rider.editors.resolveContextWidget.RiderResolveContextWidget
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.shaderLab.ShaderLabFileType
import com.jetbrains.rider.plugins.unity.model.frontendBackend.RdShaderVariantExtension
import java.awt.Point
import java.net.URL


class ShaderVariantWidget(project: Project, editor: Editor, shaderVariant: RdShaderVariantExtension) : AbstractShaderWidget(project, editor), RiderResolveContextWidget, Disposable {
    private val lifetime = createLifetime()

    init {
        label.icon = ShaderLabFileType.icon

        shaderVariant.info.advise(lifetime) { info ->
            label.text = when {
                info.suppressedCount > 0 -> UnityBundle.message("shaderVariant.widget.text.with.suppressed", info.enabledCount, info.suppressedCount, info.availableCount)
                else -> UnityBundle.message("shaderVariant.widget.text", info.enabledCount, info.availableCount)
            }
        }
    }

    override fun showPopup(pointOnComponent: Point) = ShaderVariantPopup.show(project, editor, RelativePoint.getNorthWestOf(this))

    override fun addNotify() {
        editor.textControlModel?.getExtensionSafe<RdShaderVariantExtension>()
        //model.getExtension<ShaderVar>()
        super.addNotify()
        GotItTooltip("shader_variant.widget.got.it", UnityBundle.message("shaderVariant.widget.got.it.tooltip.text"), this)
            .withHeader(UnityBundle.message("shaderVariant.widget.got.it.tooltip.header"))
            .withBrowserLink(UnityBundle.message("shaderVariant.widget.got.it.tooltip.learnMore"), URL("https://jb.gg/wacf2b"))
            .show(this, GotItTooltip.TOP_MIDDLE)
    }
}