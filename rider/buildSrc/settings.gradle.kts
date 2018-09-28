import java.net.URI

// Affects the repositories used to resolve the plugins { } block
pluginManagement {
    repositories {
        maven { url = URI("https://cache-redirector.jetbrains.com/plugins.gradle.org") }
    }
}
