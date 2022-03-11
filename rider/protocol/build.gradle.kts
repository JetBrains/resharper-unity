import com.jetbrains.rider.plugins.gradle.BackendPaths

plugins {
    kotlin("jvm")
}

val backend: BackendPaths = gradle.rootProject.extra["backend"] as BackendPaths

repositories {
    maven { setUrl { "https://cache-redirector.jetbrains.com/maven-central" } }
    flatDir {
        dir({ backend.getRdLibDirectory() })
    }
}

dependencies {
    implementation("org.jetbrains.kotlin:kotlin-stdlib")
    implementation("", "rider-model")
    implementation("", "rd-gen")
}
