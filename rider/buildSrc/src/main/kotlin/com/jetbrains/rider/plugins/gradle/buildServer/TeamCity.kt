package com.jetbrains.rider.plugins.gradle.buildServer

import java.io.File

// TODO: Make sure the messages are escaped correctly
class TeamCity: BuildServer {

    override val isAutomatedBuild
        get() = true

    override fun progress(message: String) {
        println("##teamcity[progressMessage '$message']")
    }

    override fun openBlock(name: String, description: String) {
        println("##teamcity[blockOpened name='$name' description='$description']")
    }

    override fun closeBlock(name: String) {
        println("##teamcity[blockClosed name='$name']")
    }

    override fun publishArtifact(path: File) {
        println("##teamcity[publishArtifacts '${path.absolutePath}']")
    }

    override fun setBuildNumber(version: String) {
        println("##teamcity[buildNumber '$version']")
    }
}
