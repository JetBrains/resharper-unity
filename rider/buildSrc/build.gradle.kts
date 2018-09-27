import org.jetbrains.kotlin.ir.backend.js.compile
import java.net.URI

repositories {
    maven { url = URI("https://cache-redirector.jetbrains.com/maven-central") }
}

plugins {
    `kotlin-dsl`
}

