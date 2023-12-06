package com.jetbrains.rider.plugins.unity.debugger.valueEvaluators

import com.intellij.openapi.diagnostic.thisLogger
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.project.Project
import com.intellij.openapi.rd.createNestedDisposable
import com.intellij.openapi.ui.FrameWrapper
import com.intellij.openapi.util.Disposer
import com.intellij.openapi.util.NlsContexts
import com.intellij.ui.ErrorLabel
import com.intellij.ui.components.JBLoadingPanel
import com.intellij.ui.components.JBPanel
import com.intellij.ui.components.panels.VerticalLayout
import com.intellij.util.ui.ImageUtil
import com.intellij.xdebugger.XDebugSession
import com.intellij.xdebugger.XDebuggerManager
import com.intellij.xdebugger.frame.XValueNode
import com.intellij.xdebugger.frame.XValuePlace
import com.jetbrains.rd.platform.util.toPromise
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.printlnError
import com.jetbrains.rd.util.reactive.valueOrThrow
import com.jetbrains.rider.RiderEnvironment
import com.jetbrains.rider.debugger.DotNetStackFrame
import com.jetbrains.rider.debugger.IDotNetValue
import com.jetbrains.rider.debugger.evaluators.RiderCustomComponentEvaluator
import com.jetbrains.rider.debugger.visualizers.RiderDebuggerValuePresenter
import com.jetbrains.rider.model.debuggerWorker.AdditionalActionsRequestParameter
import com.jetbrains.rider.model.debuggerWorker.ObjectPropertiesBase
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.model.debuggerWorker.UnityTextureAdditionalAction
import com.jetbrains.rider.plugins.unity.model.debuggerWorker.UnityTextureAdditionalActionParams
import com.jetbrains.rider.plugins.unity.model.debuggerWorker.UnityTextureInfo
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import org.intellij.images.editor.impl.ImageEditorManagerImpl
import java.awt.*
import java.awt.event.MouseEvent
import java.awt.geom.AffineTransform
import java.awt.image.BufferedImage
import java.awt.image.ColorModel
import javax.swing.JComponent
import javax.swing.JPanel
import javax.swing.SwingConstants


class UnityTextureCustomComponentEvaluator(node: XValueNode,
                                           properties: ObjectPropertiesBase,
                                           session: XDebugSession,
                                           place: XValuePlace,
                                           presenters: List<RiderDebuggerValuePresenter>,
                                           lifetime: Lifetime,
                                           onPopupBeingClicked: () -> Unit,
                                           shouldIgnorePropertiesComputation: () -> Boolean,
                                           shouldUpdatePresentation: Boolean,
                                           dotNetValue: IDotNetValue,
                                           actionName: String) : RiderCustomComponentEvaluator(node, properties, session, place, presenters,
                                                                                               lifetime,
                                                                                               onPopupBeingClicked,
                                                                                               shouldIgnorePropertiesComputation,
                                                                                               shouldUpdatePresentation, dotNetValue,
                                                                                               actionName) {


    override fun isShowValuePopup(): Boolean {
        return true
    }

    override fun createComponent(fullValue: String?): JComponent? {
        error("Not suppoerted")
    }

    override fun startEvaluation(callback: XFullValueEvaluationCallback) {
        error("Not suppoerted")
    }

    override fun show(event: MouseEvent, project: Project, editor: Editor?) {
        val shownPanelLifetimeDefinition = lifetime.createNested()

        val component = JPanel(CardLayout())
        val panel = FrameWrapper(
            project = project,
            dimensionKey = "texture-debugger",
            title = UnityBundle.message("debugging.texture.preview.title"),
            isDialog = true,
            component = component,
        ).apply {
            closeOnEsc()
        }

        component.add(createTextureDebugView(dotNetValue, session, shownPanelLifetimeDefinition, project))
        Disposer.register(panel) { shownPanelLifetimeDefinition.terminate() }
        panel.show()
        shownPanelLifetimeDefinition.onTermination { panel.close() }
    }

    companion object {
        private val LOG = thisLogger()
        @Suppress("LABEL_NAME_CLASH")
        fun createTextureDebugView(dotNetValue: IDotNetValue,
                                   session: XDebugSession,
                                   lifetime: Lifetime,
                                   project: Project): JComponent {
            val stagedActivity = TextureDebuggerCollector.createTextureDebuggingActivity(session.project)

            lifetime.onTerminationIfAlive {
                TextureDebuggerCollector.finishActivity(stagedActivity, ExecutionResult.Terminated)
            }

            val parentPanel = JBPanel<JBPanel<*>>(BorderLayout())

            val jbLoadingPanel = JBLoadingPanel(BorderLayout(), lifetime.createNestedDisposable())
            parentPanel.add(jbLoadingPanel)
            jbLoadingPanel.startLoading()
            parentPanel.revalidate()
            parentPanel.repaint()


            val bundledFile = RiderEnvironment.getBundledFile(
                "JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Presentation.Texture.dll",
                pluginClass = this::class.java
            )

            TextureDebuggerCollector.registerStageStarted(stagedActivity, StageType.REQUEST_ADDITIONAL_ACTIONS)
            val timeoutForAdvanceUnityEvaluation = project.solution.frontendBackendModel.backendSettings.forcedTimeoutForAdvanceUnityEvaluation.valueOrThrow

            val stackFrame = XDebuggerManager.getInstance(project).currentSession!!.currentStackFrame!! as DotNetStackFrame
            stackFrame.context.getObjectAdditionalActions
                .start(lifetime, AdditionalActionsRequestParameter(stackFrame.frameProxy.id, dotNetValue.objectProxy.id))
                .toPromise()
                .onSuccess { additionalActions ->
                    val unityTextureAdditionalAction = additionalActions.filterIsInstance<UnityTextureAdditionalAction>().firstOrNull()
                    TextureDebuggerCollector.registerStageStarted(stagedActivity, StageType.TEXTURE_PIXELS_REQUEST)
                    unityTextureAdditionalAction?.evaluateTexture
                        ?.start(lifetime, UnityTextureAdditionalActionParams(bundledFile.absolutePath, timeoutForAdvanceUnityEvaluation))
                        ?.toPromise()
                        ?.onSuccess {unityTextureAdditionalActionResult ->
                            if(unityTextureAdditionalActionResult.isTerminated)
                                return@onSuccess //if lifetime isNotAlive - window will be closed automatically

                            val errorMessage = unityTextureAdditionalActionResult.error //already localized error message from debugger worker
                            if (!errorMessage.isNullOrEmpty()) {
                                showErrorMessage(
                                    jbLoadingPanel,
                                    parentPanel,
                                    errorMessage
                                )
                                TextureDebuggerCollector.finishActivity(stagedActivity, ExecutionResult.Failed)
                            }
                            else {
                                val unityTextureInfo = unityTextureAdditionalActionResult.unityTextureInfo
                                if (unityTextureInfo == null)
                                    showErrorMessage(
                                        jbLoadingPanel,
                                        parentPanel,
                                        UnityBundle.message("debugging.cannot.get.texture.debug.information", "textureInfo is null")
                                    )
                                else {
                                    TextureDebuggerCollector.registerStageStarted(stagedActivity, StageType.PREPARE_TEXTURE_PIXELS_TO_SHOW,
                                                                                  unityTextureInfo)
                                    showTexture(unityTextureInfo, jbLoadingPanel, parentPanel)
                                    TextureDebuggerCollector.finishActivity(stagedActivity, ExecutionResult.Succeed)
                                }
                            }
                        }
                        ?.onError{
                            showErrorMessage(
                                jbLoadingPanel,
                                parentPanel,
                                UnityBundle.message("debugging.cannot.get.texture.debug.information", it)
                            )
                        }

                }
                .onError {
                    showErrorMessage(
                        jbLoadingPanel,
                        parentPanel,
                        UnityBundle.message("debugging.cannot.get.texture.debug.information", it)
                    )

                    TextureDebuggerCollector.finishActivity(stagedActivity, ExecutionResult.Failed)
                }

            return parentPanel
        }

        private fun showTexture(textureInfo: UnityTextureInfo,
                                jbLoadingPanel: JBLoadingPanel,
                                parentPanel: JBPanel<JBPanel<*>>) {
            LOG.trace("Preparing texture to show:\"${textureInfo.textureName}\"")

            val texturePanel = createPanelWithImage(textureInfo)
            jbLoadingPanel.stopLoading()

            texturePanel.background = parentPanel.background
            parentPanel.apply {
                remove(jbLoadingPanel)
                layout = VerticalLayout(5)
                add(texturePanel)
            }

            parentPanel.revalidate()
            parentPanel.repaint()
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
            parentPanel.revalidate()
            parentPanel.repaint()
            printlnError(errorMessage)
        }


        @Suppress("INACCESSIBLE_TYPE")
        private fun createPanelWithImage(textureInfo: UnityTextureInfo): JPanel {

            val defaultConfiguration = GraphicsEnvironment.getLocalGraphicsEnvironment().defaultScreenDevice.defaultConfiguration
            val dummyGraphicsConfiguration = object : GraphicsConfiguration() {
                override fun getDevice() = defaultConfiguration.device
                override fun getColorModel() = ColorModel.getRGBdefault()
                override fun getColorModel(transparency: Int) = ColorModel.getRGBdefault()
                override fun getDefaultTransform() = AffineTransform()
                override fun getNormalizingTransform() = AffineTransform()
                override fun getBounds() = Rectangle(0, 0, textureInfo.width, textureInfo.height)
            }

            val textureFormat = if (textureInfo.hasAlphaChannel) BufferedImage.TYPE_INT_ARGB_PRE else BufferedImage.TYPE_INT_RGB

            val bufferedImage = ImageUtil.createImage(dummyGraphicsConfiguration,
                                                      textureInfo.width, textureInfo.height, textureFormat)

            for (y in 0 until textureInfo.height) {
                for (x in 0 until textureInfo.width) {
                    val toInt = textureInfo.pixels[y * textureInfo.height + x]
                    bufferedImage.setRGB(x, textureInfo.height - y - 1, toInt)
                }
            }

            return ImageEditorManagerImpl.createImageEditorUI(bufferedImage,
                                                              "  ${textureInfo.textureName}  ${textureInfo.graphicsTextureFormat}")
        }
    }
}