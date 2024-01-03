plugins {
    kotlin("jvm")
}

val rdLibDirectory: () -> File by rootProject.extra
val monoRepoRootDir: File? by rootProject.extra


repositories {
    maven { setUrl { "https://cache-redirector.jetbrains.com/maven-central" } }
    if (monoRepoRootDir == null) {
        flatDir {
            dir(rdLibDirectory())
        }
    }
}

dependencies {
    implementation("org.jetbrains.kotlin:kotlin-stdlib")
    if (monoRepoRootDir == null) {
        implementation("", "rider-model")
        implementation("", "rd-gen")
    }
}
