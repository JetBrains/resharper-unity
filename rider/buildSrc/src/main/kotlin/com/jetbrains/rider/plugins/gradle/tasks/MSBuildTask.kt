package com.jetbrains.rider.plugins.gradle.tasks

import com.jetbrains.rider.plugins.gradle.buildServer.BuildServer
import com.jetbrains.rider.plugins.gradle.buildServer.buildServer
import org.apache.tools.ant.taskdefs.condition.Os
import org.gradle.api.DefaultTask
import org.gradle.api.file.RegularFileProperty
import org.gradle.api.tasks.InputFile
import org.gradle.api.tasks.TaskAction
import org.gradle.kotlin.dsl.extra
import org.apache.tools.ant.taskdefs.condition.Os.FAMILY_WINDOWS
import org.gradle.api.GradleException
import org.gradle.api.logging.LogLevel
import java.io.ByteArrayOutputStream
import java.io.File
import java.lang.IllegalStateException

open class MSBuildTask: DefaultTask() {

    @InputFile
    val buildFile: RegularFileProperty = project.objects.fileProperty()

    @TaskAction
    fun build() {
        val buildConfiguration = project.extra["BuildConfiguration"] as String
        val warningsAsErrors = project.extra["warningsAsErrors"] as String
        val file = buildFile.asFile.get()
        project.buildServer.progress("Building $file ($buildConfiguration)")

        val arguments = listOf(file.absolutePath,
                "/p:Configuration=$buildConfiguration",
                "/p:Version=${project.version}",
                "/p:TreatWarningsAsErrors=$warningsAsErrors",
                "/v:$verbosity",
                "/nologo")

        val msbuildPath = findMSBuildPath()
        logger.info("msbuild call=$msbuildPath " + arguments.toString())

        project.exec {
            executable = msbuildPath
            args = arguments
        }
    }

    private fun findMSBuildPath(): String {
        if (project.extra.has("msbuildPath")) {
            val msbuildPath = project.extra["msbuildPath"] as String
            logger.info("msbuildPath (cached): $msbuildPath")
            return msbuildPath
        }

        val msbuildPath = if (Os.isFamily(FAMILY_WINDOWS))
            findMSBuildPathWindows()
        else
            findMSBuildPathUnix()
        logger.info("msbuildPath: $msbuildPath")
        project.extra["msbuildPath"] = msbuildPath
        return msbuildPath
    }

    private fun findMSBuildPathWindows(): String {
        val stdout = ByteArrayOutputStream()
        project.exec {
            executable = File(project.projectDir, "../tools/vswhere.exe").absolutePath
            args = listOf("-all", "-version", "[15.0,)", "-products", "*", "-requires", "Microsoft.Component.MSBuild", "-requires", "Microsoft.NetCore.Component.SDK", "-property", "installationPath")
            standardOutput = stdout
        }
        val buildToolsDirs = stdout.toString().trim().split("\r\n", "\n")
        for (buildToolsDirPath in buildToolsDirs) {
            val buildToolsDir = File(buildToolsDirPath)
            if (!buildToolsDir.isDirectory) {
                logger.warn("$buildToolsDir is not a directory")
                continue
            }

            val msBuild15File = File(buildToolsDir, "MSBuild/15.0/Bin/MSBuild.exe")
            if (msBuild15File.exists())
                return msBuild15File.absolutePath
            val msBuildCurrentFile = File(buildToolsDir, "MSBuild/Current/Bin/MSBuild.exe")
            if (msBuildCurrentFile.exists())
                return msBuildCurrentFile.absolutePath
        }
        throw IllegalStateException("Failed to find MSBuild.exe inside [${buildToolsDirs.joinToString(", ")}]")
    }

    private fun findMSBuildPathUnix(): String {
        val stdout = ByteArrayOutputStream()
        val result = project.exec {
            executable = "which"
            args = listOf("msbuild")
            isIgnoreExitValue = true
            standardOutput = stdout
        }

        if (result.exitValue != 0) {
            throw GradleException("Unable to find msbuild on path. Please ensure Mono is installed")
        }

        return project.file(stdout.toString().trim()).absolutePath
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