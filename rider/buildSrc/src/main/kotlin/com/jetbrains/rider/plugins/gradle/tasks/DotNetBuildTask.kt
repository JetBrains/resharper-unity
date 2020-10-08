package com.jetbrains.rider.plugins.gradle.tasks

import com.jetbrains.rider.plugins.gradle.buildServer.BuildServer
import com.jetbrains.rider.plugins.gradle.buildServer.buildServer
import org.apache.tools.ant.taskdefs.condition.Os
import org.gradle.api.DefaultTask
import org.gradle.api.file.RegularFileProperty
import org.gradle.api.logging.LogLevel
import org.gradle.api.tasks.InputFile
import org.gradle.api.tasks.TaskAction
import org.gradle.kotlin.dsl.extra
import java.io.File

open class DotNetBuildTask: DefaultTask() {
    @InputFile
    val buildFile: RegularFileProperty = project.objects.fileProperty()

    @TaskAction
    fun build() {
        val buildConfiguration = project.extra["BuildConfiguration"] as String
        val warningsAsErrors = project.extra["warningsAsErrors"] as String
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
            executable = dotNetCliPath
            args = buildArguments
            workingDir = file.parentFile
        }
    }

    private fun findDotNetCliPath(): String {
        if (project.extra.has("dotNetCliPath")) {
            val dotNetCliPath = project.extra["dotNetCliPath"] as String
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
                project.extra["dotNetCliPath"] = dotNetCliFile.canonicalPath
                return dotNetCliFile.canonicalPath
            }
        }
        error(".NET Core CLI not found. Please add: 'dotnet' in PATH")
    }

    private val verbosity: String
        get() {
            if ((project.extra["buildServer"] as BuildServer).isAutomatedBuild) {
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