package com.jetbrains.rider.unity.test.cases.agents

import com.intellij.execution.process.ProcessOutput
import com.jetbrains.rider.plugins.unity.settings.agents.UNITY_PIPELINE_PACKAGE_ID
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpFailure
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpPackageStep
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpPackageStep
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpOperation
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpSectionPresentation
import com.jetbrains.rider.plugins.unity.settings.agents.UnityPipelineManifest
import com.jetbrains.rider.plugins.unity.settings.agents.readUnityPipelineManifest
import com.jetbrains.rider.plugins.unity.settings.agents.unityManifestPath
import com.jetbrains.rider.plugins.unity.settings.agents.unityPipelineInstallCommandLine
import com.jetbrains.rider.plugins.unity.settings.agents.unityPipelineInstallOutcome
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Assertions.assertFalse
import org.junit.jupiter.api.Assertions.assertNull
import org.junit.jupiter.api.Assertions.assertTrue
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test
import java.nio.file.Path

/**
 * Pins the Unity Pipeline package row of the Unity MCP section.
 *
 * The class runs no process and it reads no file. Every output shape below was measured on
 * 2026-09-29, against `~/.unity/bin/unity`, version `1.0.0-beta.11`.
 */
@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity for Agents")
@Tag(TeamCityTags.Plugins.Unity.General)
class UnityMcpPipelineTest {
    private val unity: Path = Path.of("/Users/dev/.unity/bin/unity")
    private val solution: Path = Path.of("/Users/dev/projects/game")

    /**
     * The manifest is the only thing the section reads about the package.
     *
     * Scope call 23 took the row that used to report it, so this one value decides whether the user
     * is asked at all. The section draws nothing else about the package.
     */
    @Test
    fun `the section asks for the package only where the project holds none`() {
        assertEquals(
            true,
            section(UnityPipelineManifest(version = null)).needsPipelinePackage,
            "an absent package needs the install",
        )
        assertEquals(
            false,
            section(UnityPipelineManifest(version = "0.8.0-exp.1")).needsPipelinePackage,
            "a package in the manifest needs nothing",
        )
        assertNull(
            section(null).needsPipelinePackage,
            "a read that has not returned is no answer, and the page must not ask on it",
        )
    }

    private fun section(manifest: UnityPipelineManifest?) = UnityMcpSectionPresentation(pipelinePackage = manifest)

    private data class StepCase(
        val name: String,
        val operation: UnityMcpOperation,
        val packagePresent: Boolean?,
        val agreed: Boolean,
        val expected: UnityMcpPackageStep,
    )

    /**
     * A configure installs the package first, and the manifest decides whether it has to.
     *
     * The order is the whole point of scope call 21: a store written before the package gives a
     * green row and a dead server, which is the defect ticket 25 found in a running Rider.
     */
    private val stepCases = listOf(
        StepCase(
            "a project with no package, and the user agreed",
            UnityMcpOperation.CONFIGURING, packagePresent = false, agreed = true,
            expected = UnityMcpPackageStep.INSTALL,
        ),
        StepCase(
            "a project with no package, and the user declined",
            UnityMcpOperation.CONFIGURING, packagePresent = false, agreed = false,
            expected = UnityMcpPackageStep.ABORT,
        ),
        StepCase(
            "a project that already holds the package installs nothing",
            UnityMcpOperation.CONFIGURING, packagePresent = true, agreed = true,
            expected = UnityMcpPackageStep.NONE,
        ),
        StepCase(
            "a project that already holds the package never asks, so the answer cannot matter",
            UnityMcpOperation.CONFIGURING, packagePresent = true, agreed = false,
            expected = UnityMcpPackageStep.NONE,
        ),
        // A removal takes an entry away. Installing a package for it would be absurd.
        StepCase(
            "a removal on a project with no package installs nothing",
            UnityMcpOperation.REMOVING, packagePresent = false, agreed = true,
            expected = UnityMcpPackageStep.NONE,
        ),
        StepCase(
            "a removal never aborts on the package",
            UnityMcpOperation.REMOVING, packagePresent = false, agreed = false,
            expected = UnityMcpPackageStep.NONE,
        ),
        StepCase(
            "a removal on a project that holds the package",
            UnityMcpOperation.REMOVING, packagePresent = true, agreed = true,
            expected = UnityMcpPackageStep.NONE,
        ),
        StepCase(
            "a removal on a project that holds the package, with no answer",
            UnityMcpOperation.REMOVING, packagePresent = true, agreed = false,
            expected = UnityMcpPackageStep.NONE,
        ),
        // A manifest that did not answer is the case the branch review found. The page never asks on
        // it, so an agreement cannot exist, and a write that installed on it would edit the project
        // of the user with no confirmation.
        StepCase(
            "a manifest that did not answer stops the configure",
            UnityMcpOperation.CONFIGURING, packagePresent = null, agreed = false,
            expected = UnityMcpPackageStep.ABORT,
        ),
        StepCase(
            "a manifest that did not answer stops the configure, whatever the page passed",
            UnityMcpOperation.CONFIGURING, packagePresent = null, agreed = true,
            expected = UnityMcpPackageStep.ABORT,
        ),
        StepCase(
            "a removal needs no manifest",
            UnityMcpOperation.REMOVING, packagePresent = null, agreed = false,
            expected = UnityMcpPackageStep.NONE,
        ),
        StepCase(
            "a removal needs no manifest, whatever the page passed",
            UnityMcpOperation.REMOVING, packagePresent = null, agreed = true,
            expected = UnityMcpPackageStep.NONE,
        ),
    )

    @Test
    fun `a configure installs the package first, and only where the manifest lacks it`() {
        for (case in stepCases) {
            assertEquals(
                case.expected,
                unityMcpPackageStep(case.operation, case.packagePresent, case.agreed),
                case.name,
            )
        }
    }

    @Test
    fun `every combination of the package rule has a case`() {
        val all = UnityMcpOperation.entries.flatMap { op ->
            // The manifest is a tri-state. `null` is "it did not answer", and it is not "absent".
            listOf(true, false, null).flatMap { present -> listOf(true, false).map { Triple(op, present, it) } }
        }
        assertEquals(
            all.toSet(),
            stepCases.map { Triple(it.operation, it.packagePresent, it.agreed) }.toSet(),
        )
    }

    @Test
    fun `every step of the package rule has a case`() {
        assertEquals(UnityMcpPackageStep.entries.toSet(), stepCases.map { it.expected }.toSet())
    }

    @Test
    fun `the manifest of the solution names the package and its version`() {
        val manifest = """
        {
          "dependencies": {
            "com.unity.ide.rider": "3.0.40",
            "com.unity.pipeline": "0.8.0-exp.1"
          }
        }
        """.trimIndent()
        assertEquals("0.8.0-exp.1", readUnityPipelineManifest(manifest, fileExists = true)?.version)
        assertTrue(readUnityPipelineManifest(manifest, fileExists = true)?.present == true)
    }

    @Test
    fun `a manifest without the package is an answer`() {
        val manifest = """{"dependencies": {"com.unity.ide.rider": "3.0.40"}}"""
        val read = readUnityPipelineManifest(manifest, fileExists = true)
        assertNull(read?.version, "the version is null")
        assertFalse(read?.present == true, "and the package is absent")
    }

    /** A missing manifest is an answer, and an unreadable one is not. This is the rule of the stores. */
    @Test
    fun `a missing manifest answers absent and an unreadable one answers nothing`() {
        assertFalse(readUnityPipelineManifest(null, fileExists = false)?.present == true, "no manifest, no package")
        assertNull(readUnityPipelineManifest(null, fileExists = true), "Rider could not read it, so it says nothing")
        assertNull(readUnityPipelineManifest("{ not json", fileExists = true), "and broken text says nothing")
    }

    @Test
    fun `the manifest sits under Packages in the solution`() {
        assertEquals(Path.of("/Users/dev/projects/game/Packages/manifest.json"), unityManifestPath(solution))
    }

    /**
     * The manifest decides, and the exit code only explains a failure.
     *
     * The install exits 0 and reports `alreadyInstalled: true` for a project that holds the package,
     * so a zero exit code proves nothing on its own.
     */
    @Test
    fun `the manifest decides whether the install landed`() {
        assertNull(unityPipelineInstallOutcome(output(0), inManifest = true), "the manifest holds it, so it landed")
        assertNull(unityPipelineInstallOutcome(output(6), inManifest = true), "even after a nonzero exit code")
        assertEquals(
            UnityMcpFailure.EXIT_CODE,
            unityPipelineInstallOutcome(output(6), inManifest = false),
            "a nonzero code explains a manifest that did not change",
        )
        assertEquals(
            UnityMcpFailure.NOT_CONFIRMED,
            unityPipelineInstallOutcome(output(0), inManifest = false),
            "a zero code with no change is the unconfirmed case",
        )
        assertEquals(
            UnityMcpFailure.NOT_CONFIRMED,
            unityPipelineInstallOutcome(output(0), inManifest = null),
            "and a manifest Rider could not read is not a confirmation",
        )
    }

    @Test
    fun `a process that did not start is a launch failure`() {
        assertEquals(UnityMcpFailure.LAUNCH_FAILED, unityPipelineInstallOutcome(null, inManifest = true))
        val timeout = ProcessOutput().apply { setTimeout() }
        assertEquals(UnityMcpFailure.LAUNCH_FAILED, unityPipelineInstallOutcome(timeout, inManifest = true))
    }

    @Test
    fun `the install command names the project and asks no question`() {
        val command = unityPipelineInstallCommandLine(unity, solution)
        assertEquals(unity.toString(), command.exePath)
        assertEquals(
            listOf("pipeline", "install", "--project-path", solution.toString(), "--non-interactive", "--json"),
            command.parametersList.parameters,
        )
        assertEquals(solution.toFile(), command.workDirectory)
    }

    @Test
    fun `the package id is the one the manifest holds`() {
        assertEquals("com.unity.pipeline", UNITY_PIPELINE_PACKAGE_ID)
    }

    private fun output(exitCode: Int): ProcessOutput = ProcessOutput().apply { this.exitCode = exitCode }
}
