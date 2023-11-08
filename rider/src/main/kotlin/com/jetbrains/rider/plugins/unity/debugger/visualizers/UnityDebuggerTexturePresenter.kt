package com.jetbrains.rider.plugins.unity.debugger.visualizers

import com.google.gson.Gson
import com.intellij.internal.statistic.StructuredIdeActivity
import com.intellij.openapi.diagnostic.thisLogger
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
import com.jetbrains.rd.framework.RdTaskResult
import com.jetbrains.rd.ide.model.ValuePropertiesModelBase
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.isNotAlive
import com.jetbrains.rd.util.printlnError
import com.jetbrains.rd.util.reactive.adviseOnce
import com.jetbrains.rd.util.reactive.valueOrThrow
import com.jetbrains.rider.RiderEnvironment
import com.jetbrains.rider.debugger.DotNetDebugProcess
import com.jetbrains.rider.debugger.DotNetValue
import com.jetbrains.rider.debugger.IDotNetValue
import com.jetbrains.rider.debugger.visualizers.RiderDebuggerPresenterTab
import com.jetbrains.rider.debugger.visualizers.RiderDebuggerValuePresenter
import com.jetbrains.rider.model.debuggerWorker.*
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import kotlinx.coroutines.ExperimentalCoroutinesApi
import org.intellij.images.editor.impl.ImageEditorManagerImpl
import java.awt.BorderLayout
import java.awt.GraphicsConfiguration
import java.awt.GraphicsEnvironment
import java.awt.Rectangle
import java.awt.geom.AffineTransform
import java.awt.image.BufferedImage
import java.awt.image.ColorModel
import javax.swing.*

class UnityDebuggerTexturePresenter : RiderDebuggerValuePresenter {

    private var texturePresentationActivity: StructuredIdeActivity? = null

    @Suppress("PropertyName")
    data class TextureInfo(val Pixels: List<Int>,
                           val Width: Int,
                           val Height: Int,
                           val OriginalWidth: Int,
                           val OriginalHeight: Int,
                           val GraphicsTextureFormat: String,
                           val TextureName: String,
                           val HasAlphaChannel: Boolean
    )

    companion object {
        private val LOG = thisLogger()
    }

    override fun isApplicable(node: XValueNode, properties: ObjectPropertiesProxy, place: XValuePlace, session: XDebugSession): Boolean =
        (session.debugProcess as? DotNetDebugProcess)?.isIl2Cpp == false
        && !properties.valueFlags.contains(ValueFlags.IsNull)
        && (properties.instanceType.definitionTypeFullName == "UnityEngine.Texture2D"
            || properties.instanceType.definitionTypeFullName == "UnityEngine.RenderTexture")


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


    override fun createTabs(value: IDotNetValue,
                            node: XValueNode,
                            properties: ObjectPropertiesBase,
                            stringPresentation: String?,
                            place: XValuePlace,
                            session: XDebugSession,
                            lifetime: Lifetime): List<RiderDebuggerPresenterTab> {
        texturePresentationActivity = TextureDebuggerCollector.texturePresentationRequestStarted(session.project)
        lifetime.onTerminationIfAlive{
            finishMasterActivity(ExecutionResult.Terminated)
        }

        val parentPanel = JBPanel<JBPanel<*>>(BorderLayout())
        val jbLoadingPanel = JBLoadingPanel(BorderLayout(), lifetime.createNestedDisposable())
        parentPanel.add(jbLoadingPanel)
        jbLoadingPanel.startLoading()
        parentPanel.revalidate()
        parentPanel.repaint()

        val loadDllActivity = TextureDebuggerCollector.loadDllStarted(session.project, texturePresentationActivity)
        val bundledFile = RiderEnvironment.getBundledFile(
            "JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Presentation.Texture.dll",
            pluginClass = javaClass
        )
        LOG.trace("Loading texture debug helper dll:\"${bundledFile.absolutePath}\"")
        val evaluationRequest = "System.Reflection.Assembly.LoadFile(@\"${bundledFile.absolutePath}\")"
        val project = session.project
        evaluate(project, evaluationRequest, lifetime, null,
                 successfullyEvaluated = {
                     TextureDebuggerCollector.finishActivity(loadDllActivity, ExecutionResult.Succeed)
                     if (lifetime.isNotAlive) return@evaluate
                     evaluateTextureAndShow(project, jbLoadingPanel, parentPanel, lifetime, value)
                 },
                 evaluationFailed = {
                     showErrorMessage(
                         jbLoadingPanel,
                         parentPanel,
                         UnityBundle.message("debugging.cannot.load.texture.dll.label", it)
                     )
                     failChildActivity(loadDllActivity)
                 })

        val name = UnityBundle.message("debugging.texture.preview.title")
        return listOf(RiderDebuggerPresenterTab(name, name, parentPanel, null))
    }

    private fun evaluateTextureAndShow(
        project: Project,
        jbLoadingPanel: JBLoadingPanel,
        parentPanel: JBPanel<JBPanel<*>>,
        lifetime: Lifetime,
        value: IDotNetValue) {

        val evaluationFullNameActivity = TextureDebuggerCollector.evaluateValueFullNameStarted(project, texturePresentationActivity)

        if (value is XValue)
            value.calculateEvaluationExpression()
                .onSuccess { expr ->
                    run {
                        TextureDebuggerCollector.finishActivity(evaluationFullNameActivity, ExecutionResult.Succeed)
                        if (lifetime.isNotAlive) return@onSuccess
                        continueTextureEvaluation(expr.expression, project, lifetime, jbLoadingPanel, parentPanel)
                    }
                }
                .onError {
                    TextureDebuggerCollector.finishActivity(evaluationFullNameActivity, false)
                    showErrorMessage(jbLoadingPanel, parentPanel,
                                     UnityBundle.message("debugging.cannot.get.texture.debug.information", it))
                    failChildActivity(evaluationFullNameActivity)
                }
        else {
            TextureDebuggerCollector.finishActivity(evaluationFullNameActivity, false)
            showErrorMessage(jbLoadingPanel, parentPanel,
                             UnityBundle.message("debugging.cannot.get.texture.debug.information", value.toString()))
            failChildActivity(evaluationFullNameActivity)
        }
    }

    private fun continueTextureEvaluation(nodeName: String,
                                          project: Project,
                                          lifetime: Lifetime,
                                          jbLoadingPanel: JBLoadingPanel,
                                          parentPanel: JBPanel<JBPanel<*>>) {

        val texturePixelsRequestActivity = TextureDebuggerCollector.requestTexturePixelsStarted(project, texturePresentationActivity)
        LOG.trace("Evaluating texture:\"${nodeName}\"")
        val timeoutForAdvanceUnityEvaluation = project.solution.frontendBackendModel.backendSettings.forcedTimeoutForAdvanceUnityEvaluation.valueOrThrow

        val evaluationRequest = "JetBrains.Debugger.Worker.Plugins.Unity.Presentation.Texture.UnityTextureAdapter.GetPixelsInString($nodeName as UnityEngine.Texture2D)"
        evaluate(project, evaluationRequest, lifetime, timeoutForAdvanceUnityEvaluation,
                 successfullyEvaluated = {
                     TextureDebuggerCollector.finishActivity(texturePixelsRequestActivity, ExecutionResult.Succeed)
                     if (lifetime.isNotAlive) return@evaluate
                     showTexture(it, jbLoadingPanel, parentPanel, project)
                 },
                 evaluationFailed = {
                     showErrorMessage(jbLoadingPanel, parentPanel,
                                      UnityBundle.message("debugging.cannot.get.texture.debug.information", it))
                     failChildActivity(texturePixelsRequestActivity)
                 })
    }

    private fun showTexture(it: ValuePropertiesModelBase,
                            jbLoadingPanel: JBLoadingPanel,
                            parentPanel: JBPanel<JBPanel<*>>,
                            project: Project) {

        try {
            val json = it.value[0].value

            val textureInfo = Gson().fromJson(json, TextureInfo::class.java)
            val prepareTextureToShowStarted =
                TextureDebuggerCollector.prepareTextureToShowStarted(project, texturePresentationActivity, textureInfo.Width,
                                                                     textureInfo.Height)

            if (textureInfo == null) {
                showErrorMessage(jbLoadingPanel, parentPanel, UnityBundle.message("debugging.cannot.parse.texture.info", json))
                failChildActivity(prepareTextureToShowStarted)
            }
            else {
                LOG.trace("Preparing texture to show:\"${textureInfo}\"")

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
                TextureDebuggerCollector.finishActivity(prepareTextureToShowStarted, ExecutionResult.Succeed)
                finishMasterActivity(ExecutionResult.Succeed)
            }
        }
        catch (e: Throwable) {
            showErrorMessage(jbLoadingPanel, parentPanel, UnityBundle.message("debugging.cannot.parse.texture.info", it))
            finishMasterActivity(ExecutionResult.Failed)
        }
    }

    private fun finishMasterActivity(isSucceed: ExecutionResult) {

        if(texturePresentationActivity == null)
            return

        val activity = texturePresentationActivity!!
        texturePresentationActivity = null;
        TextureDebuggerCollector.finishActivity(activity, isSucceed)
    }

    private fun failChildActivity(childActivity: StructuredIdeActivity?) {
        TextureDebuggerCollector.finishActivity(childActivity, ExecutionResult.Failed)
        finishMasterActivity(ExecutionResult.Failed)
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

        val textureFormat = if (textureInfo.HasAlphaChannel) BufferedImage.TYPE_INT_ARGB_PRE else BufferedImage.TYPE_INT_RGB

        val bufferedImage = ImageUtil.createImage(dummyGraphicsConfiguration,
                                                  textureInfo.Width, textureInfo.Height, textureFormat)

        for (y in 0 until textureInfo.Height) {
            for (x in 0 until textureInfo.Width) {
                val toInt = textureInfo.Pixels[y * textureInfo.Height + x]
                bufferedImage.setRGB(x, textureInfo.Height - y - 1, toInt)
            }
        }

        return ImageEditorManagerImpl.createImageEditorUI(bufferedImage,
                                                          "  ${textureInfo.TextureName}  ${textureInfo.GraphicsTextureFormat}")
    }

    @OptIn(ExperimentalCoroutinesApi::class)
    private fun evaluate(project: Project,
                         evaluationRequest: String,
                         lifetime: Lifetime,
                         evaluationTimeoutMs: Int?,
                         successfullyEvaluated: (value: ValuePropertiesModelBase) -> Unit,
                         evaluationFailed: (errorMessage: String) -> Unit) {
        val evaluator = XDebuggerManager.getInstance(project).currentSession!!.currentStackFrame!!.evaluator!!

        evaluator.evaluate(evaluationRequest,
                           object : XDebuggerEvaluator.XEvaluationCallback {
                               override fun errorOccurred(errorMessage: String) = evaluationFailed(errorMessage)
                               override fun evaluated(evaluationResult: XValue) {
                                   if (evaluationResult is DotNetValue) {
                                       evaluationResult.objectProxy.computeObjectProperties.start(
                                           lifetime,
                                           ComputeObjectPropertiesArg(allowInvoke = true,
                                                                      allowCrossThread = true,
                                                                      ellipsizeStrings = false,
                                                                      ellipsizedLength = null,
                                                                      nameAliases = emptyList(),
                                                                      extraInfo = null,
                                                                      allowDisabledMethodsInvoke = true,
                                                                      forcedEvaluationTimeoutMs = evaluationTimeoutMs)
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