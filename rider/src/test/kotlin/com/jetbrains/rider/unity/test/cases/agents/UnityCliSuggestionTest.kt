package com.jetbrains.rider.unity.test.cases.agents

import com.intellij.mock.MockProject
import com.intellij.notification.DoNotAskAppManager
import com.intellij.notification.Notification
import com.intellij.openapi.Disposable
import com.intellij.openapi.application.ApplicationManager
import com.intellij.testFramework.junit5.RegistryKey
import com.intellij.testFramework.junit5.TestApplication
import com.intellij.testFramework.junit5.TestDisposable
import com.intellij.testFramework.replaceService
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliDeclines
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliEnvironment
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliFailure
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliInstallMethod
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliOperation
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliPresentation
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliSuggestion
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliSuggestionActivity
import com.jetbrains.rider.plugins.unity.settings.agents.UnityForAgentsFeature
import com.jetbrains.rider.plugins.unity.settings.agents.unityCliSuggestionActs
import com.jetbrains.rider.plugins.unity.settings.agents.unityCliSuggestion
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import kotlinx.coroutines.runBlocking
import org.junit.jupiter.api.AfterEach
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Assertions.assertFalse
import org.junit.jupiter.api.Assertions.assertNotEquals
import org.junit.jupiter.api.Assertions.assertTrue
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test
import java.nio.file.Path

/**
 * Which balloon the project-open sweep raises, and when it raises none.
 *
 * The rule is a pure function, so the table below needs no project and no application. That is
 * deliberate: a real project in this module loads `RiderDefaultBackend`, which asks for a solution
 * and fails without one.
 *
 * The two flag cases drive the real activity with a [MockProject], because the gate returns before
 * the activity reads anything else from the project.
 */
@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity for Agents")
@Tag(TeamCityTags.Plugins.Unity.General)
@TestApplication
class UnityCliSuggestionTest {
    private data class Case(
        val name: String,
        val presentation: UnityCliPresentation,
        val installDeclined: Boolean = false,
        val updateDeclined: Boolean = false,
        val expected: UnityCliSuggestion,
    )

    private val installed = Path.of("/opt/homebrew/bin/unity")

    private val cases: List<Case> = listOf(
        Case(
            "no Unity CLI exists, so Rider offers the install",
            UnityCliPresentation(detected = true),
            expected = UnityCliSuggestion.INSTALL,
        ),
        Case(
            "the user declined the install, so Rider stays quiet for ever",
            UnityCliPresentation(detected = true),
            installDeclined = true,
            expected = UnityCliSuggestion.NONE,
        ),
        Case(
            "a newer copy exists and Rider can update it",
            outdated(UnityCliInstallMethod.HOMEBREW),
            expected = UnityCliSuggestion.UPDATE,
        ),
        Case(
            "the user declined the update",
            outdated(UnityCliInstallMethod.HOMEBREW),
            updateDeclined = true,
            expected = UnityCliSuggestion.NONE,
        ),
        // The balloon does not stay silent about a tool problem it cannot fix. The page names the
        // command that needs sudo, because the balloon only points.
        Case(
            "apt owns the copy, and Rider still says a newer one exists",
            outdated(UnityCliInstallMethod.APT),
            expected = UnityCliSuggestion.UPDATE,
        ),
        Case(
            "the install decline does not silence the update, and the reverse holds too",
            outdated(UnityCliInstallMethod.HOMEBREW),
            installDeclined = true,
            expected = UnityCliSuggestion.UPDATE,
        ),
        Case(
            "the update decline does not silence the install",
            UnityCliPresentation(detected = true),
            updateDeclined = true,
            expected = UnityCliSuggestion.INSTALL,
        ),
        // Rider stays quiet in the background sweep about what it cannot say. No decline flag is
        // written either, so the next project open asks again.
        Case(
            "an unread version feed raises nothing",
            UnityCliPresentation(detected = true, installedVersion = "1.0.0-beta.9", location = installed),
            expected = UnityCliSuggestion.NONE,
        ),
        Case(
            "the installed copy is the latest one",
            UnityCliPresentation(
                detected = true,
                installedVersion = "1.0.0-beta.10",
                location = installed,
                latestVersion = "1.0.0-beta.10",
            ),
            expected = UnityCliSuggestion.NONE,
        ),
        // The trust gate belongs on the act, and the install carries it. A user who never trusted
        // the project still learns that the Unity CLI is missing.
        Case(
            "an untrusted project still learns that the Unity CLI is missing",
            UnityCliPresentation(detected = true, projectTrusted = false),
            expected = UnityCliSuggestion.INSTALL,
        ),
        Case(
            "an untrusted project still learns that a newer copy exists",
            outdated(UnityCliInstallMethod.HOMEBREW).copy(projectTrusted = false),
            expected = UnityCliSuggestion.UPDATE,
        ),
        Case(
            "the detection has not finished",
            UnityCliPresentation(),
            expected = UnityCliSuggestion.NONE,
        ),
        Case(
            "an install runs now",
            UnityCliPresentation(detected = true, operation = UnityCliOperation.INSTALLING),
            expected = UnityCliSuggestion.NONE,
        ),
        // The sweep never reports a failure. The error balloon owns that, in a group of its own, and
        // these decline flags never silence it.
        Case(
            "the last install stopped",
            UnityCliPresentation(detected = true, failure = UnityCliFailure.INSTALLER_EXIT_CODE, failureExitCode = 1),
            expected = UnityCliSuggestion.NONE,
        ),
    )

    @AfterEach
    fun forgetTheDeclines() {
        val declines = DoNotAskAppManager.getInstance()
        declines.clearDoNotAsk(UnityCliDeclines.INSTALL_DISPLAY_ID)
        declines.clearDoNotAsk(UnityCliDeclines.UPDATE_DISPLAY_ID)
    }

    @Test
    fun eachStateProducesItsSuggestion() {
        for ((name, presentation, installDeclined, updateDeclined, expected) in cases) {
            assertEquals(expected, unityCliSuggestion(presentation, installDeclined, updateDeclined), name)
        }
    }

    /** Fails when [UnityCliSuggestion] gains a value that no case above reaches. */
    @Test
    fun everySuggestionValueHasACase() {
        assertEquals(
            UnityCliSuggestion.entries.toSet(),
            cases.map { it.expected }.toSet(),
            "every suggestion needs one case",
        )
    }

    /**
     * A decline is written for the whole application, and it survives the next project open.
     *
     * Three of the four declines of this plugin are project level, and this one must not be. A
     * project-level decline would be a silent defect: the balloon would return in the next project.
     *
     * The case declines through the platform, exactly as the balloon action does, so it also proves
     * that the display id keys the entry. A shared key would silence both balloons at once.
     */
    @Test
    fun aDeclineIsWrittenForTheWholeApplication() {
        assertEquals(UnityCliDeclines.INSTALL_DISPLAY_ID, UnityCliDeclines.displayIdOf(UnityCliSuggestion.INSTALL))
        assertEquals(UnityCliDeclines.UPDATE_DISPLAY_ID, UnityCliDeclines.displayIdOf(UnityCliSuggestion.UPDATE))
        assertNotEquals(
            UnityCliDeclines.INSTALL_DISPLAY_ID,
            UnityCliDeclines.UPDATE_DISPLAY_ID,
            "one display id would silence both balloons at once",
        )
        assertFalse(UnityCliDeclines.isDeclined(null, UnityCliDeclines.INSTALL_DISPLAY_ID))

        declineTheInstallBalloon()

        // The application store holds it, with no project argument anywhere.
        assertTrue(Notification.isDoNotAskFor(null, UnityCliDeclines.INSTALL_DISPLAY_ID))
        // A second project open reads the same decline and raises nothing.
        assertEquals(
            UnityCliSuggestion.NONE,
            unityCliSuggestion(
                UnityCliPresentation(detected = true),
                installDeclined = UnityCliDeclines.isDeclined(null, UnityCliDeclines.INSTALL_DISPLAY_ID),
                updateDeclined = false,
            ),
        )
        // The update decline is untouched, because the two are separate switches.
        assertFalse(UnityCliDeclines.isDeclined(null, UnityCliDeclines.UPDATE_DISPLAY_ID))
    }

    @Test
    fun `the balloon installs by itself`() {
        assertTrue(
            unityCliSuggestionActs(UnityCliSuggestion.INSTALL, UnityCliPresentation(detected = true)),
            "the install balloon lost its action",
        )
    }

    @Test
    fun `the balloon updates a copy Rider owns`() {
        for (method in listOf(UnityCliInstallMethod.CDN, UnityCliInstallMethod.HOMEBREW, UnityCliInstallMethod.WINGET)) {
            val presentation = UnityCliPresentation(detected = true, installMethod = method)

            assertTrue(
                unityCliSuggestionActs(UnityCliSuggestion.UPDATE, presentation),
                "the update balloon lost its action on $method",
            )
        }
    }

    @Test
    fun `the balloon offers no update where Rider needs sudo`() {
        // `apt` and `dnf` reach the balloon as UPDATE, through SHOW_UPDATE_COMMAND. Rider does not
        // elevate, so an Update action there would call an update that refuses. The balloon points
        // at the page instead, and the page names the command.
        for (method in listOf(UnityCliInstallMethod.APT, UnityCliInstallMethod.DNF)) {
            val presentation = UnityCliPresentation(detected = true, installMethod = method)

            assertFalse(
                unityCliSuggestionActs(UnityCliSuggestion.UPDATE, presentation),
                "the balloon offered an update that Rider cannot run on $method",
            )
        }
    }

    /**
     * Declines the install prompt the way the checkbox of the dialog does.
     *
     * `null` for the project, so the entry lands in the store of the application. The dialog writes
     * it through [DoNotAskAppManager] and [UnityCliDeclines.isDeclined] reads the same store through
     * [Notification.isDoNotAskFor], which is the point of the case below.
     */
    private fun declineTheInstallBalloon() {
        DoNotAskAppManager.getInstance().markDoNotAsk(
            UnityCliDeclines.INSTALL_DISPLAY_ID,
            UnityBundle.message("unity.agents.cli.suggest.install.title"),
        )
    }

    @Test
    fun theKillSwitchStopsTheActivityBeforeItDetectsAnything(@TestDisposable disposable: Disposable) {
        val lookups = installRecordingEnvironment(disposable)

        runBlocking { UnityCliSuggestionActivity().execute(MockProject(null, disposable)) }

        assertEquals(0, lookups.size, "a feature that is off still looked for the Unity CLI: $lookups")
    }

    @Test
    @RegistryKey(key = UnityForAgentsFeature.ENABLED_KEY, value = "true")
    @RegistryKey(key = UnityForAgentsFeature.SUGGEST_CLI_INSTALL_KEY, value = "false")
    fun theBalloonKeyStopsTheActivityWhileTheFeatureIsOn(@TestDisposable disposable: Disposable) {
        assertTrue(UnityForAgentsFeature.isEnabled(), "the case needs the feature on")
        val lookups = installRecordingEnvironment(disposable)

        runBlocking { UnityCliSuggestionActivity().execute(MockProject(null, disposable)) }

        assertEquals(0, lookups.size, "a silenced balloon still looked for the Unity CLI: $lookups")
    }

    /** Records every lookup of the `PATH`, which is the first thing the detection does. */
    private fun installRecordingEnvironment(disposable: Disposable): List<String> {
        val lookups = mutableListOf<String>()
        val environment = UnityCliEnvironment.environmentAnswering(
            findOnPath = { name ->
                lookups += name
                null
            },
        )
        ApplicationManager.getApplication().replaceService(UnityCliEnvironment::class.java, environment, disposable)
        return lookups
    }

    private fun outdated(method: UnityCliInstallMethod): UnityCliPresentation = UnityCliPresentation(
        detected = true,
        installedVersion = "1.0.0-beta.9",
        location = installed,
        installMethod = method,
        latestVersion = "1.0.0-beta.10",
        updateCommand = "brew upgrade unity-cli",
    )
}
