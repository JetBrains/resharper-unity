package com.jetbrains.rider.plugins.unity.debugger.visualizers

import com.google.gson.Gson
import com.intellij.openapi.project.Project
import com.intellij.openapi.rd.createNestedDisposable
import com.intellij.openapi.util.NlsContexts
import com.intellij.ui.ErrorLabel
import com.intellij.ui.components.JBLoadingPanel
import com.intellij.ui.components.JBPanel
import com.intellij.ui.components.panels.VerticalLayout
import com.intellij.util.ui.ImageUtil
import com.intellij.xdebugger.XDebugSession
import com.intellij.xdebugger.XDebuggerManager
import com.intellij.xdebugger.evaluation.XDebuggerEvaluator
import com.intellij.xdebugger.frame.XValue
import com.intellij.xdebugger.frame.XValueNode
import com.intellij.xdebugger.frame.XValuePlace
import com.intellij.xdebugger.impl.ui.tree.nodes.XValueNodeImpl
import com.jetbrains.rd.framework.RdTaskResult
import com.jetbrains.rd.ide.model.ValuePropertiesModelBase
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.printlnError
import com.jetbrains.rd.util.reactive.adviseOnce
import com.jetbrains.rider.RiderEnvironment
import com.jetbrains.rider.debugger.DotNetValue
import com.jetbrains.rider.debugger.visualizers.RiderDebuggerPresenterTab
import com.jetbrains.rider.debugger.visualizers.RiderDebuggerValuePresenter
import com.jetbrains.rider.model.debuggerWorker.ComputeObjectPropertiesArg
import com.jetbrains.rider.model.debuggerWorker.FailedObjectProperties
import com.jetbrains.rider.model.debuggerWorker.ObjectPropertiesProxy
import kotlinx.coroutines.ExperimentalCoroutinesApi
import org.intellij.images.editor.impl.ImageEditorManagerImpl.createImageEditorUI
import java.awt.BorderLayout
import java.awt.GraphicsConfiguration
import java.awt.GraphicsEnvironment
import java.awt.Rectangle
import java.awt.geom.AffineTransform
import java.awt.image.BufferedImage
import java.awt.image.ColorModel
import javax.swing.JComponent
import javax.swing.JLabel
import javax.swing.JPanel
import javax.swing.SwingConstants

class UnityDebuggerTexturePresenter : RiderDebuggerValuePresenter {

    @Suppress("PropertyName")
    data class TextureInfo(val Pixels: List<Int>,
                           val Width: Int,
                           val Height: Int,
                           val OriginalWidth: Int,
                           val OriginalHeight: Int,
                           val GraphicsTextureFormat: String,
                           val TextureFormat: String
    )

    override fun isApplicable(node: XValueNode, properties: ObjectPropertiesProxy, place: XValuePlace, session: XDebugSession): Boolean {
        return properties.instanceType.definitionTypeFullName == "UnityEngine.Texture2D"
    }

    override fun getPriority(): Int {
        return 0
    }

    override fun shouldIgnorePropertiesReevaluation(
        node: XValueNode,
        properties: ObjectPropertiesProxy,
        place: XValuePlace,
        session: XDebugSession
    ): Boolean {
        return true
    }


    override fun createTabs(
        node: XValueNode,
        properties: ObjectPropertiesProxy,
        place: XValuePlace,
        session: XDebugSession,
        lifetime: Lifetime
    ): List<RiderDebuggerPresenterTab> {
        val parentPanel = JBPanel<JBPanel<*>>(VerticalLayout(5))
        val jbLoadingPanel = JBLoadingPanel(BorderLayout(), lifetime.createNestedDisposable())
        parentPanel.add(jbLoadingPanel)
        jbLoadingPanel.startLoading()
        parentPanel.revalidate()
        parentPanel.repaint()

        val bundledFile = RiderEnvironment.getBundledFile(
            "JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Presentation.Texture.dll",
            pluginClass = javaClass
        )

        val evaluationRequest = "System.Reflection.Assembly.LoadFile(@\"${bundledFile.absolutePath}\")"
        val project = session.project
        evaluate(project, evaluationRequest, lifetime,
            successfullyEvaluated = { evaluateTextureAndShow(node, project, jbLoadingPanel, parentPanel, lifetime) },
            evaluationFailed = {
                showErrorMessage(
                    jbLoadingPanel,
                    parentPanel,
                    "Can't load texture helpers dll:\n$it"
                )
            })

        val name = (node as XValueNodeImpl).rawValue!!
        return listOf(RiderDebuggerPresenterTab(name, name, parentPanel, null))
    }

    private fun evaluateTextureAndShow(node: XValueNode,
                                       project: Project,
                                       jbLoadingPanel: JBLoadingPanel,
                                       parentPanel: JBPanel<JBPanel<*>>,
                                       lifetime: Lifetime) {
        val nodeName = (node as XValueNodeImpl).name!!
        val evaluationRequest = "JetBrains.Debugger.Worker.Plugins.Unity.Presentation.Texture.UnityTextureAdapter.GetPixelsInString($nodeName as UnityEngine.Texture2D)"
        evaluate(project, evaluationRequest, lifetime,
            successfullyEvaluated = { showTexture(it, jbLoadingPanel, parentPanel) },
            evaluationFailed = { showErrorMessage(jbLoadingPanel, parentPanel, "Can't get texture debug information:\n$it") })
    }

    private fun showTexture(it: ValuePropertiesModelBase,
                            jbLoadingPanel: JBLoadingPanel,
                            parentPanel: JBPanel<JBPanel<*>>) {
        val textureInfo = parseTextureEvaluationResult(it)

        if (textureInfo == null)
            showErrorMessage(jbLoadingPanel, parentPanel, "Can't parse texture info")
        else {
            val texturePanel = createPanelWithImage(textureInfo)
            jbLoadingPanel.stopLoading()
            parentPanel.remove(jbLoadingPanel)

            parentPanel.apply {
                add(JLabel("Texture Size: ${textureInfo.OriginalWidth}x${textureInfo.OriginalHeight}"))
                add(JLabel("Texture Format: ${textureInfo.TextureFormat}"))
                add(JLabel("Texture Graphics Format: ${textureInfo.GraphicsTextureFormat}"))
            }
            parentPanel.add(texturePanel)
        }
        parentPanel.revalidate()
        parentPanel.repaint()
    }

    private fun parseTextureEvaluationResult(it: ValuePropertiesModelBase): TextureInfo? {
        val json = it.value[0].value
        return Gson().fromJson(json, TextureInfo::class.java)
    }

    private fun showErrorMessage(jbLoadingPanel: JBLoadingPanel,
                                 parentPanel: JBPanel<JBPanel<*>>,
                                 @NlsContexts.Label errorMessage: String) {
        jbLoadingPanel.stopLoading()
        parentPanel.remove(jbLoadingPanel)
        parentPanel.add(ErrorLabel(errorMessage).apply {
            verticalAlignment = SwingConstants.TOP
            horizontalAlignment = SwingConstants.LEFT
        })
        printlnError(errorMessage)
    }


    @Suppress("INACCESSIBLE_TYPE")
    private fun createPanelWithImage(textureInfo: TextureInfo): JPanel {

        val defaultConfiguration = GraphicsEnvironment.getLocalGraphicsEnvironment().defaultScreenDevice.defaultConfiguration
        val dummyGraphicsConfiguration = object : GraphicsConfiguration() {
            override fun getDevice() = defaultConfiguration.device
            override fun getColorModel() = ColorModel.getRGBdefault()
            override fun getColorModel(transparency: Int) = ColorModel.getRGBdefault()
            override fun getDefaultTransform() = AffineTransform()
            override fun getNormalizingTransform() = AffineTransform()
            override fun getBounds() = Rectangle(0, 0, textureInfo.Width, textureInfo.Height)
        }

        val bufferedImage = ImageUtil.createImage(dummyGraphicsConfiguration,
            textureInfo.Width, textureInfo.Height, BufferedImage.TYPE_INT_RGB)

        for (y in 0 until textureInfo.Height) {
            for (x in 0 until textureInfo.Width) {
                val toInt = textureInfo.Pixels[y * textureInfo.Height + x]
                bufferedImage.setRGB(x, textureInfo.Height - y - 1, toInt)
            }
        }

        return createImageEditorUI(bufferedImage)
    }

    @OptIn(ExperimentalCoroutinesApi::class)
    private fun evaluate(project: Project,
                         evaluationRequest: String,
                         lifetime: Lifetime,
                         successfullyEvaluated: (value: ValuePropertiesModelBase) -> Unit,
                         evaluationFailed: (errorMessage: String) -> Unit) {
        val evaluator = XDebuggerManager.getInstance(project).currentSession!!.currentStackFrame!!.evaluator!!

        evaluator.evaluate(evaluationRequest,
            object : XDebuggerEvaluator.XEvaluationCallback {
                override fun errorOccurred(errorMessage: String) = evaluationFailed(errorMessage)
                override fun evaluated(loadDllresult: XValue) {
                    if (loadDllresult is DotNetValue) {
                        loadDllresult.objectProxy.computeObjectProperties.start(
                            lifetime,
                            ComputeObjectPropertiesArg(allowInvoke = true,
                                allowCrossThread = true,
                                ellipsizeStrings = false,
                                ellipsizedLength = null,
                                nameAliases = emptyList(),
                                extraInfo = null,
                                allowDisabledMethodsInvoke = null)
                        ).result.adviseOnce(lifetime) {
                            when (it) {
                                is RdTaskResult.Success -> {
                                    val value = it.value
                                    if (value is FailedObjectProperties)
                                        evaluationFailed(value.value.joinToString("\n"))
                                    else
                                        successfullyEvaluated(value)
                                }
                                is RdTaskResult.Cancelled -> evaluationFailed("Cancelled $it")
                                is RdTaskResult.Fault -> evaluationFailed("Fault $it")
                            }
                        }
                    }
                }
            }, null)
    }
}