plugins {
    kotlin("jvm")
}

val rdLibDirectory: () -> File by rootProject.extra

repositories {
    maven { setUrl { "https://cache-redirector.jetbrains.com/maven-central" } }
    flatDir {
        dir(rdLibDirectory())
    }
}

dependencies {
    implementation("org.jetbrains.kotlin:kotlin-stdlib")
    implementation("", "rider-model")
    implementation("", "rd-gen")
}
