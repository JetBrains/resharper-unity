package com.jetbrains.rider.unity.test.cases.agents

import com.jetbrains.rider.plugins.unity.settings.agents.UNITY_PIPELINE_PACKAGE_ID
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpOperation
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpPackageStep
import com.jetbrains.rider.plugins.unity.settings.agents.UnityPipelineRegistry
import com.jetbrains.rider.plugins.unity.settings.agents.UnityPipelineRelease
import com.jetbrains.rider.plugins.unity.settings.agents.parseUnityEditorVersion
import com.jetbrains.rider.plugins.unity.settings.agents.parseUnityRegistry
import com.jetbrains.rider.plugins.unity.settings.agents.readUnityPipelineManifest
import com.jetbrains.rider.plugins.unity.settings.agents.unityManifestPath
import com.jetbrains.rider.plugins.unity.settings.agents.unityManifestWithDependency
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpPackageStep
import com.jetbrains.rider.plugins.unity.settings.agents.unityPipelineRegistryUrl
import com.jetbrains.rider.plugins.unity.settings.agents.unityProjectVersionPath
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
 * Pins the Unity Pipeline package row of the Unity MCP section, and the manifest and the registry of the package.
 *
 * The class reads no file and contacts no registry.
 */
@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity for Agents")
@Tag(TeamCityTags.Plugins.Unity.General)
class UnityMcpPipelineTest {
    private val solution: Path = Path.of("/Users/dev/projects/game")

    private data class StepCase(
        val name: String,
        val operation: UnityMcpOperation,
        val packagePresent: Boolean?,
        val agreed: Boolean,
        val expected: UnityMcpPackageStep,
    )

    private data class ManifestCase(
        val name: String,
        val content: String,
        val expected: String?,
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

    private val manifestCases = listOf(
        ManifestCase(
            "the package goes after the other dependencies",
            RIDER_MANIFEST,
            RIDER_AND_PIPELINE,
        ),
        ManifestCase(
            "the package goes last, and the other dependencies keep their order",
            """
            {
              "dependencies": {
                "com.unity.ide.rider": "3.0.40",
                "com.unity.ugui": "1.0.0"
              }
            }
            """.trimIndent(),
            """
            {
              "dependencies": {
                "com.unity.ide.rider": "3.0.40",
                "com.unity.ugui": "1.0.0",
                "com.unity.pipeline": "0.8.0-exp.1"
              }
            }
            """.trimIndent(),
        ),
        ManifestCase(
            "a present package takes the new version in its old place",
            """
            {
              "dependencies": {
                "com.unity.pipeline": "0.7.0-exp.1",
                "com.unity.ide.rider": "3.0.40"
              }
            }
            """.trimIndent(),
            """
            {
              "dependencies": {
                "com.unity.pipeline": "0.8.0-exp.1",
                "com.unity.ide.rider": "3.0.40"
              }
            }
            """.trimIndent(),
        ),
        ManifestCase(
            "a manifest without dependencies gets one, last",
            """
            {
              "testables": [
                "com.unity.ugui"
              ]
            }
            """.trimIndent(),
            """
            {
              "testables": [
                "com.unity.ugui"
              ],
              "dependencies": {
                "com.unity.pipeline": "0.8.0-exp.1"
              }
            }
            """.trimIndent(),
        ),
        ManifestCase(
            "the other top-level keys keep their order",
            """
            {
              "scopedRegistries": [
                {
                  "name": "Mirror",
                  "url": "https://mirror.example",
                  "scopes": [
                    "com.unity"
                  ]
                }
              ],
              "dependencies": {
                "com.unity.ide.rider": "3.0.40"
              },
              "testables": [
                "com.unity.ugui"
              ]
            }
            """.trimIndent(),
            """
            {
              "scopedRegistries": [
                {
                  "name": "Mirror",
                  "url": "https://mirror.example",
                  "scopes": [
                    "com.unity"
                  ]
                }
              ],
              "dependencies": {
                "com.unity.ide.rider": "3.0.40",
                "com.unity.pipeline": "0.8.0-exp.1"
              },
              "testables": [
                "com.unity.ugui"
              ]
            }
            """.trimIndent(),
        ),
        ManifestCase(
            "a manifest with another indent is written with 2 spaces",
            """
            {
                "dependencies": {
                    "com.unity.ide.rider": "3.0.40"
                }
            }
            """.trimIndent(),
            RIDER_AND_PIPELINE,
        ),
        ManifestCase(
            "a manifest that ends with a line break keeps it",
            RIDER_MANIFEST + "\n",
            RIDER_AND_PIPELINE + "\n",
        ),
        ManifestCase(
            "a CRLF manifest stays CRLF",
            RIDER_MANIFEST.crlf() + "\r\n",
            RIDER_AND_PIPELINE.crlf() + "\r\n",
        ),
        ManifestCase("an array is no manifest object", "[]", null),
        ManifestCase("text that is not JSON is no manifest object", "nope", null),
        ManifestCase("an empty text is no manifest object", "", null),
        ManifestCase(
            "a dependencies array is no dependencies object",
            """{"dependencies": ["com.unity.ugui"]}""",
            null,
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
    fun `the manifest names its registry at the top level`() {
        val manifest = """{"registry": "https://mirror.example/npm/", "dependencies": {}}"""
        assertEquals("https://mirror.example/npm/", readUnityPipelineManifest(manifest, fileExists = true)?.registry)
        assertNull(
            readUnityPipelineManifest("""{"dependencies": {}}""", fileExists = true)?.registry,
            "a manifest without a registry names none",
        )
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

    @Test
    fun `the registry document answers the published versions up to the latest, or nothing`() {
        assertEquals(
            UnityPipelineRegistry("0.9.1-exp.1", listOf(UnityPipelineRelease("0.9.1-exp.1", "6000.0"))),
            parseUnityRegistry("""{"dist-tags":{"latest":"0.9.1-exp.1"},"versions":{"0.9.1-exp.1":{"unity":"6000.0"}}}"""),
        )
        assertNull(
            parseUnityRegistry("""{"name":"com.unity.pipeline"}"""),
            "a document without dist-tags has no latest version",
        )
        assertNull(parseUnityRegistry("""{"dist-tags":{"latest":" "}}"""), "nor does a blank latest version")
        assertNull(parseUnityRegistry("not json"), "and text that is not JSON is no answer")
    }

    @Test
    fun `every published version up to the latest keeps its minimum Editor`() {
        val json = """
        {
          "dist-tags": {"latest": "0.9.1-exp.1"},
          "versions": {
            "0.8.0-exp.1": {"unity": "6000.0"},
            "0.9.1-exp.1": {"unity": "6000.0"}
          }
        }
        """.trimIndent()
        assertEquals(
            UnityPipelineRegistry(
                "0.9.1-exp.1",
                listOf(
                    UnityPipelineRelease("0.8.0-exp.1", "6000.0"),
                    UnityPipelineRelease("0.9.1-exp.1", "6000.0"),
                ),
            ),
            parseUnityRegistry(json),
        )
    }

    @Test
    fun `a version newer than the latest is left out`() {
        val json = """
        {
          "dist-tags": {"latest": "0.9.1-exp.1"},
          "versions": {
            "0.9.1-exp.1": {"unity": "6000.0"},
            "1.0.0-pre.1": {"unity": "6000.2"}
          }
        }
        """.trimIndent()
        assertEquals(
            UnityPipelineRegistry("0.9.1-exp.1", listOf(UnityPipelineRelease("0.9.1-exp.1", "6000.0"))),
            parseUnityRegistry(json),
        )
    }

    @Test
    fun `a version key that is no version is left out`() {
        val json = """
        {
          "dist-tags": {"latest": "0.9.1-exp.1"},
          "versions": {
            "main": {"unity": "6000.2"},
            "0.9.1-exp.1": {"unity": "6000.0"}
          }
        }
        """.trimIndent()
        assertEquals(
            UnityPipelineRegistry("0.9.1-exp.1", listOf(UnityPipelineRelease("0.9.1-exp.1", "6000.0"))),
            parseUnityRegistry(json),
        )
    }

    @Test
    fun `a latest version without an entry in the versions gives that version a null minimum`() {
        assertEquals(
            UnityPipelineRegistry("0.9.1-exp.1", listOf(UnityPipelineRelease("0.9.1-exp.1", null))),
            parseUnityRegistry("""{"dist-tags":{"latest":"0.9.1-exp.1"}}"""),
            "a document without versions names no minimum Editor",
        )
        val json = """
        {
          "dist-tags": {"latest": "0.9.1-exp.1"},
          "versions": {
            "0.8.0-exp.1": {"unity": "6000.0"}
          }
        }
        """.trimIndent()
        assertEquals(
            UnityPipelineRegistry(
                "0.9.1-exp.1",
                listOf(
                    UnityPipelineRelease("0.8.0-exp.1", "6000.0"),
                    UnityPipelineRelease("0.9.1-exp.1", null),
                ),
            ),
            parseUnityRegistry(json),
        )
    }

    @Test
    fun `a latest that is not a version gives no registry`() {
        assertNull(
            parseUnityRegistry("""{"dist-tags":{"latest":"https://evil.example/pkg.git"}}"""),
            "a git URL is no version, so Rider never writes it into the manifest",
        )
        assertNull(parseUnityRegistry("""{"dist-tags":{"latest":"file:../x"}}"""), "a file path is no version")
        assertNull(parseUnityRegistry("""{"dist-tags":{"latest":"latest"}}"""), "a tag name is no version")
        assertNull(parseUnityRegistry("""{"dist-tags":{}}"""), "a missing latest version gives no registry")
    }

    @Test
    fun `the Unity Editor version is the m_EditorVersion of the project version file`() {
        val content = "m_EditorVersion: 2022.3.62f1\nm_EditorVersionWithRevision: 2022.3.62f1 (4af31df58517)\n"
        assertEquals("2022.3.62f1", parseUnityEditorVersion(content))
        assertEquals("2022.3.62f1", parseUnityEditorVersion(content.replace("\n", "\r\n")))
        assertNull(parseUnityEditorVersion("m_ProjectName: game\n"), "text without the key has no Editor version")
        assertNull(parseUnityEditorVersion("m_EditorVersion: \n"), "an empty value is no Editor version")
    }

    @Test
    fun `the project version file sits under ProjectSettings in the solution`() {
        assertEquals(Path.of("/Users/dev/projects/game/ProjectSettings/ProjectVersion.txt"), unityProjectVersionPath(solution))
    }

    @Test
    fun `the registry of the package is the one of the manifest, or else the registry of Unity`() {
        assertEquals("https://packages.unity.com/com.unity.pipeline", unityPipelineRegistryUrl(null))
        assertEquals("https://mirror.example/npm/com.unity.pipeline", unityPipelineRegistryUrl("https://mirror.example/npm/"))
        assertEquals(
            "https://packages.unity.com/com.unity.pipeline",
            unityPipelineRegistryUrl("file:///tmp/registry"),
            "a file path is no registry",
        )
    }

    @Test
    fun `the dependency is written into the manifest text, and text that is no object is refused`() {
        for ((name, content, expected) in manifestCases) {
            assertEquals(
                expected,
                unityManifestWithDependency(content, UNITY_PIPELINE_PACKAGE_ID, "0.8.0-exp.1"),
                name,
            )
        }
    }

    @Test
    fun `the package id is the one the manifest holds`() {
        assertEquals("com.unity.pipeline", UNITY_PIPELINE_PACKAGE_ID)
    }

    private fun String.crlf(): String = replace("\n", "\r\n")

    private companion object {
        val RIDER_MANIFEST = """
            {
              "dependencies": {
                "com.unity.ide.rider": "3.0.40"
              }
            }
        """.trimIndent()

        val RIDER_AND_PIPELINE = """
            {
              "dependencies": {
                "com.unity.ide.rider": "3.0.40",
                "com.unity.pipeline": "0.8.0-exp.1"
              }
            }
        """.trimIndent()
    }
}
