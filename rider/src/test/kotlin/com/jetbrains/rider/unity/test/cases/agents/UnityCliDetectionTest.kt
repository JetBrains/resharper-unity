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
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliEnvironment
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliFileStamp
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliInstallMethod
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliInstallerRunner
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliOffer
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliPresentation
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliProcessRunner
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliStatusProvider
import com.jetbrains.rider.plugins.unity.settings.agents.dpkgListsUnityCli
import com.jetbrains.rider.plugins.unity.settings.agents.isUnityCliHelpOutput
import com.jetbrains.rider.plugins.unity.settings.agents.parseUnityCliManifestVersion
import com.jetbrains.rider.plugins.unity.settings.agents.parseUnityCliVersion
import com.jetbrains.rider.plugins.unity.settings.agents.unityCliChannel
import com.jetbrains.rider.plugins.unity.settings.agents.unityCliChannelManifest
import com.jetbrains.rider.plugins.unity.settings.agents.unityCliDirectoryOffPath
import com.jetbrains.rider.plugins.unity.settings.agents.unityCliExecutableName
import com.jetbrains.rider.plugins.unity.settings.agents.unityCliInstallMethodFromPath
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
import org.junit.jupiter.api.condition.DisabledOnOs
import org.junit.jupiter.api.condition.OS
import org.junit.jupiter.api.io.TempDir
import java.nio.file.Files
import java.nio.file.Path
import java.util.concurrent.TimeUnit

/**
 * Which copy of the Unity CLI Rider reports, and which binaries it launches to decide.
 *
 * Rider runs `--version`, then `--help`, to decide whether a binary is the Unity CLI.
 *
 * Each case drives the real [UnityCliStatusProvider.read], with [UnityCliEnvironment] describing a
 * machine and [UnityCliProcessRunner] answering for each launch. Everything from the candidate order
 * onwards runs as it does in the product.
 *
 * Every case also records the command lines, because what Rider must **not** launch carries as much
 * of the rule as what it must.
 *
 * The [parseUnityCliManifestVersion] cases near the bottom call that pure function directly. A local
 * test server stands in for the CDN through [UnityCliInstallerRunner.runnerAnswering], so no case
 * reaches the real CDN.
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

        val machine = machineWith().answering(windowsCopy) { command ->
            when (command.parametersList.list) {
                listOf("--version") -> ProcessOutput("1.0.0-beta.12", "", 0, false, false)
                listOf("--help") -> ProcessOutput(HELP_OUTPUT, "", 0, false, false)
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
    fun `a Windows machine discovers the MSIX alias in WindowsApps when PATH misses it`(
        @TestDisposable disposable: Disposable,
    ) {
        val localAppData = "C:\\Users\\tester\\AppData\\Local"
        val alias = Path.of(localAppData, "Microsoft", "WindowsApps", unityCliExecutableName())
        val machine = machineWith(alias)
        install(disposable, machine, environment(localAppData = localAppData, executables = setOf(alias)))

        val presentation = detect()

        assertEquals(alias, presentation.location)
        assertEquals(INSTALLED_VERSION, presentation.installedVersion)
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
        val machine = machineWith(HOME_ROOT_COPY).answering(DESKTOP_SHELL) { command ->
            when (command.parametersList.list) {
                listOf("--version") -> ProcessOutput("unity 7.5.0", "", 0, false, false)
                listOf("--help") -> ProcessOutput(OPTPARSE_HELP, "", 0, false, false)
                else -> ProcessOutput("", "unknown command", 2, false, false)
            }
        }
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
        // A space would end the version word, so the markup has none.
        val version = "$INSTALLED_VERSION-<img/src=x/onerror=alert(1)>"
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
    fun `a copy is identified by --version, then --help`(
        @TestDisposable disposable: Disposable,
    ) {
        val copy = Path.of("/opt/help-first/bin/unity")
        val machine = machineWith(copy)
        install(disposable, machine, environment(onPath = copy))

        val presentation = detect()

        assertEquals(copy, presentation.location)
        val launches = machine.launched.map { it.parametersList.list }
        assertEquals(listOf(listOf("--version"), listOf("--help")), launches)
    }

    @Test
    fun `the Unity Editor on the PATH is not the Unity CLI, and only --version runs`(
        @TestDisposable disposable: Disposable,
    ) {
        val editor = Path.of("/opt/unity-editor/2022.3.10f1/Editor/Unity")
        val machine = machineWith(editor).answering(editor) { command ->
            when (command.parametersList.list) {
                listOf("--version") -> ProcessOutput("2022.3.10f1", "", 0, false, false)
                else -> ProcessOutput("", "unknown command", 2, false, false)
            }
        }
        install(disposable, machine, environment(onPath = editor))

        val presentation = detect()

        assertEquals(UnityCliOffer.INSTALL, presentation.offer)
        assertEquals(editor, presentation.foreignBinaryOnPath)
        assertEquals(listOf(listOf("--version")), machine.launched.map { it.parametersList.list })
    }

    @Test
    fun `an empty --version output is not the Unity CLI, and only --version runs`(
        @TestDisposable disposable: Disposable,
    ) {
        val copy = Path.of("/opt/silent/bin/unity")
        val machine = machineWith(copy).answering(copy) { command ->
            when (command.parametersList.list) {
                listOf("--version") -> ProcessOutput("", "", 0, false, false)
                else -> ProcessOutput("", "unknown command", 2, false, false)
            }
        }
        install(disposable, machine, environment(onPath = copy))

        val presentation = detect()

        assertEquals(UnityCliOffer.INSTALL, presentation.offer)
        assertEquals(copy, presentation.foreignBinaryOnPath)
        assertEquals(listOf(listOf("--version")), machine.launched.map { it.parametersList.list })
    }

    @Test
    fun `a --version that exits with 1 is not the Unity CLI, even when it prints a version`(
        @TestDisposable disposable: Disposable,
    ) {
        val copy = Path.of("/opt/failing/bin/unity")
        val machine = machineWith(copy).answering(copy) { command ->
            when (command.parametersList.list) {
                listOf("--version") -> ProcessOutput(INSTALLED_VERSION, "", 1, false, false)
                else -> ProcessOutput("", "unknown command", 2, false, false)
            }
        }
        install(disposable, machine, environment(onPath = copy))

        val presentation = detect()

        assertEquals(UnityCliOffer.INSTALL, presentation.offer)
        assertEquals(copy, presentation.foreignBinaryOnPath)
        assertEquals(listOf(listOf("--version")), machine.launched.map { it.parametersList.list })
    }

    @Test
    fun `a program name before the version is skipped, and the version is read`(
        @TestDisposable disposable: Disposable,
    ) {
        val copy = Path.of("/opt/named/bin/unity")
        val machine = machineWith(copy).answering(copy) { command ->
            when (command.parametersList.list) {
                listOf("--version") -> ProcessOutput("unity 1.0.0-beta.16", "", 0, false, false)
                listOf("--help") -> ProcessOutput(HELP_OUTPUT, "", 0, false, false)
                else -> ProcessOutput("", "unknown command", 2, false, false)
            }
        }
        install(disposable, machine, environment(onPath = copy))

        val presentation = detect()

        assertEquals(copy, presentation.location)
        assertEquals("1.0.0-beta.16", presentation.installedVersion)
    }

    @Test
    fun `a banner line above the version is skipped, and the version is read`(
        @TestDisposable disposable: Disposable,
    ) {
        val copy = Path.of("/opt/banner/bin/unity")
        val machine = machineWith(copy).answering(copy) { command ->
            when (command.parametersList.list) {
                listOf("--version") -> ProcessOutput("Unity CLI\n1.0.0-beta.16\n", "", 0, false, false)
                listOf("--help") -> ProcessOutput(HELP_OUTPUT, "", 0, false, false)
                else -> ProcessOutput("", "unknown command", 2, false, false)
            }
        }
        install(disposable, machine, environment(onPath = copy))

        val presentation = detect()

        assertEquals(copy, presentation.location)
        assertEquals("1.0.0-beta.16", presentation.installedVersion)
    }

    @Test
    fun `a help without the MCP server is not the Unity CLI`(
        @TestDisposable disposable: Disposable,
    ) {
        val copy = Path.of("/opt/no-mcp/bin/unity")
        val machine = machineWith(copy).answering(copy) { command ->
            when (command.parametersList.list) {
                listOf("--version") -> ProcessOutput(INSTALLED_VERSION, "", 0, false, false)
                listOf("--help") -> ProcessOutput(HELP_WITHOUT_MCP, "", 0, false, false)
                else -> ProcessOutput("", "unknown command", 2, false, false)
            }
        }
        install(disposable, machine, environment(onPath = copy))

        val presentation = detect()

        assertEquals(UnityCliOffer.INSTALL, presentation.offer)
        assertEquals(copy, presentation.foreignBinaryOnPath)
    }

    @Test
    fun `a help that names upgrade, and not self-update, is the Unity CLI`(
        @TestDisposable disposable: Disposable,
    ) {
        val copy = Path.of("/opt/older-name/bin/unity")
        val olderHelp = HELP_OUTPUT.replace("self-update|upgrade", "upgrade")
        val machine = machineWith(copy).answering(copy) { command ->
            when (command.parametersList.list) {
                listOf("--version") -> ProcessOutput(INSTALLED_VERSION, "", 0, false, false)
                listOf("--help") -> ProcessOutput(olderHelp, "", 0, false, false)
                else -> ProcessOutput("", "unknown command", 2, false, false)
            }
        }
        install(disposable, machine, environment(onPath = copy))

        val presentation = detect()

        assertEquals(copy, presentation.location)
        assertEquals(INSTALLED_VERSION, presentation.installedVersion)
    }

    @Test
    fun `a --help that exits with 2 is not the Unity CLI, even when it prints the help`(
        @TestDisposable disposable: Disposable,
    ) {
        val copy = Path.of("/opt/failing-help/bin/unity")
        val machine = machineWith(copy).answering(copy) { command ->
            when (command.parametersList.list) {
                listOf("--version") -> ProcessOutput(INSTALLED_VERSION, "", 0, false, false)
                listOf("--help") -> ProcessOutput(HELP_OUTPUT, "", 2, false, false)
                else -> ProcessOutput("", "unknown command", 2, false, false)
            }
        }
        install(disposable, machine, environment(onPath = copy))

        val presentation = detect()

        assertEquals(UnityCliOffer.INSTALL, presentation.offer)
        assertEquals(copy, presentation.foreignBinaryOnPath)
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
    fun `the latest version comes from the channel manifest of the installed version`(
        @TestDisposable disposable: Disposable,
    ) {
        server.createContext("/latest-beta.json") { exchange ->
            val body = """{"version": "$LATEST_VERSION"}""".toByteArray()
            exchange.sendResponseHeaders(200, body.size.toLong())
            exchange.responseBody.write(body)
            exchange.close()
        }
        val machine = machineWith(BREW_COPY)
        install(disposable, machine, environment(onPath = BREW_COPY, realPath = CASKROOM_COPY))

        val presentation = detect()

        assertEquals(LATEST_VERSION, presentation.latestVersion)
        assertEquals(UnityCliOffer.UPDATE, presentation.offer)
        assertEquals(UnityCliInstallMethod.HOMEBREW, presentation.installMethod)
        assertEquals(listOf(listOf("--version"), listOf("--help")), machine.launched.map { it.parametersList.list })
    }

    @Test
    fun `a channel manifest that does not answer leaves the latest version unknown`(
        @TestDisposable disposable: Disposable,
    ) {
        // No context is registered, so the server answers 404 for the manifest.
        val machine = machineWith(BREW_COPY)
        install(disposable, machine, environment(onPath = BREW_COPY, realPath = CASKROOM_COPY))

        val presentation = detect()

        assertEquals(UnityCliOffer.LATEST_VERSION_UNKNOWN, presentation.offer)
    }

    @Test
    fun `a copy with an unchanged stamp runs --version and --help once`(
        @TestDisposable disposable: Disposable,
    ) {
        val copy = Path.of("/opt/stamped/bin/unity")
        val machine = machineWith(copy)
        install(disposable, machine, environment(onPath = copy, fileStamp = { UnityCliFileStamp(100, 200) }))

        detect()
        detect()

        assertEquals(listOf(listOf("--version"), listOf("--help")), machine.launched.map { it.parametersList.list })
    }

    @Test
    fun `a copy with a new stamp runs --version and --help again`(
        @TestDisposable disposable: Disposable,
    ) {
        val copy = Path.of("/opt/stamped/bin/unity")
        var stamp = UnityCliFileStamp(100, 200)
        val machine = machineWith(copy)
        install(disposable, machine, environment(onPath = copy, fileStamp = { stamp }))

        detect()
        stamp = UnityCliFileStamp(100, 300)
        detect()

        val launches = machine.launched.map { it.parametersList.list }
        assertEquals(listOf(listOf("--version"), listOf("--help"), listOf("--version"), listOf("--help")), launches)
    }

    @Test
    fun `a copy without a stamp runs --version and --help on each read`(
        @TestDisposable disposable: Disposable,
    ) {
        val copy = Path.of("/opt/unstamped/bin/unity")
        val machine = machineWith(copy)
        install(disposable, machine, environment(onPath = copy))

        detect()
        detect()

        val launches = machine.launched.map { it.parametersList.list }
        assertEquals(listOf(listOf("--version"), listOf("--help"), listOf("--version"), listOf("--help")), launches)
    }

    @Test
    fun `a timed-out --help is not cached, so the next read runs it again`(
        @TestDisposable disposable: Disposable,
    ) {
        val copy = Path.of("/opt/timed-out/bin/unity")
        var helpLaunches = 0
        val machine = machineWith(copy).answering(copy) { command ->
            when (command.parametersList.list) {
                listOf("--version") -> ProcessOutput(INSTALLED_VERSION, "", 0, false, false)
                listOf("--help") -> {
                    helpLaunches++
                    // The first --help times out, and the second one answers.
                    if (helpLaunches == 1) ProcessOutput("", "", -1, true, false) else ProcessOutput(HELP_OUTPUT, "", 0, false, false)
                }
                else -> ProcessOutput("", "unknown command", 2, false, false)
            }
        }
        install(disposable, machine, environment(onPath = copy, fileStamp = { UnityCliFileStamp(100, 200) }))

        val first = detect()
        val second = detect()

        assertNull(first.location, "a timed-out --help named the copy")
        assertEquals(copy, second.location, "the next read did not detect the copy")
        val launches = machine.launched.map { it.parametersList.list }
        assertEquals(listOf(listOf("--version"), listOf("--help"), listOf("--version"), listOf("--help")), launches)
    }

    @Test
    fun `a version stays cached for six hours`(
        @TestDisposable disposable: Disposable,
    ) {
        val copy = Path.of("/opt/cached/bin/unity")
        var now = 0L
        val machine = machineWith(copy)
        install(
            disposable,
            machine,
            environment(onPath = copy, fileStamp = { UnityCliFileStamp(100, 200) }, currentTimeMs = { now }),
        )

        detect()
        now = TimeUnit.HOURS.toMillis(5)
        detect()

        assertEquals(listOf(listOf("--version"), listOf("--help")), machine.launched.map { it.parametersList.list })
    }

    @Test
    fun `a version runs --version and --help again after six hours`(
        @TestDisposable disposable: Disposable,
    ) {
        val copy = Path.of("/opt/cached/bin/unity")
        var now = 0L
        val machine = machineWith(copy)
        install(
            disposable,
            machine,
            environment(onPath = copy, fileStamp = { UnityCliFileStamp(100, 200) }, currentTimeMs = { now }),
        )

        detect()
        now = TimeUnit.HOURS.toMillis(6)
        detect()

        val launches = machine.launched.map { it.parametersList.list }
        assertEquals(listOf(listOf("--version"), listOf("--help"), listOf("--version"), listOf("--help")), launches)
    }

    @Test
    fun `a copy whose --version exits with 1 is not run again within a minute`(
        @TestDisposable disposable: Disposable,
    ) {
        val copy = Path.of("/opt/failing-version/bin/unity")
        var now = 0L
        val machine = machineWith(copy).answering(copy) { command ->
            when (command.parametersList.list) {
                listOf("--version") -> ProcessOutput(INSTALLED_VERSION, "", 1, false, false)
                else -> ProcessOutput("", "unknown command", 2, false, false)
            }
        }
        install(
            disposable,
            machine,
            environment(onPath = copy, fileStamp = { UnityCliFileStamp(100, 200) }, currentTimeMs = { now }),
        )

        detect()
        now = TimeUnit.SECONDS.toMillis(59)
        detect()

        assertEquals(listOf(listOf("--version")), machine.launched.map { it.parametersList.list })
    }

    @Test
    fun `a copy whose --version exits with 1 runs --version again after a minute`(
        @TestDisposable disposable: Disposable,
    ) {
        val copy = Path.of("/opt/failing-version/bin/unity")
        var now = 0L
        val machine = machineWith(copy).answering(copy) { command ->
            when (command.parametersList.list) {
                listOf("--version") -> ProcessOutput(INSTALLED_VERSION, "", 1, false, false)
                else -> ProcessOutput("", "unknown command", 2, false, false)
            }
        }
        install(
            disposable,
            machine,
            environment(onPath = copy, fileStamp = { UnityCliFileStamp(100, 200) }, currentTimeMs = { now }),
        )

        detect()
        now = TimeUnit.MINUTES.toMillis(1)
        detect()

        assertEquals(listOf(listOf("--version"), listOf("--version")), machine.launched.map { it.parametersList.list })
    }

    @Test
    fun `a copy that fails --version once is detected on the next read after a minute`(
        @TestDisposable disposable: Disposable,
    ) {
        val copy = Path.of("/opt/flaky/bin/unity")
        var now = 0L
        var versionLaunches = 0
        val machine = machineWith(copy).answering(copy) { command ->
            when (command.parametersList.list) {
                listOf("--version") -> {
                    versionLaunches++
                    // The first start fails, and the next start answers.
                    if (versionLaunches == 1) ProcessOutput("", "", 1, false, false)
                    else ProcessOutput(INSTALLED_VERSION, "", 0, false, false)
                }
                listOf("--help") -> ProcessOutput(HELP_OUTPUT, "", 0, false, false)
                else -> ProcessOutput("", "unknown command", 2, false, false)
            }
        }
        install(
            disposable,
            machine,
            environment(onPath = copy, fileStamp = { UnityCliFileStamp(100, 200) }, currentTimeMs = { now }),
        )

        val first = detect()
        now = TimeUnit.MINUTES.toMillis(1)
        val second = detect()

        assertNull(first.location, "a failed --version named the copy")
        assertEquals(copy, second.location, "the next read did not detect the copy")
        assertEquals(INSTALLED_VERSION, second.installedVersion)
    }

    @Test
    fun `a Caskroom copy of the Homebrew cask is Homebrew`() {
        assertEquals(UnityCliInstallMethod.HOMEBREW, methodOf(CASKROOM_COPY))
    }

    @Test
    fun `a WindowsApps alias and WinGet copies in the user or machine scope are WinGet`() {
        val alias = Path.of("/home/tester/AppData/Local/Microsoft/WindowsApps/unity.exe")
        val link = Path.of("/home/tester/AppData/Local/Microsoft/WinGet/Links/unity.exe")
        val machineLink = Path.of("/Program Files/WinGet/Links/unity.exe")
        val machinePackage = Path.of("/Program Files/WinGet/Packages/Unity.CLI_Microsoft.Winget.Source_8wekyb3d8bbwe/unity.exe")
        assertEquals(UnityCliInstallMethod.WINGET, methodOf(alias))
        assertEquals(UnityCliInstallMethod.WINGET, methodOf(link))
        assertEquals(UnityCliInstallMethod.WINGET, methodOf(machineLink))
        assertEquals(UnityCliInstallMethod.WINGET, methodOf(machinePackage))
    }

    @Test
    fun `a copy in a CDN folder is CDN`() {
        val copy = Path.of("/home/tester/.unity/bin/unity")
        val method = unityCliInstallMethodFromPath(
            path = copy,
            realPath = copy,
            cdnDirectories = listOf(Path.of("/home/tester/.unity/bin")),
            dpkgListsUnityCli = { false },
            dnfIsInstalled = { false },
        )
        assertEquals(UnityCliInstallMethod.CDN, method)
    }

    @Test
    fun `a link in a CDN folder to an apt copy is APT`() {
        val link = Path.of("/home/tester/.local/bin/unity")
        val method = unityCliInstallMethodFromPath(
            path = link,
            realPath = Path.of("/usr/bin/unity"),
            cdnDirectories = listOf(Path.of("/home/tester/.local/bin")),
            dpkgListsUnityCli = { true },
            dnfIsInstalled = { false },
        )
        assertEquals(UnityCliInstallMethod.APT, method)
    }

    @Test
    fun `a link in a CDN folder to an unknown copy is not CDN`() {
        val link = Path.of("/home/tester/.local/bin/unity")
        val method = unityCliInstallMethodFromPath(
            path = link,
            realPath = Path.of("/opt/tool/unity"),
            cdnDirectories = listOf(Path.of("/home/tester/.local/bin")),
            dpkgListsUnityCli = { false },
            dnfIsInstalled = { false },
        )
        assertEquals(UnityCliInstallMethod.UNKNOWN, method)
    }

    @Test
    fun `a copy in a CDN folder behind a linked home folder is CDN`() {
        val copy = Path.of("/home/tester/.local/bin/unity")
        val method = unityCliInstallMethodFromPath(
            path = copy,
            realPath = Path.of("/data/home/tester/.local/bin/unity"),
            cdnDirectories = listOf(Path.of("/home/tester/.local/bin"), Path.of("/data/home/tester/.local/bin")),
            dpkgListsUnityCli = { false },
            dnfIsInstalled = { false },
        )
        assertEquals(UnityCliInstallMethod.CDN, method)
    }

    @Test
    fun `a copy under usr that dpkg lists is APT`() {
        assertEquals(UnityCliInstallMethod.APT, methodOf(Path.of("/usr/bin/unity"), dpkgListsUnityCli = true))
    }

    @Test
    fun `a copy under usr without dpkg and with dnf is DNF`() {
        assertEquals(UnityCliInstallMethod.DNF, methodOf(Path.of("/usr/bin/unity"), dnfIsInstalled = true))
    }

    @Test
    fun `a copy under usr with neither dpkg nor dnf is UNKNOWN, as on Arch and openSUSE`() {
        assertEquals(UnityCliInstallMethod.UNKNOWN, methodOf(Path.of("/usr/bin/unity")))
    }

    @Test
    fun `a copy under usr local or in an unknown folder has no package manager`() {
        assertEquals(UnityCliInstallMethod.UNKNOWN, methodOf(Path.of("/usr/local/bin/unity")))
        assertEquals(UnityCliInstallMethod.UNKNOWN, methodOf(Path.of("/opt/tool/unity")))
    }

    @Test
    fun `dpkg lists unity-cli when its list file is there`(@TempDir dpkgInfo: Path) {
        Files.createFile(dpkgInfo.resolve("unity-cli.list"))

        assertTrue(dpkgListsUnityCli(dpkgInfo))
    }

    @Test
    @DisabledOnOs(OS.WINDOWS, disabledReason = "Windows file names cannot hold a colon")
    fun `dpkg lists unity-cli when its Multi-Arch list file carries the architecture`(@TempDir dpkgInfo: Path) {
        Files.createFile(dpkgInfo.resolve("unity-cli:amd64.list"))

        assertTrue(dpkgListsUnityCli(dpkgInfo))
    }

    @Test
    fun `dpkg does not list unity-cli when only another package is listed`(@TempDir dpkgInfo: Path) {
        Files.createFile(dpkgInfo.resolve("other.list"))

        assertFalse(dpkgListsUnityCli(dpkgInfo))
    }

    @Test
    fun `dpkg does not list unity-cli when its info folder is missing`(@TempDir tempDir: Path) {
        assertFalse(dpkgListsUnityCli(tempDir.resolve("missing")))
    }

    @Test
    fun `the beta and alpha tags of the installed version name their channels`() {
        assertEquals("beta", unityCliChannel("", "1.0.0-beta.15"))
        assertEquals("alpha", unityCliChannel("", "1.0.0-alpha.3"))
    }

    @Test
    fun `a version with no pre-release tag is on the stable channel`() {
        assertEquals("stable", unityCliChannel("", "1.0.0"))
    }

    @Test
    fun `a forced channel wins over the channel of the installed version`() {
        assertEquals("stable", unityCliChannel("stable", "1.0.0-beta.1"))
        assertEquals("alpha", unityCliChannel("alpha", "1.0.0"))
    }

    @Test
    fun `each channel names its manifest on the CDN`() {
        assertEquals("latest-beta.json", unityCliChannelManifest("beta"))
        assertEquals("latest-alpha.json", unityCliChannelManifest("alpha"))
        assertEquals("latest.json", unityCliChannelManifest("stable"))
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

    @Test
    fun `a copy in a well-known root that the PATH does not have gives its directory`() {
        val environment = environment(executables = setOf(LOCAL_ROOT_COPY))
        assertEquals(LOCAL_ROOT_COPY.parent, environment.unityCliDirectoryOffPath())
    }

    @Test
    fun `a copy on the PATH gives no directory`() {
        val environment = environment(onPath = BREW_COPY, executables = setOf(HOME_ROOT_COPY))
        assertNull(environment.unityCliDirectoryOffPath())
    }

    @Test
    fun `no copy in the well-known roots gives no directory`() {
        assertNull(environment().unityCliDirectoryOffPath())
    }

    // --- parseUnityCliVersion: the version word of `--version` ---

    @Test
    fun `the version word of --version is the version, with or without a program name`() {
        assertEquals("1.0.0-beta.15", parseUnityCliVersion("1.0.0-beta.15"))
        assertEquals("1.0.0-beta.15", parseUnityCliVersion("unity 1.0.0-beta.15"))
        assertEquals("7.5.0", parseUnityCliVersion("unity 7.5.0"))
    }

    @Test
    fun `a pre-release segment is kept to the end of the version word`() {
        assertEquals("1.0.0-something.123", parseUnityCliVersion("1.0.0-something.123"))
    }

    @Test
    fun `white space around the version word is not part of the version`() {
        assertEquals("1.0.0", parseUnityCliVersion("  1.0.0\n"))
    }

    @Test
    fun `output with no version word names no version`() {
        assertNull(parseUnityCliVersion("2022.3.10f1"), "a Unity Editor version read as the CLI version")
        assertNull(parseUnityCliVersion(""))
        assertNull(parseUnityCliVersion("unity"))
        assertNull(parseUnityCliVersion("1.0"))
    }

    // --- isUnityCliHelpOutput: the help of the CLI ---

    @Test
    fun `the help of the CLI is recognized, in any letter case`() {
        assertTrue(isUnityCliHelpOutput(HELP_OUTPUT), "the help of the CLI was refused")
        assertTrue(isUnityCliHelpOutput(HELP_OUTPUT.uppercase()), "the upper-case help was refused")
    }

    @Test
    fun `the optparse help of the desktop shell is not the help of the CLI`() {
        assertFalse(isUnityCliHelpOutput(OPTPARSE_HELP))
    }

    @Test
    fun `a help without the MCP server is not the help of the CLI`() {
        assertFalse(isUnityCliHelpOutput(HELP_WITHOUT_MCP))
    }

    @Test
    fun `a help without the usage line is not the help of the CLI`() {
        val helpWithoutUsage = HELP_OUTPUT.replace("Usage: unity [options] [command]", "Synopsis: the CLI")
        assertFalse(isUnityCliHelpOutput(helpWithoutUsage))
    }

    @Test
    fun `a help with upgrade and no self-update is the help of the CLI`() {
        assertTrue(isUnityCliHelpOutput(HELP_OUTPUT.replace("self-update|upgrade", "upgrade")))
    }

    /** One machine, and the record of every command line it was asked to run. */
    private class Machine(private val unityCliCopies: Set<Path>) {
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
                else -> ProcessOutput("", "unknown command", 2, false, false)
            }
        }
    }

    private fun machineWith(vararg unityCliCopies: Path): Machine = Machine(unityCliCopies.toSet())

    private fun methodOf(path: Path, dpkgListsUnityCli: Boolean = false, dnfIsInstalled: Boolean = false): UnityCliInstallMethod =
        unityCliInstallMethodFromPath(
            path = path,
            realPath = path,
            cdnDirectories = emptyList(),
            dpkgListsUnityCli = { dpkgListsUnityCli },
            dnfIsInstalled = { dnfIsInstalled },
        )

    private fun environment(
        onPath: Path? = null,
        unityCliHome: String? = null,
        localAppData: String? = null,
        executables: Set<Path> = emptySet(),
        realPath: Path? = null,
        fileStamp: (Path) -> UnityCliFileStamp? = { null },
        currentTimeMs: () -> Long = { 0L },
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
        fileStamp = fileStamp,
        currentTimeMs = currentTimeMs,
    )

    private fun install(disposable: Disposable, machine: Machine, environment: UnityCliEnvironment) {
        val application = ApplicationManager.getApplication()
        application.replaceService(UnityCliEnvironment::class.java, environment, disposable)
        application.replaceService(
            UnityCliProcessRunner::class.java,
            UnityCliProcessRunner.runnerAnswering { command, _ -> machine.run(command) },
            disposable,
        )
        application.replaceService(
            UnityCliInstallerRunner::class.java,
            UnityCliInstallerRunner.runnerAnswering(cdnBaseUrl = "${server.url}/"),
            disposable,
        )
        UnityCliStatusProvider.getInstance().forgetUpdateInfo()
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

        // An excerpt of the help of the CLI 1.0.0-beta.15.
        val HELP_OUTPUT = """
            Usage: unity [options] [command]

            Automate Unity from the command line: install an Editor and create a project
            without the GUI.

            Editor Automation Commands:
              mcp [options]                              MCP server and client configuration for Unity Editor

            Maintenance Commands:
              self-update|upgrade [options]              Update the unity CLI to the latest version
        """.trimIndent()

        // The help of the optparse program that the Ubuntu desktop shell installs as `unity`.
        val OPTPARSE_HELP = """
            Usage: unity [options]

            Options:
              --version             show program's version number and exit
              --replace             Run unity
              --reset-icons         Reset the default launcher icons.
        """.trimIndent()

        // The help of the CLI without its MCP line.
        val HELP_WITHOUT_MCP: String = HELP_OUTPUT.lines().filterNot { it.contains("mcp", ignoreCase = true) }.joinToString("\n")
    }
}
