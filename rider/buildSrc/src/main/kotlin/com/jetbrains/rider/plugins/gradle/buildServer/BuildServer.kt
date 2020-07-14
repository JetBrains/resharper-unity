package com.jetbrains.rider.plugins.gradle.buildServer

import org.gradle.api.Project
import org.gradle.api.Task
import org.gradle.api.execution.TaskExecutionListener
import org.gradle.api.invocation.Gradle
import org.gradle.api.tasks.TaskState
import org.gradle.kotlin.dsl.extra
import java.io.File

fun initBuildServer(gradle: Gradle): BuildServer {
    val server = when {
        System.getenv("TEAMCITY_VERSION") != null -> TeamCity()
        else -> NullBuildServer()
    }
    gradle.taskGraph.addTaskExecutionListener(BuildServerEventLogger(server))
    return server
}

interface BuildServer {
    val isAutomatedBuild: Boolean

    fun progress(message: String)
    fun openBlock(name: String, description: String)
    fun closeBlock(name: String)
    fun publishArtifact(path: File)
    fun setBuildNumber(version: String)
}

class NullBuildServer: BuildServer {

    override val isAutomatedBuild
        get() = false

    override fun progress(message: String) {
        println(message)
    }

    override fun openBlock(name: String, description: String) { }
    override fun closeBlock(name: String) { }

    override fun publishArtifact(path: File) {
        println("Publish: $path.absolutePath")
    }

    override fun setBuildNumber(version: String) {
        println("Build: $version")
    }
}

class BuildServerEventLogger(private val server: BuildServer): TaskExecutionListener {

    override fun beforeExecute(task: Task) {
        server.openBlock("gradle-${task.name}", "${task.name}")
    }

    override fun afterExecute(task: Task, state: TaskState) {
        server.closeBlock("gradle-${task.name}")
    }
}

val Project.buildServer : BuildServer
    get() { return this.extra["buildServer"] as BuildServer }