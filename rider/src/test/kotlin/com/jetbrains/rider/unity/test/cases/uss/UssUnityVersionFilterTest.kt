package com.jetbrains.rider.unity.test.cases.uss

import com.intellij.openapi.util.Version
import com.jetbrains.rider.plugins.unity.css.uss.codeInsight.css.filterByUnityVersion
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Assertions.assertTrue
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test
import java.util.Properties
import javax.xml.stream.XMLInputFactory
import javax.xml.stream.XMLStreamConstants

@Tag(TeamCityTags.Plugins.Unity.General)
class UssUnityVersionFilterTest {
    private val unitySince = mapOf(
        "transition" to Version(2021, 3),
        "background-size" to Version(2022, 3),
        "filter" to Version(6000, 3),
        "grid-row" to Version(7000, 0),
    )
    private val properties = listOf("width", "transition", "background-size", "filter", "grid-row")

    private fun filter(unityVersion: String?) = filterByUnityVersion(properties, unitySince, unityVersion?.let { Version.parseVersion(it)!! }) { it }

    @Test
    fun hidesPropertiesThatAreNewerThanTheUnityVersion() {
        assertEquals(listOf("width", "transition", "background-size"), filter("2022.3"))
    }

    @Test
    fun keepsAllPropertiesForTheNewestUnityVersion() {
        assertEquals(properties, filter("7000.0"))
    }

    @Test
    fun keepsAllPropertiesWhenTheUnityVersionIsUnknown() {
        assertEquals(properties, filter(null))
    }

    @Test
    fun keepsOnlyOldPropertiesForAnOldUnityVersion() {
        assertEquals(listOf("width"), filter("2019.4"))
    }

    @Test
    fun ignoresTheBugfixAndSuffixOfTheUnityVersion() {
        assertEquals(listOf("width", "transition", "background-size", "filter"), filter("6000.3.1f1"))
        assertEquals(listOf("width", "transition", "background-size"), filter("6000.2.9a1"))
    }

    @Test
    fun unitySinceVersionsReferToSchemaProperties() {
        val versions = Properties()
        javaClass.getResourceAsStream("/uss/unity-since.properties")!!.use { versions.load(it) }
        assertTrue(versions.isNotEmpty())
        val unknownIds = versions.stringPropertyNames() - readSchemaPropertyIds()
        assertEquals(emptySet<String>(), unknownIds, "unity-since.properties has ids that element-descriptors.xml does not declare")
        val invalidVersions = versions.stringPropertyNames().filterNot { id ->
            val since = versions.getProperty(id)
            since.matches(Regex("\\d{4}\\.\\d+")) && Version.parseVersion(since) != null
        }
        assertEquals(emptyList<String>(), invalidVersions, "unity-since.properties has values that are not Unity versions")
    }

    private fun readSchemaPropertyIds(): Set<String> {
        val ids = mutableSetOf<String>()
        javaClass.getResourceAsStream("/uss/element-descriptors.xml")!!.use { stream ->
            val reader = XMLInputFactory.newDefaultFactory().createXMLStreamReader(stream)
            while (reader.hasNext()) {
                if (reader.next() == XMLStreamConstants.START_ELEMENT && reader.localName == "property") {
                    reader.getAttributeValue(null, "id")?.let { ids.add(it) }
                }
            }
            reader.close()
        }
        return ids
    }
}
