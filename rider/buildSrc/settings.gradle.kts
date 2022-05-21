// Affects the repositories used to resolve the plugins { } block
pluginManagement {
    repositories {
        maven { setUrl("https://cache-redirector.jetbrains.com/plugins.gradle.org") }
        // This is for snapshot version of 'org.jetbrains.intellij' plugin
        maven { setUrl("https://oss.sonatype.org/content/repositories/snapshots/") }
    }
}
