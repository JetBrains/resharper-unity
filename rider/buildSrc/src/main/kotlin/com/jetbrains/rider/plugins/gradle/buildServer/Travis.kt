package com.jetbrains.rider.plugins.gradle.buildServer

import java.io.File

class Travis: BuildServer {

    override val isAutomatedBuild: Boolean
        get() = true

    override fun progress(message: String) {
        println(message)
    }

    override fun openBlock(name: String, description: String) {
        println("travis_fold:start:$name\u001b[33;1m:$description\u001b[0m")
    }

    override fun closeBlock(name: String) {
        println("\ntravis_fold:end:$name")
    }

    override fun publishArtifact(path: File) {
        println("Publish $path.absolutePath")
    }

    override fun setBuildNumber(version: String) {
        println("Build $version")
    }
}