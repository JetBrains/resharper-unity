package intellij.rider.plugins.unity.debugger.textureVisualizer.frontend

import com.intellij.internal.statistic.StructuredIdeActivity
import com.intellij.openapi.application.EDT
import com.intellij.openapi.diagnostic.thisLogger
import com.intellij.openapi.rd.createNestedDisposable
import com.intellij.openapi.ui.FrameWrapper
import com.intellij.openapi.util.Disposer
import com.intellij.openapi.util.NlsContexts
import com.intellij.platform.debugger.impl.shared.proxy.XDebugSessionProxy
import com.intellij.ui.ErrorLabel
import com.intellij.ui.components.JBLoadingPanel
import com.intellij.ui.components.JBPanel
import com.intellij.ui.components.panels.VerticalLayout
import com.intellij.util.ui.ImageUtil
import com.jetbrains.rd.util.AtomicReference
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.printlnError
import com.jetbrains.rd.util.reactive.valueOrThrow
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.UnityPluginScopeService
import com.jetbrains.rider.plugins.unity.debugger.valueEvaluators.ExecutionResult
import com.jetbrains.rider.plugins.unity.debugger.valueEvaluators.StageType
import com.jetbrains.rider.plugins.unity.debugger.valueEvaluators.TextureDebuggerCollector
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import intellij.rider.plugins.unity.debugger.textureVisualizer.RiderTextureAccessorId
import intellij.rider.plugins.unity.debugger.textureVisualizer.RiderTextureDataApi
import intellij.rider.plugins.unity.debugger.textureVisualizer.UnityTextureAdditionalActionResult
import intellij.rider.plugins.unity.debugger.textureVisualizer.UnityTextureInfo
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import org.intellij.images.editor.impl.ImageEditorManagerImpl
import java.awt.BorderLayout
import java.awt.CardLayout
import java.awt.GraphicsConfiguration
import java.awt.GraphicsEnvironment
import java.awt.Rectangle
import java.awt.geom.AffineTransform
import java.awt.image.BufferedImage
import java.awt.image.ColorModel
import javax.swing.JComponent
import javax.swing.JPanel
import javax.swing.SwingConstants

object UnityTextureCustomComponentEvaluator {

    private val LOG = thisLogger()

    @OptIn(ExperimentalCoroutinesApi::class)
    private suspend fun unityTextureAdditionalActionResult(
        accessorId: RiderTextureAccessorId,
        //TODO(Korovin): This lifetime should be passed to the backend, coroutine scope?
        lifetime: Lifetime,
        timeoutForAdvanceUnityEvaluation: Int,
        onErrorCallback: (String) -> Unit
    ): UnityTextureAdditionalActionResult? {

        val additionalActionResult: UnityTextureAdditionalActionResult

        try {
            withContext(Dispatchers.EDT) {
                additionalActionResult = RiderTextureDataApi.getInstance().evaluateTexture(accessorId, timeoutForAdvanceUnityEvaluation)
            }
            return additionalActionResult
        } catch (t: Throwable) {
            onErrorCallback(t.toString())
            return null
        }
    }

    suspend fun getUnityTextureInfo(
        accessorId: RiderTextureAccessorId,
        lifetime: Lifetime,
        timeoutForAdvanceUnityEvaluation: Int,
        stagedActivity: AtomicReference<StructuredIdeActivity?>?,
        errorCallback: (String) -> Unit
    ): UnityTextureInfo? {
        if (stagedActivity != null)
            TextureDebuggerCollector.registerStageStarted(stagedActivity, StageType.TEXTURE_PIXELS_REQUEST)

        val unityTextureAdditionalActionResult = unityTextureAdditionalActionResult(
            accessorId, lifetime,
            timeoutForAdvanceUnityEvaluation, errorCallback
        )

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
        session: XDebugSessionProxy,
        accessorId: RiderTextureAccessorId,
        lifetime: Lifetime
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
        val timeoutForAdvanceUnityEvaluation =
            session.project.solution.frontendBackendModel.backendSettings.forcedTimeoutForAdvanceUnityEvaluation.valueOrThrow

        fun errorCallback(it: String) {
            showErrorMessage(
                jbLoadingPanel,
                parentPanel,
                UnityBundle.message("debugging.cannot.get.texture.debug.information", it)
            )
            TextureDebuggerCollector.finishActivity(stagedActivity, ExecutionResult.Failed)
        }

        UnityPluginScopeService.getScope().launch {
            when (val unityTextureInfo = getUnityTextureInfo(
                accessorId, lifetime,
                timeoutForAdvanceUnityEvaluation, stagedActivity, ::errorCallback
            )) {
                null -> errorCallback("textureInfo is null")
                else -> {
                    TextureDebuggerCollector.registerStageStarted(
                        stagedActivity, StageType.PREPARE_TEXTURE_PIXELS_TO_SHOW,
                        unityTextureInfo
                    )
                    withContext(Dispatchers.EDT) {
                        showTexture(unityTextureInfo, jbLoadingPanel, parentPanel)
                    }
                    TextureDebuggerCollector.finishActivity(stagedActivity, ExecutionResult.Succeed)
                }
            }
        }

        return parentPanel
    }

    private fun showTexture(
        textureInfo: UnityTextureInfo,
        jbLoadingPanel: JBLoadingPanel,
        parentPanel: JBPanel<JBPanel<*>>
    ) {

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
        } catch (t: Throwable) {
            showErrorMessage(
                jbLoadingPanel,
                parentPanel,
                UnityBundle.message("debugging.cannot.get.texture.debug.information", t)
            )
        }


        parentPanel.revalidate()
        parentPanel.repaint()
    }

    private fun showErrorMessage(
        jbLoadingPanel: JBLoadingPanel,
        parentPanel: JBPanel<JBPanel<*>>,
        @NlsContexts.Label errorMessage: String
    ) {
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

        val bufferedImage = ImageUtil.createImage(
            dummyGraphicsConfiguration,
            textureInfo.width, textureInfo.height, textureFormat
        )

        for (y in 0 until textureInfo.height) {
            for (x in 0 until textureInfo.width) {
                val toInt = textureInfo.pixels[y * textureInfo.width + x]
                bufferedImage.setRGB(x, textureInfo.height - y - 1, toInt)
            }
        }

        return ImageEditorManagerImpl.createImageEditorUI(
            bufferedImage,
            "  ${textureInfo.textureName}  ${textureInfo.graphicsTextureFormat}"
        )
    }
}