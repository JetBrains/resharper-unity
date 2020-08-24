package integrationTests

import base.integrationTests.*
import com.jetbrains.rd.util.reactive.adviseNotNull
import com.jetbrains.rdclient.util.idea.pumpMessages
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.model.EditorLogEntry
import com.jetbrains.rider.test.asserts.shouldBe
import com.jetbrains.rider.test.framework.executeWithGold
import org.testng.annotations.Test
import java.time.Duration
import java.util.*

class PlayModeTest : IntegrationTestWithEditorBase() {
    override fun getSolutionDirectoryName() = "SimpleUnityProjectWithoutPlugin"

    @Test
    fun checkPlayingPauseModes() {
        val timeout = Duration.ofSeconds(3)
        val nestedLifetime = lifetime.createNested()
        val editorLogEntries: MutableList<EditorLogEntry> = Collections.synchronizedList(mutableListOf())
        rdUnityModel.onUnityLogEvent.adviseNotNull(nestedLifetime) { entry ->
            editorLogEntries.add(entry)
        }

        play()
        waitForUnityEditorPlaying()
        waitAndPump(timeout, { editorLogEntries.size >= 0 })
        { "Editor log entries actual count is ${editorLogEntries.size}, expected >= 0" }

        pause()
        waitForUnityEditorPaused()
        pumpMessages(timeout)
        val editorLogEntriesSizeBeforePause = editorLogEntries.size
        pumpMessages(timeout)
        editorLogEntries.size.shouldBe(editorLogEntriesSizeBeforePause,
            "Editor log entries actual count is ${editorLogEntries.size}, expected == $editorLogEntriesSizeBeforePause")

        unpause()
        waitForUnityEditorPlaying()
        waitAndPump(timeout, { editorLogEntries.size >= editorLogEntriesSizeBeforePause })
        { "Editor log entries actual count is ${editorLogEntries.size}, expected >= $editorLogEntriesSizeBeforePause" }
    }

    @Test
    fun chekSteps() {
        play()
        waitForUnityEditorPlaying()
        pause()
        waitForUnityEditorPaused()

        val nestedLifetime = lifetime.createNested()
        val editorLogEntries: MutableList<EditorLogEntry> = Collections.synchronizedList(mutableListOf())
        rdUnityModel.onUnityLogEvent.adviseNotNull(nestedLifetime) { entry ->
            editorLogEntries.add(entry)
            if (editorLogEntries.size == 2) {
                nestedLifetime.terminate()
            }
        }

        repeat(2) {
            step()
            waitAndPump(Duration.ofSeconds(5), { editorLogEntries.size == it + 1 })
            { "Editor log entries actual count is ${editorLogEntries.size}, expected is ${it + 1}" }
        }

        executeWithGold(testGoldFile) { stream ->
            editorLogEntries.forEach { entry ->
                printEditorLogEntry(stream, entry)
            }
        }
    }
}