package com.jetbrains.rider.plugins.unity.debugger.valueEvaluators

import com.intellij.internal.statistic.StructuredIdeActivity
import com.intellij.openapi.application.EDT
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
import com.jetbrains.rd.util.AtomicReference
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.printlnError
import com.jetbrains.rd.util.reactive.valueOrThrow
import com.jetbrains.rider.debugger.DotNetNamedValue
import com.jetbrains.rider.debugger.DotNetStackFrame
import com.jetbrains.rider.debugger.IDotNetValue
import com.jetbrains.rider.debugger.evaluators.RiderCustomComponentEvaluator
import com.jetbrains.rider.debugger.visualizers.RiderDebuggerValuePresenter
import com.jetbrains.rider.model.debuggerWorker.ObjectPropertiesBase
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.UnityPluginScopeService
import com.jetbrains.rider.plugins.unity.model.debuggerWorker.UnityTexturePropertiesData
import com.jetbrains.rider.plugins.unity.model.debuggerWorker.UnityTextureAdditionalActionParams
import com.jetbrains.rider.plugins.unity.model.debuggerWorker.UnityTextureAdditionalActionResult
import com.jetbrains.rider.plugins.unity.model.debuggerWorker.UnityTextureInfo
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
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

        component.add(createTextureDebugView(dotNetValue, properties, session, shownPanelLifetimeDefinition, project))
        Disposer.register(panel) { shownPanelLifetimeDefinition.terminate() }
        panel.show()
        shownPanelLifetimeDefinition.onTermination { panel.close() }
    }

    companion object {
        private val LOG = thisLogger()

        private suspend fun unityTextureAdditionalActionResult(
            dotNetValue: IDotNetValue,
            unityTextureAdditionalAction: UnityTexturePropertiesData,
            lifetime: Lifetime,
            timeoutForAdvanceUnityEvaluation: Int,
            onErrorCallback: (String) -> Unit
        ): UnityTextureAdditionalActionResult? {

            val additionalActionResult: UnityTextureAdditionalActionResult

            try {
                val value = dotNetValue as DotNetNamedValue
                withContext(Dispatchers.EDT) {
                    additionalActionResult = unityTextureAdditionalAction.evaluateTexture
                        .startSuspending(lifetime, UnityTextureAdditionalActionParams(timeoutForAdvanceUnityEvaluation, value.frame.frameProxy.id))
                }
                return additionalActionResult
            }
            catch (t: Throwable) {
                onErrorCallback(t.toString())
                return null
            }
        }

        suspend fun getUnityTextureInfo(
            dotNetValue: IDotNetValue,
            properties: ObjectPropertiesBase,
            lifetime: Lifetime,
            timeoutForAdvanceUnityEvaluation: Int,
            stagedActivity: AtomicReference<StructuredIdeActivity?>?,
            errorCallback: (String) -> Unit
        ): UnityTextureInfo? {
            val unityTextureAdditionalAction = properties.additionalData.filterIsInstance<UnityTexturePropertiesData>().firstOrNull()

            if (unityTextureAdditionalAction == null) {
                errorCallback("unityTextureAdditionalAction == null")
                return null
            }

            if (stagedActivity != null)
                TextureDebuggerCollector.registerStageStarted(stagedActivity, StageType.TEXTURE_PIXELS_REQUEST)

            val unityTextureAdditionalActionResult = unityTextureAdditionalActionResult(dotNetValue, unityTextureAdditionalAction, lifetime,
                                                                                        timeoutForAdvanceUnityEvaluation, errorCallback)

            if (unityTextureAdditionalActionResult == null) {
                errorCallback("unityTextureAdditionalActionResult == null")
                return null
            }

            if (unityTextureAdditionalActionResult.isTerminated)  //Terminated in case of lifetime is not alive
                return null //if lifetime isNotAlive - window will be closed automatically

            val errorMessage = unityTextureAdditionalActionResult.error //already localized error message from debugger worker
            if (!errorMessage.isNullOrEmpty()) {
                withContext(Dispatchers.EDT) {
                    errorCallback(errorMessage)
                }
                return null
            }

            return unityTextureAdditionalActionResult.unityTextureInfo
        }

        @Suppress("LABEL_NAME_CLASH")
        fun createTextureDebugView(
            dotNetValue: IDotNetValue,
            properties: ObjectPropertiesBase,
            session: XDebugSession,
            lifetime: Lifetime,
            project: Project,
        ): JComponent {
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


            TextureDebuggerCollector.registerStageStarted(stagedActivity, StageType.REQUEST_ADDITIONAL_ACTIONS)
            val timeoutForAdvanceUnityEvaluation = project.solution.frontendBackendModel.backendSettings.forcedTimeoutForAdvanceUnityEvaluation.valueOrThrow

            val stackFrame = XDebuggerManager.getInstance(project).currentSession!!.currentStackFrame!! as DotNetStackFrame
            fun errorCallback(it: String) {
                showErrorMessage(
                    jbLoadingPanel,
                    parentPanel,
                    UnityBundle.message("debugging.cannot.get.texture.debug.information", it)
                )
                TextureDebuggerCollector.finishActivity(stagedActivity, ExecutionResult.Failed)

            }

            UnityPluginScopeService.getScope().launch {
                when (val unityTextureInfo = getUnityTextureInfo(dotNetValue, properties, lifetime,
                                                                 timeoutForAdvanceUnityEvaluation, stagedActivity, ::errorCallback)) {
                    null -> errorCallback("textureInfo is null")
                    else -> {
                        TextureDebuggerCollector.registerStageStarted(stagedActivity, StageType.PREPARE_TEXTURE_PIXELS_TO_SHOW,
                                                                      unityTextureInfo)
                        withContext(Dispatchers.EDT) {
                            showTexture(unityTextureInfo, jbLoadingPanel, parentPanel)
                        }
                        TextureDebuggerCollector.finishActivity(stagedActivity, ExecutionResult.Succeed)
                    }
                }
            }

            return parentPanel
        }

        private fun showTexture(textureInfo: UnityTextureInfo,
                                jbLoadingPanel: JBLoadingPanel,
                                parentPanel: JBPanel<JBPanel<*>>) {

            try {
                LOG.trace("Preparing texture to show:\"${textureInfo.textureName}\"")

                val texturePanel = createPanelWithImage(textureInfo)
                jbLoadingPanel.stopLoading()

                texturePanel.background = parentPanel.background
                parentPanel.apply {
                    remove(jbLoadingPanel)
                    layout = VerticalLayout(5)
                    add(texturePanel)
                }
            }
            catch (t: Throwable) {
                showErrorMessage(
                    jbLoadingPanel,
                    parentPanel,
                    UnityBundle.message("debugging.cannot.get.texture.debug.information", t)
                )
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
                    val toInt = textureInfo.pixels[y * textureInfo.width + x]
                    bufferedImage.setRGB(x, textureInfo.height - y - 1, toInt)
                }
            }

            return ImageEditorManagerImpl.createImageEditorUI(bufferedImage,
                                                              "  ${textureInfo.textureName}  ${textureInfo.graphicsTextureFormat}")
        }
    }
}