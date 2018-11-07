package com.jetbrains.rider.plugins.unity.util

import kotlin.math.min

data class SemVer(val major: Int, val minor: Int, val patch: Int, val prerelease: String?, val buildMetadata: String?)
    : Comparable<SemVer> {

    companion object {
        fun parse(version: String): SemVer? {
            // This isn't exactly strict, but will match a well formed version
            val pattern = Regex("""(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?:-(?<prerelease>[0-9A-Za-z\-]+(?:\.([0-9A-Za-z\-])+)*)(?:\+(?<build>[0-9A-Za-z\-.]+))?)?""")
            val match = pattern.matchEntire(version)
            return match?.let {
                val major = it.groups["major"]?.value?.toInt() ?: 0
                val minor = it.groups["minor"]?.value?.toInt() ?: 0
                val patch = it.groups["patch"]?.value?.toInt() ?: 0
                val prerelease = it.groups["prerelease"]?.value
                val build = it.groups["build"]?.value
                SemVer(major, minor, patch, prerelease, build)
            }
        }
    }

    override fun toString(): String {
        var result = "$major.$minor.$patch"
        if (prerelease != null) {
            result += "-$prerelease"
        }
        if (buildMetadata != null) {
            result += "+$buildMetadata"
        }
        return result
    }

    override fun compareTo(other: SemVer): Int {
        if (major > other.major) return 1
        if (major < other.major) return -1
        if (minor > other.minor) return 1
        if (minor < other.minor) return -1
        if (patch > other.patch) return 1
        if (patch < other.patch) return -1

        // Non-null pre-release has lower precedence
        if (prerelease == null && other.prerelease == null) return 0
        if (other.prerelease == null) return -1
        if (prerelease == null) return 1

        val segments = prerelease.split(".")
        val otherSegments = other.prerelease.split(".")

        val commonSegments = min(segments.size, otherSegments.size)
        for (i in 0..(commonSegments - 1)) {
            val segment = segments[i]
            val otherSegment = otherSegments[i]

            val digitVal = segment.toIntOrNull()
            val otherDigitVal = segment.toIntOrNull()

            // Numeric has lower precedence than non-numeric
            if (digitVal == null && otherDigitVal != null) return 1
            if (digitVal != null && otherDigitVal == null) return -1

            if (digitVal != null && otherDigitVal != null) {
                if (digitVal > otherDigitVal) return 1
                if (digitVal < otherDigitVal) return -1
            }
            else {
                if (segment > otherSegment) return 1
                if (segment < otherSegment) return -1
            }
        }

        // More segments has higher precedence
        if (segments.size > otherSegments.size) return 1
        if (segments.size < otherSegments.size) return -1

        return 0
    }
}