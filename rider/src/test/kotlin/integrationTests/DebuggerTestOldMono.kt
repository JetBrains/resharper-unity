package integrationTests

import java.io.File

class DebuggerTestOldMono : DebuggerTestBase() {
    override fun preprocessTempDirectory(tempDir: File) {
        super.preprocessTempDirectory(tempDir)

        // enable old mono
        val projectSettings = tempDir.resolve("ProjectSettings/ProjectSettings.asset")
        projectSettings.writeText(projectSettings.readText().replace("scriptingRuntimeVersion: 1","scriptingRuntimeVersion: 0"))

        // make code compatible with old mono
        val newBehaviourScript = "NewBehaviourScript.cs"
        val script  = tempDir.resolve("Assets").resolve(newBehaviourScript)
        script.writeText(script.readText().replace("0b_0001_1110_1000_0100_1000_0000", "0"))
    }
}