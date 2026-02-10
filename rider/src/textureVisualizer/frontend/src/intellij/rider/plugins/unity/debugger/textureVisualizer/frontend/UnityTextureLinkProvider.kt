package intellij.rider.plugins.unity.debugger.textureVisualizer.frontend

import com.intellij.openapi.application.EDT
import com.intellij.openapi.project.Project
import com.intellij.openapi.ui.FrameWrapper
import com.intellij.platform.debugger.impl.shared.proxy.XDebugManagerProxy
import com.intellij.platform.debugger.impl.shared.proxy.XDebugSessionProxy
import com.intellij.util.awaitCancellationAndInvoke
import com.intellij.xdebugger.frame.XDebuggerTreeNodeHyperlink
import com.intellij.xdebugger.impl.collection.visualizer.XDebuggerNodeLinkActionProvider
import com.intellij.xdebugger.impl.frame.XDebugView
import com.intellij.xdebugger.impl.ui.tree.nodes.XValueNodeImpl
import com.jetbrains.rd.util.threading.coroutines.withLifetime
import com.jetbrains.rider.plugins.unity.UnityBundle
import intellij.rider.plugins.unity.debugger.textureVisualizer.RiderTextureAccessorId
import intellij.rider.plugins.unity.debugger.textureVisualizer.RiderTextureDataApi
import kotlinx.coroutines.CoroutineName
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import java.awt.CardLayout
import java.awt.event.MouseEvent
import javax.swing.JPanel
import com.intellij.platform.util.coroutines.childScope
import com.intellij.util.AwaitCancellationAndInvoke
import com.intellij.xdebugger.XDebuggerBundle


internal class UnityTextureLinkProvider : XDebuggerNodeLinkActionProvider {
    override suspend fun CoroutineScope.provideHyperlink(
        project: Project,
        node: XValueNodeImpl
    ): XDebuggerTreeNodeHyperlink? {
        if (!XDebugManagerProxy.getInstance().hasBackendCounterpart(node.valueContainer)) {
            return null
        }
        val session = withContext(Dispatchers.EDT) {
            XDebugView.getSessionProxy(node.tree)
        } ?: return null
        return XDebugManagerProxy.getInstance().withId(node.valueContainer, session) { valueId ->
            val accessorId = RiderTextureDataApi.getInstance().findTextureAccessor(valueId).await() ?: return@withId null
            object : XDebuggerTreeNodeHyperlink(XDebuggerBundle.message("node.test.show.full.value")) {
                override fun onClick(event: MouseEvent?) {
                    this@provideHyperlink.launch {
                        showDialog(session, accessorId)
                    }
                }
            }
        }
    }

    @OptIn(AwaitCancellationAndInvoke::class)
    private suspend fun CoroutineScope.showDialog(
        session: XDebugSessionProxy,
        accessorId: RiderTextureAccessorId
    ) {
        val componentScope = childScope("${coroutineContext[CoroutineName.Key] ?: "debugger suspend scope"} (limited to a window)")
        withContext(Dispatchers.EDT) {
            //TODO(Korovin): Check lifetime here
            withLifetime { lifetime ->
                val project = session.project
                val component = JPanel(CardLayout())
                component.add(
                    UnityTextureCustomComponentEvaluator.createTextureDebugView(
                        session,
                        accessorId,
                        lifetime
                    )
                )
                val frame = FrameWrapper(
                    project = project,
                    dimensionKey = "texture-debugger",
                    title = UnityBundle.message("debugging.texture.preview.title"),
                    isDialog = true,
                    component = component,
                    coroutineScope = componentScope
                )
                frame.apply {
                    awaitCancellationAndInvoke {
                        // outer scope canceled -> caused by change in program suspend state;
                        // IDE should close the dialog explicitly
                        // as the dialog is not disposed of yet, lambda in setOnCloseHandler will be called
                        close()
                    }
                    closeOnEsc()
                    show()
                }
            }
        }
    }
}
