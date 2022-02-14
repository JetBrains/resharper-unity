// Note: The `buildscript` block affects the dependencies ONLY for the build scripts of the buildSrc projects
// (i.e. buildSrc/build.gradle.kts et al)
// Use top level `repositories` and `dependencies` for the buildSrc project itself. Also note that the dependencies of
// the buildSrc project become dependencies for the root project, too

repositories {
    maven { setUrl("https://cache-redirector.jetbrains.com/maven-central") }
    maven { setUrl("https://cache-redirector.jetbrains.com/plugins.gradle.org") }
}

dependencies {
    implementation("org.jetbrains.intellij.plugins", "gradle-intellij-plugin", "1.4.0")
}

plugins {
    `kotlin-dsl`
}
