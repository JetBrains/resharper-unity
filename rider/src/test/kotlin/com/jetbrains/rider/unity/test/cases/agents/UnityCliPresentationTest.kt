package com.jetbrains.rider.unity.test.cases.agents

import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliFailure
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliInstallMethod
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliOffer
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliOperation
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliPresentation
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test
import java.nio.file.Path

/**
 * Pins [UnityCliPresentation.offer], the one derivation point of the Unity CLI section.
 *
 * The class runs no process, it reads no file and it needs no application. The table below is the
 * specification of the order of the branches, so a reader learns the rule from the test.
 */
@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity for Agents")
@Tag(TeamCityTags.Plugins.Unity.General)
class UnityCliPresentationTest {
    private data class Case(val name: String, val presentation: UnityCliPresentation, val expected: UnityCliOffer)

    private val installed = Path.of("/opt/homebrew/bin/unity")

    private val cases: List<Case> = listOf(
        Case(
            "the first detection has not finished",
            UnityCliPresentation(),
            UnityCliOffer.CHECKING,
        ),
        Case(
            "no Unity CLI exists on the machine",
            UnityCliPresentation(detected = true),
            UnityCliOffer.INSTALL,
        ),
        Case(
            "Rider could not read the latest version",
            UnityCliPresentation(detected = true, installedVersion = "1.0.0-beta.10", location = installed),
            UnityCliOffer.LATEST_VERSION_UNKNOWN,
        ),
        Case(
            "an unread latest version outranks the update command of apt",
            UnityCliPresentation(
                detected = true,
                installedVersion = "1.0.0-beta.9",
                location = Path.of("/usr/bin/unity"),
                installMethod = UnityCliInstallMethod.APT,
                updateCommand = "sudo apt install --only-upgrade unity-cli",
            ),
            UnityCliOffer.LATEST_VERSION_UNKNOWN,
        ),
        Case(
            "the installed copy is the latest one",
            UnityCliPresentation(
                detected = true,
                installedVersion = "1.0.0-beta.10",
                location = installed,
                latestVersion = "1.0.0-beta.10",
            ),
            UnityCliOffer.CURRENT,
        ),
        // A text comparison answers both of these wrong. It reads beta.9 as newer than beta.11,
        // because "9" sorts above "1", and it reads any build metadata as a new version.
        Case(
            "a feed that reports an older prerelease leaves the copy up to date",
            UnityCliPresentation(
                detected = true,
                installedVersion = "1.0.0-beta.11",
                location = installed,
                latestVersion = "1.0.0-beta.9",
            ),
            UnityCliOffer.CURRENT,
        ),
        Case(
            "build metadata carries no precedence, so it offers no update",
            UnityCliPresentation(
                detected = true,
                installedVersion = "1.0.0",
                location = installed,
                latestVersion = "1.0.0+build.5",
            ),
            UnityCliOffer.CURRENT,
        ),
        Case(
            "a newer prerelease of the same patch offers the update",
            UnityCliPresentation(
                detected = true,
                installedVersion = "1.0.0-beta.9",
                location = installed,
                installMethod = UnityCliInstallMethod.CDN,
                latestVersion = "1.0.0-beta.11",
            ),
            UnityCliOffer.UPDATE,
        ),
        Case(
            "a release outranks its own prerelease",
            UnityCliPresentation(
                detected = true,
                installedVersion = "1.0.0-beta.11",
                location = installed,
                installMethod = UnityCliInstallMethod.CDN,
                latestVersion = "1.0.0",
            ),
            UnityCliOffer.UPDATE,
        ),
        Case(
            "Homebrew owns the copy and a newer one exists",
            UnityCliPresentation(
                detected = true,
                installedVersion = "1.0.0-beta.9",
                location = installed,
                installMethod = UnityCliInstallMethod.HOMEBREW,
                latestVersion = "1.0.0-beta.10",
                updateCommand = "brew upgrade unity-cli",
            ),
            UnityCliOffer.UPDATE,
        ),
        Case(
            "apt owns the copy, so Rider shows the command and runs nothing",
            UnityCliPresentation(
                detected = true,
                installedVersion = "1.0.0-beta.9",
                location = Path.of("/usr/bin/unity"),
                installMethod = UnityCliInstallMethod.APT,
                latestVersion = "1.0.0-beta.10",
                updateCommand = "sudo apt install --only-upgrade unity-cli",
            ),
            UnityCliOffer.SHOW_UPDATE_COMMAND,
        ),
        Case(
            "an untrusted project with no Unity CLI offers trust, and not the install",
            UnityCliPresentation(detected = true, projectTrusted = false),
            UnityCliOffer.NOT_TRUSTED,
        ),
        Case(
            "an untrusted project reports trust even when the copy is the latest one",
            UnityCliPresentation(
                detected = true,
                installedVersion = "1.0.0-beta.10",
                location = installed,
                latestVersion = "1.0.0-beta.10",
                projectTrusted = false,
            ),
            UnityCliOffer.NOT_TRUSTED,
        ),
        Case(
            "the update command of apt survives an untrusted project, because Rider runs nothing",
            UnityCliPresentation(
                detected = true,
                installedVersion = "1.0.0-beta.9",
                location = Path.of("/usr/bin/unity"),
                installMethod = UnityCliInstallMethod.APT,
                latestVersion = "1.0.0-beta.10",
                updateCommand = "sudo apt install --only-upgrade unity-cli",
                projectTrusted = false,
            ),
            UnityCliOffer.SHOW_UPDATE_COMMAND,
        ),
        Case(
            "an untrusted project outranks an unfinished detection, because detection needs trust too",
            UnityCliPresentation(detected = false, projectTrusted = false),
            UnityCliOffer.NOT_TRUSTED,
        ),
        Case(
            "an install runs now",
            UnityCliPresentation(detected = true, operation = UnityCliOperation.INSTALLING),
            UnityCliOffer.BUSY,
        ),
        Case(
            "the last install stopped on an exit code",
            UnityCliPresentation(detected = true, failure = UnityCliFailure.INSTALLER_EXIT_CODE, failureExitCode = 1),
            UnityCliOffer.FAILED,
        ),
        Case(
            "a failure outranks an unfinished detection",
            UnityCliPresentation(detected = false, failure = UnityCliFailure.DOWNLOAD),
            UnityCliOffer.FAILED,
        ),
        Case(
            "an operation outranks a failure",
            UnityCliPresentation(
                detected = true,
                operation = UnityCliOperation.UPDATING,
                failure = UnityCliFailure.UPDATE_EXIT_CODE,
                failureExitCode = 1,
            ),
            UnityCliOffer.BUSY,
        ),
        Case(
            "a command that never started is a failure with no exit code",
            UnityCliPresentation(
                detected = true,
                failure = UnityCliFailure.LAUNCH_FAILED,
                failedOperation = UnityCliOperation.UPDATING,
            ),
            UnityCliOffer.FAILED,
        ),
    )

    @Test
    fun eachStateProducesItsOffer() {
        for ((name, presentation, expected) in cases) {
            assertEquals(expected, presentation.offer, name)
        }
    }

    /**
     * Fails when [UnityCliOffer] gains a value that no case reaches.
     *
     * Most values are unreachable in the product today, because the provider is still a stub, and
     * this test is the only thing that reaches them.
     */
    @Test
    fun everyOfferValueHasACase() {
        assertEquals(UnityCliOffer.entries.toSet(), cases.map { it.expected }.toSet(), "every offer needs one case")
    }

    @Test
    fun riderRunsTheUpdateOnlyForTheMethodsItOwns() {
        val expected = mapOf(
            UnityCliInstallMethod.CDN to true,
            UnityCliInstallMethod.HOMEBREW to true,
            UnityCliInstallMethod.WINGET to true,
            UnityCliInstallMethod.APT to false,
            UnityCliInstallMethod.DNF to false,
            UnityCliInstallMethod.UNKNOWN to false,
        )
        assertEquals(UnityCliInstallMethod.entries.toSet(), expected.keys, "every install method needs one row")
        for ((method, updatable) in expected) {
            assertEquals(updatable, method.updatableFromRider, method.name)
        }
    }
}
