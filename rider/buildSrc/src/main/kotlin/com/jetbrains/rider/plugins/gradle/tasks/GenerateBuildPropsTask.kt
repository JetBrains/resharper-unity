package com.jetbrains.rider.plugins.gradle.tasks

import com.jetbrains.rider.plugins.gradle.buildServer.buildServer
import org.gradle.api.DefaultTask
import org.gradle.api.GradleException
import org.gradle.api.tasks.Input
import org.gradle.api.tasks.OutputFile
import org.gradle.api.tasks.TaskAction
import java.io.File
import java.util.regex.Pattern

open class GenerateBuildPropsTask: DefaultTask() {

    @Input
    var packagesDirectory: Any? = null

    @Input
    var packageName: String = ""

    @Input
    var msBuildParameter: String = ""

    @Input
    var packageVersion = project.providers.provider({ parsePackageVersion() })

    @OutputFile
    var propsFile = project.providers.provider({ File("${project.projectDir}/../resharper/$packageName.generated.props") })

    @TaskAction
    fun generate() {
        project.buildServer.progress("Generating build.props for $packageName")

        val version = packageVersion.get()
        logger.info("$msBuildParameter=$version")

        project.file(propsFile).writeText("""<Project>
  <PropertyGroup>
    <$msBuildParameter>[$version]</$msBuildParameter>
  </PropertyGroup>
</Project>
""")
    }

    private var parsedPackageVersion: String? = null

    private fun parsePackageVersion(): String? {
        if (parsedPackageVersion != null) return parsedPackageVersion

        if (packagesDirectory == null) {
            throw GradleException("packagesDirectory must be specified")
        }

        val packagesDir = project.file(packagesDirectory!!)
        assert(packagesDir.isDirectory)
        val escapedPackageName = Pattern.quote(packageName)

        logger.info("Looking for package $packageName in $packagesDir")

        val regex = """^$escapedPackageName\.((\d+\.)+\d+((-eap|-snapshot)\d+(d?)(pre|internal)?)?)\.nupkg$""".toRegex(RegexOption.IGNORE_CASE)
        parsedPackageVersion = packagesDir.listFiles().mapNotNull {
            logger.trace(it.name)

            val match = regex.matchEntire(it.name)
            if (match != null) match.groupValues[1] else null
        }.firstOrNull()

        if (parsedPackageVersion == null) {
            throw GradleException("No files found matching package version")
        }

        return parsedPackageVersion
    }
}