package base.integrationTests

import com.intellij.xdebugger.XDebuggerManager
import com.intellij.xdebugger.XDebuggerUtil
import com.intellij.xdebugger.breakpoints.XLineBreakpoint
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.debugger.breakpoint.DotNetLineBreakpointProperties
import com.jetbrains.rider.plugins.unity.debugger.breakpoints.UnityPausepointBreakpointType
import com.jetbrains.rider.plugins.unity.debugger.breakpoints.convertToLineBreakpoint
import com.jetbrains.rider.plugins.unity.debugger.breakpoints.convertToPausepoint
import com.jetbrains.rider.test.framework.frameworkLogger
import com.jetbrains.rider.test.scriptingApi.removeAllBreakpoints
import com.jetbrains.rider.test.scriptingApi.toggleBreakpoint
import org.testng.annotations.AfterMethod

abstract class IntegrationDebuggerTestBase : IntegrationTestWithEditorBase() {
    private val pausepoits: MutableList<XLineBreakpoint<DotNetLineBreakpointProperties>> = mutableListOf()

    @AfterMethod(alwaysRun = true)
    fun removeAllUnityPausepoints() {
        pausepoits.forEach {
            convertToLineBreakpoint(project, it)
        }
        pausepoits.clear()
        removeAllBreakpoints()
    }

    fun toggleUnityPausepoint(projectFile: String, lineNumber: Int, condition: String = ""): XLineBreakpoint<DotNetLineBreakpointProperties> {
        @Suppress("UNCHECKED_CAST")
        val breakpoint = toggleBreakpoint(projectFile, lineNumber)
            as XLineBreakpoint<DotNetLineBreakpointProperties>

        val unityPausepointType = XDebuggerUtil.getInstance()
            .findBreakpointType(UnityPausepointBreakpointType::class.java)
        val breakpointManager = XDebuggerManager.getInstance(project).breakpointManager

        val oldPausepointsCount = breakpointManager.getBreakpoints(unityPausepointType).size
        convertToPausepoint(project, breakpoint)
        frameworkLogger.info("Convert line breakpoint to unity pausepoint")

        waitAndPump(defaultTimeout, { breakpointManager.getBreakpoints(unityPausepointType).size == oldPausepointsCount + 1 })
        { "Pausepoint isn't created" }
        val pausepoint = breakpointManager.getBreakpoints(unityPausepointType).first()
        pausepoint.condition = condition
        frameworkLogger.info("Set pausepoint condition: '$condition'")

        pausepoits.add(pausepoint)

        return pausepoint
    }
}