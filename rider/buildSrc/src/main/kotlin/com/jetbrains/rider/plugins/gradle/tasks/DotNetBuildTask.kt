package com.jetbrains.rider.plugins.gradle.tasks

import com.jetbrains.rider.plugins.gradle.buildServer.BuildServer
import com.jetbrains.rider.plugins.gradle.buildServer.buildServer
import org.apache.tools.ant.taskdefs.condition.Os
import org.gradle.api.DefaultTask
import org.gradle.api.file.RegularFileProperty
import org.gradle.api.logging.LogLevel
import org.gradle.api.tasks.InputFile
import org.gradle.api.tasks.TaskAction
import org.jetbrains.kotlin.gradle.plugin.extraProperties
import java.io.File

open class DotNetBuildTask: DefaultTask() {
    @InputFile
    val buildFile: RegularFileProperty = project.objects.fileProperty()

    @TaskAction
    fun build() {
        val buildConfiguration = project.extraProperties["BuildConfiguration"] as String
        val warningsAsErrors = project.extraProperties["warningsAsErrors"] as String
        val file = buildFile.asFile.get()

        project.buildServer.progress("Building $file ($buildConfiguration)")

        val dotNetCliPath = findDotNetCliPath()
        val slnDir = file.parentFile
        val buildArguments = listOf(
                "build",
                file.canonicalPath,
                "/p:Configuration=$buildConfiguration",
                "/p:Version=${project.version}",
                "/p:TreatWarningsAsErrors=$warningsAsErrors",
                "/v:$verbosity",
                "/bl:${file.name+".binlog"}",
                "/nologo")

        logger.info("dotnet call: '$dotNetCliPath' '$buildArguments' in '$slnDir'")
        project.exec {
            it.executable = dotNetCliPath
            it.args = buildArguments
            it.workingDir = file.parentFile
        }
    }

    private fun findDotNetCliPath(): String {
        if (project.extraProperties.has("dotNetCliPath")) {
            val dotNetCliPath = project.extraProperties["dotNetCliPath"] as String
            logger.info("dotNetCliPath (cached): $dotNetCliPath")
            return dotNetCliPath
        }

        val pathComponents = System.getenv("PATH").split(File.pathSeparatorChar)
        for (dir in pathComponents) {
            val dotNetCliFile = File(dir, if (Os.isFamily(Os.FAMILY_WINDOWS)) {
                "dotnet.exe"
            } else {
                "dotnet"
            })
            if (dotNetCliFile.exists()) {
                logger.info("dotNetCliPath: ${dotNetCliFile.canonicalPath}")
                project.extraProperties["dotNetCliPath"] = dotNetCliFile.canonicalPath
                return dotNetCliFile.canonicalPath
            }
        }
        error(".NET Core CLI not found. Please add: 'dotnet' in PATH")
    }

    private val verbosity: String
        get() {
            if ((project.extraProperties["buildServer"] as BuildServer).isAutomatedBuild) {
                return "normal"
            }

            return when (project.gradle.startParameter.logLevel) {
                LogLevel.QUIET -> "quiet"
                LogLevel.LIFECYCLE -> "minimal"
                LogLevel.INFO -> "normal"
                LogLevel.DEBUG -> "detailed"
                else -> "normal"
            }
        }
}