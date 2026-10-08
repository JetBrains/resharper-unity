package com.jetbrains.rider.unity.test.cases.agents

import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.process.ProcessOutput
import com.intellij.openapi.Disposable
import com.intellij.openapi.application.ApplicationManager
import com.intellij.testFramework.junit5.TestApplication
import com.intellij.testFramework.junit5.TestDisposable
import com.intellij.testFramework.junit5.fixture.TestFixtures
import com.intellij.testFramework.junit5.http.localhostHttpServer
import com.intellij.testFramework.junit5.http.url
import com.intellij.testFramework.replaceService
import com.jetbrains.rider.plugins.unity.settings.agents.UNITY_CLI_CDN_BASE_URL
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliEnvironment
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliInstallMethod
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliOffer
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliPresentation
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliProcessRunner
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliStatusProvider
import com.jetbrains.rider.plugins.unity.settings.agents.isReadableManifestUrl
import com.jetbrains.rider.plugins.unity.settings.agents.isUnityCliDiagnoseUpdateOutput
import com.jetbrains.rider.plugins.unity.settings.agents.isUnityCliHelpOutput
import com.jetbrains.rider.plugins.unity.settings.agents.parseUnityCliManifestVersion
import com.jetbrains.rider.plugins.unity.settings.agents.unityCliExecutableName
import com.jetbrains.rider.plugins.unity.settings.agents.unityCliVersionMessage
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import kotlinx.coroutines.runBlocking
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Assertions.assertFalse
import org.junit.jupiter.api.Assertions.assertNull
import org.junit.jupiter.api.Assertions.assertTrue
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test
import java.nio.file.Path

/**
 * Which copy of the Unity CLI Rider reports, and which binaries it launches to decide.
 *
 * Each case drives the real [UnityCliStatusProvider.read], with [UnityCliEnvironment] describing a
 * machine and [UnityCliProcessRunner] answering for each launch. Everything from the candidate order
 * onwards runs as it does in the product.
 *
 * Every case also records the command lines, because what Rider must **not** launch carries as much
 * of the rule as what it must.
 *
 * The [isReadableManifestUrl] and [parseUnityCliManifestVersion] cases near the bottom call those
 * two pure functions directly. Rider pins the manifest read to one host, so a local test server can
 * no longer stand in for it inside [detect]. These two functions are the only way left to pin the
 * gate and the parse it guards.
 */
@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity for Agents")
@Tag(TeamCityTags.Plugins.Unity.General)
@TestApplication
@TestFixtures
class UnityCliDetectionTest {
    private val serverFixture = localhostHttpServer()
    private val server get() = serverFixture.get()

    @Test
    fun `the copy on the PATH wins, and the well-known roots are left unread`(
        @TestDisposable disposable: Disposable,
    ) {
        val machine = machineWith(BREW_COPY, HOME_ROOT_COPY)
        install(
            disposable,
            machine,
            environment(onPath = BREW_COPY, unityCliHome = "/opt/unity-cli", executables = setOf(HOME_ROOT_COPY)),
        )

        val presentation = detect()

        assertEquals(BREW_COPY, presentation.location, "the copy on the PATH lost")
        assertEquals(INSTALLED_VERSION, presentation.installedVersion)
        assertNoLaunchOf(machine, HOME_ROOT_COPY)
    }

    @Test
    fun `UNITY_CLI_HOME is read before the roots in the home directory`(
        @TestDisposable disposable: Disposable,
    ) {
        val overrideCopy = Path.of("/opt/unity-cli", "bin", unityCliExecutableName())
        val machine = machineWith(overrideCopy, HOME_ROOT_COPY)
        install(
            disposable,
            machine,
            environment(
                unityCliHome = "/opt/unity-cli",
                executables = setOf(overrideCopy, HOME_ROOT_COPY, LOCAL_ROOT_COPY),
            ),
        )

        val presentation = detect()

        assertEquals(overrideCopy, presentation.location)
        assertNoLaunchOf(machine, HOME_ROOT_COPY)
        assertNoLaunchOf(machine, LOCAL_ROOT_COPY)
    }

    @Test
    fun `a root that holds no file is skipped, and the next one answers`(
        @TestDisposable disposable: Disposable,
    ) {
        val machine = machineWith(LOCAL_ROOT_COPY)
        install(disposable, machine, environment(executables = setOf(LOCAL_ROOT_COPY)))

        val presentation = detect()

        assertEquals(LOCAL_ROOT_COPY, presentation.location)
        assertNoLaunchOf(machine, HOME_ROOT_COPY)
    }

    @Test
    fun `a Windows machine discovers the copy in LOCALAPPDATA when PATH is empty`(
        @TestDisposable disposable: Disposable,
    ) {
        val localAppData = "C:\\Users\\tester\\AppData\\Local"
        val windowsCopy = Path.of(localAppData, "Unity", "bin", unityCliExecutableName())
        val beta12Help = """
            Usage: unity [options] [command]

            Automate Unity from the command line: install an Editor and create a project without the GUI.

            Commands:
              editors Manage Unity editors
              self-update Update the unity CLI
        """.trimIndent()

        val machine = machineWith().answering(windowsCopy) { command ->
            when (command.parametersList.list) {
                listOf("--version") -> ProcessOutput("1.0.0-beta.12", "", 0, false, false)
                listOf("--help") -> ProcessOutput(beta12Help, "", 0, false, false)
                listOf("diagnose", "update", "--format", "json") ->
                    ProcessOutput(diagnoseUpdateJson(null), "", 0, false, false)
                else -> ProcessOutput("", "unknown command", 2, false, false)
            }
        }

        install(
            disposable,
            machine,
            environment(
                localAppData = localAppData,
                executables = setOf(windowsCopy),
            ),
        )

        val presentation = detect()

        assertEquals(windowsCopy, presentation.location)
        assertEquals("1.0.0-beta.12", presentation.installedVersion)
    }

    @Test
    fun `an empty machine launches nothing and offers the install`(
        @TestDisposable disposable: Disposable,
    ) {
        val machine = machineWith()
        install(disposable, machine, environment())

        val presentation = detect()

        assertEquals(UnityCliOffer.INSTALL, presentation.offer)
        assertNull(presentation.installedVersion)
        assertNull(presentation.foreignBinaryOnPath, "an empty machine named a binary")
        assertTrue(machine.launched.isEmpty(), "Rider launched ${machine.launched}")
    }

    @Test
    fun `a binary on the PATH that is not the Unity CLI reads as not installed, and Rider names it`(
        @TestDisposable disposable: Disposable,
    ) {
        // The Ubuntu Unity desktop shell. It is on a real machine, and it answers `unity 7.5.0`.
        val machine = machineWith(HOME_ROOT_COPY).answering(DESKTOP_SHELL) { ProcessOutput("unity 7.5.0", "", 0, false, false) }
        install(
            disposable,
            machine,
            environment(onPath = DESKTOP_SHELL, executables = setOf(HOME_ROOT_COPY)),
        )

        val presentation = detect()

        assertEquals(UnityCliOffer.INSTALL, presentation.offer)
        assertEquals(DESKTOP_SHELL, presentation.foreignBinaryOnPath)
        // The PATH decides alone. A copy the shell will not reach must not be reported instead.
        assertNoLaunchOf(machine, HOME_ROOT_COPY)
    }

    // The old guard was a regular expression that refused a `+` segment, so a copy that reported
    // one read as a foreign program. Semantic Versioning allows it.
    @Test
    fun `a version that carries build metadata is still the Unity CLI`(
        @TestDisposable disposable: Disposable,
    ) {
        val version = "$INSTALLED_VERSION+build.5"
        val machine = machineWith(BREW_COPY).answering(BREW_COPY) { command ->
            when (command.parametersList.list) {
                listOf("--version") -> ProcessOutput(version, "", 0, false, false)
                listOf("--help") -> ProcessOutput(HELP_OUTPUT, "", 0, false, false)
                else -> ProcessOutput("", "unknown command", 2, false, false)
            }
        }
        install(disposable, machine, environment(onPath = BREW_COPY))

        val presentation = detect()

        assertEquals(BREW_COPY, presentation.location, "a version with build metadata was refused")
        assertEquals(version, presentation.installedVersion)
    }

    // The gate is `SemVer.parseFromText`, which checks the three numbers and takes the pre-release
    // segment verbatim to the end of the string. So the gate proves no content, and every sentence
    // that renders the version escapes it. This case pins both halves of that claim.
    @Test
    fun `a version that carries markup reaches the page escaped`(
        @TestDisposable disposable: Disposable,
    ) {
        val version = "$INSTALLED_VERSION-<img src=x onerror=alert(1)>"
        val machine = machineWith(BREW_COPY).answering(BREW_COPY) { command ->
            when (command.parametersList.list) {
                listOf("--version") -> ProcessOutput(version, "", 0, false, false)
                listOf("--help") -> ProcessOutput(HELP_OUTPUT, "", 0, false, false)
                else -> ProcessOutput("", "unknown command", 2, false, false)
            }
        }
        install(disposable, machine, environment(onPath = BREW_COPY))

        val presentation = detect()

        assertEquals(version, presentation.installedVersion, "detection refused the string")
        val message = unityCliVersionMessage(presentation.installedVersion)
        assertFalse(message.contains("<img"), "the version row rendered the markup: '$message'")
    }

    @Test
    fun `a bare version with no Unity help is not the Unity CLI`(
        @TestDisposable disposable: Disposable,
    ) {
        val impostor = Path.of("/usr/local/bin/unity")
        val machine = machineWith().answering(impostor) { command ->
            if (command.parametersList.list == listOf("--version")) ProcessOutput(INSTALLED_VERSION, "", 0, false, false)
            else ProcessOutput("Usage: some other tool", "", 0, false, false)
        }
        install(disposable, machine, environment(onPath = impostor))

        val presentation = detect()

        assertEquals(UnityCliOffer.INSTALL, presentation.offer)
        assertEquals(impostor, presentation.foreignBinaryOnPath)
    }

    @Test
    fun `a copy whose help answers runs diagnose update only for the update check`(
        @TestDisposable disposable: Disposable,
    ) {
        val copy = Path.of("/opt/help-first/bin/unity")
        val machine = machineWith(copy)
        install(disposable, machine, environment(onPath = copy))

        val presentation = detect()

        assertEquals(copy, presentation.location)
        val launches = machine.launched.map { it.parametersList.list }
        assertEquals(
            listOf(listOf("--version"), listOf("--help"), listOf("diagnose", "update", "--format", "json")),
            launches,
        )
    }

    @Test
    fun `a copy whose help Rider does not know is identified by diagnose update`(
        @TestDisposable disposable: Disposable,
    ) {
        val copy = Path.of("/opt/reworded-help/bin/unity")
        val machine = machineWith().answering(copy) { command ->
            when (command.parametersList.list) {
                listOf("--version") -> ProcessOutput(INSTALLED_VERSION, "", 0, false, false)
                listOf("--help") -> ProcessOutput("Usage: some reworded help", "", 0, false, false)
                listOf("diagnose", "update", "--format", "json") ->
                    ProcessOutput(diagnoseUpdateJson(null), "", 0, false, false)
                else -> ProcessOutput("", "unknown command", 2, false, false)
            }
        }
        install(disposable, machine, environment(onPath = copy))

        val presentation = detect()

        assertEquals(copy, presentation.location, "the diagnose update fallback did not identify the copy")
    }

    @Test
    fun `the private copy of the Unity Hub is never reported and never launched`(
        @TestDisposable disposable: Disposable,
    ) {
        val machine = machineWith(HUB_COPY)
        install(disposable, machine, environment(onPath = HUB_COPY, executables = setOf(HUB_COPY)))

        val presentation = detect()

        assertEquals(UnityCliOffer.INSTALL, presentation.offer)
        assertEquals(HUB_COPY, presentation.foreignBinaryOnPath, "Rider left the binary in the way unnamed")
        assertTrue(machine.launched.isEmpty(), "Rider launched the private copy of the Hub: ${machine.launched}")
    }

    @Test
    fun `a root inside the bundle of the Unity Hub is never launched`(
        @TestDisposable disposable: Disposable,
    ) {
        val machine = machineWith(HUB_COPY)
        install(
            disposable,
            machine,
            environment(
                unityCliHome = "/Applications/Unity Hub.app/Contents/Resources/cli",
                executables = setOf(
                    Path.of("/Applications/Unity Hub.app/Contents/Resources/cli", "bin", unityCliExecutableName()),
                ),
            ),
        )

        val presentation = detect()

        assertEquals(UnityCliOffer.INSTALL, presentation.offer)
        assertTrue(machine.launched.isEmpty(), "Rider launched ${machine.launched}")
    }

    @Test
    fun `a manifest at a foreign host is refused, and Homebrew still comes from the path`(
        @TestDisposable disposable: Disposable,
    ) {
        // The CLI can report any address. Rider reads only the pinned CDN host, so a local test
        // server stands in for a foreign one and Rider must never connect to it. The gate itself is
        // pinned below, in the isReadableManifestUrl cases.
        val machine = machineWith(BREW_COPY, diagnoseManifestUrl = server.url + "/latest-beta.json")
        install(disposable, machine, environment(onPath = BREW_COPY, realPath = CASKROOM_COPY))

        val presentation = detect()

        assertNull(presentation.latestVersion, "Rider read a manifest at a foreign host")
        assertEquals(UnityCliOffer.LATEST_VERSION_UNKNOWN, presentation.offer)
        // The install method comes from the resolved path, so it still renders when the manifest is
        // refused.
        assertEquals(UnityCliInstallMethod.HOMEBREW, presentation.installMethod)
        assertEquals(UPDATE_COMMAND, presentation.updateCommand)
    }

    @Test
    fun `a manifest address Rider will not read leaves the latest version unknown`(
        @TestDisposable disposable: Disposable,
    ) {
        // A blank manifest address is possible, because the field comes from a third party.
        // It raised `URI is not absolute` inside the platform reader, and the page rendered nothing.
        val copy = Path.of("/opt/blank/bin/unity")
        val machine = machineWith(copy, diagnoseManifestUrl = "")
        install(disposable, machine, environment(onPath = copy))

        val presentation = detect()

        assertEquals(copy, presentation.location, "detection stopped on a manifest address")
        assertNull(presentation.latestVersion)
        assertEquals(UnityCliOffer.LATEST_VERSION_UNKNOWN, presentation.offer)
    }

    // --- isReadableManifestUrl: the gate a reported manifest address must pass before Rider reads it ---

    @Test
    fun `the pinned CDN host is accepted over https`() {
        assertTrue(isReadableManifestUrl("${UNITY_CLI_CDN_BASE_URL}latest-beta.json"))
    }

    @Test
    fun `a foreign host is refused, even over https`() {
        assertFalse(isReadableManifestUrl("https://evil.example.com/latest-beta.json"))
    }

    @Test
    fun `the pinned host is refused over plain http`() {
        val httpUrl = "${UNITY_CLI_CDN_BASE_URL}latest-beta.json".replaceFirst("https://", "http://")
        assertFalse(isReadableManifestUrl(httpUrl))
    }

    @Test
    fun `a file address is refused`() {
        assertFalse(isReadableManifestUrl("file:///etc/passwd"))
    }

    @Test
    fun `a blank address is refused`() {
        assertFalse(isReadableManifestUrl(""))
    }

    @Test
    fun `a relative address is refused`() {
        assertFalse(isReadableManifestUrl("latest-beta.json"))
    }

    // --- parseUnityCliManifestVersion: the version inside a channel manifest ---

    @Test
    fun `a channel manifest reports its version`() {
        assertEquals(LATEST_VERSION, parseUnityCliManifestVersion("""{"version": "$LATEST_VERSION"}"""))
    }

    @Test
    fun `a manifest with no version field reports none`() {
        assertNull(parseUnityCliManifestVersion("""{"channel": "beta"}"""))
    }

    @Test
    fun `a manifest with a blank version reports none`() {
        assertNull(parseUnityCliManifestVersion("""{"version": ""}"""))
    }

    @Test
    fun `a document that is not JSON reports no version`() {
        assertNull(parseUnityCliManifestVersion("not json"))
    }

    // --- isUnityCliHelpOutput: identifies the Unity CLI across help output formats ---

    @Test
    fun `modern beta 12 help output is accepted`() {
        val beta12Help = """
            Usage: unity [options] [command]

            Automate Unity from the command line: install an Editor and create a project without the GUI.

            Options:
              -V, --version output the version number
            Commands:
              editors Manage Unity editors
              self-update Update the unity CLI
        """.trimIndent()
        assertTrue(isUnityCliHelpOutput(beta12Help))
    }

    @Test
    fun `help output with usage and commands but no banner is accepted`() {
        val bannerlessHelp = """
            Usage: unity [options] [command]

            Commands:
              editors Manage Unity editors
              self-update Update the unity CLI
        """.trimIndent()
        assertTrue(isUnityCliHelpOutput(bannerlessHelp))
    }

    @Test
    fun `an impostor help output is rejected`() {
        val desktopShellHelp = """
            Usage: unity [options]
            Options:
              --replace
              --debug
        """.trimIndent()
        assertFalse(isUnityCliHelpOutput(desktopShellHelp))
        assertFalse(isUnityCliHelpOutput("Usage: some other tool"))
    }

    // --- isUnityCliDiagnoseUpdateOutput: verifies structured JSON output from diagnose update ---

    @Test
    fun `a valid diagnose update JSON response identifies the CLI`() {
        val validJson = """
            {
              "success": true,
              "command": "diagnose-update",
              "data": {
                "version": "1.0.0-beta.12",
                "installMethod": "cdn"
              }
            }
        """.trimIndent()
        assertTrue(isUnityCliDiagnoseUpdateOutput(validJson))
    }

    @Test
    fun `an invalid or non-JSON output does not identify the CLI`() {
        assertFalse(isUnityCliDiagnoseUpdateOutput("not json"))
        assertFalse(isUnityCliDiagnoseUpdateOutput("""{"command": "other"}"""))
        assertFalse(isUnityCliDiagnoseUpdateOutput("""{"data": {}}"""))
        assertFalse(isUnityCliDiagnoseUpdateOutput("""{"command": "diagnose-update"}"""))
        assertFalse(isUnityCliDiagnoseUpdateOutput("""{"command": "diagnose-update", "data": {}}"""))
    }

    /** One machine, and the record of every command line it was asked to run. */
    private class Machine(
        private val unityCliCopies: Set<Path>,
        private val diagnoseManifestUrl: String?,
    ) {
        val launched: MutableList<GeneralCommandLine> = mutableListOf()
        private val answers: MutableMap<Path, (GeneralCommandLine) -> ProcessOutput> = mutableMapOf()

        fun answering(executable: Path, answer: (GeneralCommandLine) -> ProcessOutput): Machine {
            answers[executable] = answer
            return this
        }

        fun run(command: GeneralCommandLine): ProcessOutput {
            launched += command
            val executable = Path.of(command.exePath)
            answers[executable]?.let { return it(command) }
            if (executable !in unityCliCopies) return ProcessOutput("", "no such file", 127, false, false)
            return when (command.parametersList.list) {
                listOf("--version") -> ProcessOutput(INSTALLED_VERSION, "", 0, false, false)
                listOf("--help") -> ProcessOutput(HELP_OUTPUT, "", 0, false, false)
                listOf("diagnose", "update", "--format", "json") ->
                    ProcessOutput(diagnoseUpdateJson(diagnoseManifestUrl), "", 0, false, false)
                else -> ProcessOutput("", "unknown command", 2, false, false)
            }
        }
    }

    /** A null [diagnoseManifestUrl] leaves the field out of the envelope, so no read is attempted. */
    private fun machineWith(vararg unityCliCopies: Path, diagnoseManifestUrl: String? = null): Machine =
        Machine(unityCliCopies.toSet(), diagnoseManifestUrl)

    private fun environment(
        onPath: Path? = null,
        unityCliHome: String? = null,
        localAppData: String? = null,
        executables: Set<Path> = emptySet(),
        realPath: Path? = null,
    ): UnityCliEnvironment = UnityCliEnvironment.environmentAnswering(
        findOnPath = { onPath },
        environmentVariable = { name ->
            when (name) {
                "UNITY_CLI_HOME" -> unityCliHome
                "LOCALAPPDATA" -> localAppData
                else -> null
            }
        },
        userHome = { USER_HOME },
        isExecutable = { it in executables || it == onPath },
        realPath = { path -> if (realPath != null && path == onPath) realPath else path },
    )

    private fun install(disposable: Disposable, machine: Machine, environment: UnityCliEnvironment) {
        val application = ApplicationManager.getApplication()
        application.replaceService(UnityCliEnvironment::class.java, environment, disposable)
        application.replaceService(
            UnityCliProcessRunner::class.java,
            UnityCliProcessRunner.runnerAnswering { command, _ -> machine.run(command) },
            disposable,
        )
    }

    private fun detect(): UnityCliPresentation = runBlocking { UnityCliStatusProvider.getInstance().read() }

    private fun assertNoLaunchOf(machine: Machine, executable: Path) {
        val paths = machine.launched.map { it.exePath }
        assertTrue(executable.toString() !in paths, "Rider launched $executable. It launched $paths")
    }

    private companion object {
        val USER_HOME: Path = Path.of("/home/tester")
        val BREW_COPY: Path = Path.of("/opt/homebrew/bin/unity")
        val CASKROOM_COPY: Path = Path.of("/opt/homebrew/Caskroom/unity-cli/1.0.0-beta.9/unity")
        // The name carries `.exe` on Windows, and the rule that adds it lives in the product.
        val HOME_ROOT_COPY: Path = USER_HOME.resolve(".unity").resolve("bin").resolve(unityCliExecutableName())
        val LOCAL_ROOT_COPY: Path = USER_HOME.resolve(".local").resolve("bin").resolve(unityCliExecutableName())
        val HUB_COPY: Path = Path.of("/Applications/Unity Hub.app/Contents/Resources/cli/unity")
        val DESKTOP_SHELL: Path = Path.of("/usr/bin/unity")

        const val INSTALLED_VERSION = "1.0.0-beta.9"
        const val LATEST_VERSION = "1.0.0-beta.10"
        const val UPDATE_COMMAND = "brew upgrade unity-cli"

        val HELP_OUTPUT = """
            Usage: unity [options] [command]

            Commands:
              editors Manage Unity editors
              self-update Update the unity CLI
        """.trimIndent()

        fun diagnoseUpdateJson(manifestUrl: String?): String {
            val manifest = if (manifestUrl == null) "" else """"manifestUrl": "$manifestUrl","""
            return """
                {
                  "success": true,
                  "command": "diagnose-update",
                  "data": {
                    "version": "$INSTALLED_VERSION",
                    "installMethod": "homebrew",
                    "channel": "beta",
                    $manifest
                    "updateCommand": "$UPDATE_COMMAND"
                  },
                  "errors": [],
                  "warnings": []
                }
            """.trimIndent()
        }
    }
}
