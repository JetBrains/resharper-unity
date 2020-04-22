package com.jetbrains.rider.plugins.gradle.tasks

import com.jetbrains.rider.plugins.gradle.buildServer.buildServer
import org.gradle.api.DefaultTask
import org.gradle.api.tasks.Input
import org.gradle.api.tasks.OutputFile
import org.gradle.api.tasks.TaskAction
import java.io.File

open class GenerateDotNetSdkPathPropsTask: DefaultTask() {
    @Input
    var dotNetSdkPath: Any? = null

    @OutputFile
    var propsFile = File("${project.projectDir}/../resharper/build/generated/DotNetSdkPath.generated.props")

    @TaskAction
    fun generate() {
        project.buildServer.progress("Generating DotNetSdkPath.generated.props")
        project.file(propsFile).writeText("""<Project>
          <PropertyGroup>
            <DotNetSdkPath>${dotNetSdkPath?.let { project.file(it)} }</DotNetSdkPath>
          </PropertyGroup>
        </Project>""".trimIndent())
    }
}