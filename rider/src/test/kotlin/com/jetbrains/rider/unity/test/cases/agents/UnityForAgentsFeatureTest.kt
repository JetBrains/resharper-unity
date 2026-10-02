package com.jetbrains.rider.unity.test.cases.agents

import com.intellij.mock.MockProject
import com.intellij.openapi.Disposable
import com.intellij.openapi.util.registry.Registry
import com.intellij.testFramework.junit5.RegistryKey
import com.intellij.testFramework.junit5.TestApplication
import com.intellij.testFramework.junit5.TestDisposable
import com.jetbrains.rider.plugins.unity.settings.agents.UnityForAgentsConfigurableProvider
import com.jetbrains.rider.plugins.unity.settings.agents.UnityForAgentsFeature
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Assertions.assertFalse
import org.junit.jupiter.api.Assertions.assertTrue
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test

/**
 * The kill switch of the Unity for Agents feature, and the page it takes away.
 *
 * The feature ships dark, so the default of `rider.unity.agents.enabled` is false and no release
 * build shows the page until somebody turns the key on. `rider.unity.suggest.cli.install` is the
 * narrower key, and it silences the project-open balloon alone.
 *
 * Both keys write their default twice, in the XML and as a fallback in the code, and nothing but the
 * two tests below checks that the pairs agree.
 *
 * The project is a [MockProject] and not a `projectFixture`. The provider reads
 * [com.intellij.openapi.project.Project.isDefault] and nothing else, and a real project in this
 * module loads `RiderDefaultBackend`, which needs a solution and fails without one.
 */
@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity for Agents")
@Tag(TeamCityTags.Plugins.Unity.General)
@TestApplication
class UnityForAgentsFeatureTest {
    /**
     * The default is written twice, and nothing else checks that the two agree.
     *
     * `UnityForAgentsFeature.isEnabled` carries a fallback and
     * `intellij.rider.plugins.unity.backend.xml` carries a `defaultValue`. A change to one is a
     * change to both, and a feature that is off in the declaration and on in the code ships to
     * every user.
     *
     * The declared value is read with [Registry.getBundleValueOrNull] and **not** with
     * `Registry.is(key, false)`. The second one answers with the fallback of the caller until the
     * components are loaded, so both sides of the assertion would collapse to the same literal and
     * the test would pass whatever the declaration says. `@TestApplication` above is what loads them.
     */
    @Test
    fun `the default of the declaration and the default of the code agree`() {
        val declared = Registry.getInstance().getBundleValueOrNull(UnityForAgentsFeature.ENABLED_KEY)

        assertEquals("false", declared, "the declaration of ${UnityForAgentsFeature.ENABLED_KEY} is not false")
        assertFalse(UnityForAgentsFeature.isEnabled(), "the code reads a default the declaration does not carry")
    }

    /**
     * The same guard for the narrower key, which silences the project-open balloon alone.
     *
     * Its default is true, and it is written twice as well. The kill switch has to be on to read it:
     * [UnityForAgentsFeature.isCliSuggestionEnabled] answers false while the feature is off, whatever
     * this key says, which is the rule and not a defect.
     */
    @Test
    @RegistryKey(key = UnityForAgentsFeature.ENABLED_KEY, value = "true")
    fun `the default of the balloon key and the default of the code agree`() {
        val key = UnityForAgentsFeature.SUGGEST_CLI_INSTALL_KEY
        val declared = Registry.getInstance().getBundleValueOrNull(key)

        assertEquals("true", declared, "the declaration of $key is not true")
        assertTrue(UnityForAgentsFeature.isCliSuggestionEnabled(), "the code reads a default the declaration lacks")
    }

    /** A [com.intellij.openapi.options.Configurable] has no hook that hides it, so the page must never be created. */
    @Test
    fun `the settings page is not created while the feature is off`(@TestDisposable disposable: Disposable) {
        val provider = UnityForAgentsConfigurableProvider(MockProject(null, disposable))

        assertFalse(provider.canCreateConfigurable(), "a feature that is off still offered its page")
    }

    @Test
    @RegistryKey(key = UnityForAgentsFeature.ENABLED_KEY, value = "true")
    fun `the settings page is created once the feature is on`(@TestDisposable disposable: Disposable) {
        val provider = UnityForAgentsConfigurableProvider(MockProject(null, disposable))

        assertTrue(provider.canCreateConfigurable(), "a feature that is on offered no page")
    }
}
