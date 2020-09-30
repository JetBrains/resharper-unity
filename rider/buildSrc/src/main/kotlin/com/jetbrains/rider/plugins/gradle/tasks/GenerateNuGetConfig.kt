package com.jetbrains.rider.plugins.gradle.tasks

import com.jetbrains.rider.plugins.gradle.buildServer.buildServer
import org.gradle.api.DefaultTask
import org.gradle.api.tasks.Input
import org.gradle.api.tasks.OutputFile
import org.gradle.api.tasks.TaskAction
import java.io.File

open class GenerateNuGetConfig: DefaultTask() {
    @Input
    var dotNetSdkPath: Any? = null

    @OutputFile
    var nuGetConfigFile = File("${project.projectDir}/../NuGet.Config")

    @TaskAction
    fun generate() {
        val dotNetSdkFile = dotNetSdkPath?.let { project.file(it)} ?: error("dotNetSdkLocation not set")
        logger.info("dotNetSdk location: '$dotNetSdkFile'")
        assert(dotNetSdkFile.isDirectory)

        project.buildServer.progress("Generating :${nuGetConfigFile.canonicalPath}...")
        val nugetConfigText = """<?xml version="1.0" encoding="utf-8"?>
            |<configuration>
            |  <packageSources>
            |    <clear />
            |    <add key="local-dotnet-sdk" value="${dotNetSdkFile.canonicalPath}" />
            |    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
            |  </packageSources>
            |</configuration>
            """.trimMargin()
        nuGetConfigFile.writeText(nugetConfigText)

        logger.info("Generated content:\n$nugetConfigText")

        val sb = StringBuilder("Dump dotNetSdkFile content:\n")
        for(file in dotNetSdkFile.listFiles()) {
            sb.append("${file.canonicalPath}\n")
        }
        logger.info(sb.toString())
    }
}