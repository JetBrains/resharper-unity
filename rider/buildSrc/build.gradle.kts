// Note: The `buildscript` block affects the dependencies ONLY for the build scripts of the buildSrc projects
// (i.e. buildSrc/build.gradle.kts et al)
// Use top level `repositories` and `dependencies` for the buildSrc project itself. Also note that the dependencies of
// the buildSrc project become dependencies for the root project, too

repositories {
    maven { setUrl("https://cache-redirector.jetbrains.com/maven-central") }
    maven { setUrl("https://cache-redirector.jetbrains.com/plugins.gradle.org") }
    // This is for snapshot version of 'org.jetbrains.intellij' plugin
    maven { setUrl("https://oss.sonatype.org/content/repositories/snapshots/") }
}

dependencies {
    implementation("org.jetbrains.intellij", "org.jetbrains.intellij.gradle.plugin", "1.6.0")
}

plugins {
    `kotlin-dsl`
}
