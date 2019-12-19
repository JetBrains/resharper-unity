package com.jetbrains.rider.plugins.gradle

import org.apache.tools.ant.taskdefs.condition.Os
import org.gradle.api.Project
import org.gradle.api.logging.Logger
import org.jetbrains.intellij.IntelliJPluginExtension
import java.io.File

class BackendPaths(private val project: Project, logger: Logger, val repositoryRoot: File, private val productVersion: String) {
    private val unityRoot: File
    val backendRoot: File
    val resharperHostPluginSolution: File
    val unityPluginSolution: File
    val bundledReSharperSdkPath: File
    var downloadedReSharperSdkPath: File? = null
    private val bundledRiderSdkPath: File

    init {
        assert(repositoryRoot.isDirectory)

        backendRoot = File(repositoryRoot, "resharper")
        assert(backendRoot.isDirectory)

        unityRoot = File(repositoryRoot, "unity")
        unityPluginSolution = File(unityRoot, "JetBrains.Rider.Unity.Editor.sln")

        // Temporary workaround - the R# SDK contains yFile assembly that is obfuscated, and kills msbuild on mac
        val sln = if (!Os.isFamily(Os.FAMILY_WINDOWS)) "rider-unity.sln" else "resharper-unity.sln"
        resharperHostPluginSolution = File(backendRoot, sln)
        assert(resharperHostPluginSolution.isFile)

        bundledRiderSdkPath = File(repositoryRoot, "rider/dependencies")
        if (bundledRiderSdkPath.isDirectory) {
            val riderSdkPath = File(bundledRiderSdkPath.canonicalPath, "lib/ReSharperHostSdk")
            assert(riderSdkPath.isDirectory)
            logger.lifecycle("Rider SDK bundle found: ${riderSdkPath.canonicalPath}")
        }

        bundledReSharperSdkPath = File(repositoryRoot, "rider/build/JetBrains.ReSharperUltimate.Packages")
        if (bundledReSharperSdkPath.isDirectory) {
            logger.lifecycle("ReSharper SDK bundle found: ${bundledReSharperSdkPath.canonicalPath}")
        }
    }

    fun getRiderSdkRoot(): File {
        if (bundledRiderSdkPath.isDirectory) {
            return bundledRiderSdkPath
        }

        val intellij = project.extensions.findByType(IntelliJPluginExtension::class.java)!!

        var root = File(repositoryRoot, "rider/build/riderRD-$productVersion-SNAPSHOT")
        if (intellij.ideaDependencyCachePath != null) {
            root = File(intellij.ideaDependencyCachePath)
        }
        if (!root.isDirectory) {
            // If this assert fires, then you've likely called getRiderSdkPath during configuration
            // Try to wrap this call in a closure, so that it's evaluated at execution time, once the
            // intellij dependencies have been downloaded
            assert(intellij.ideaDependency != null)
            root = File(intellij.ideaDependency.classes.absolutePath)
        }

        return root
    }

    fun getRiderSdkPath(): File {
        val sdkRoot = File(getRiderSdkRoot(), "lib/ReSharperHostSdk")
        assert(sdkRoot.isDirectory)
        return sdkRoot
    }

    fun getReSharperSdkPath(): File {
        if (bundledReSharperSdkPath.isDirectory) {
            return bundledReSharperSdkPath
        }
        assert(downloadedReSharperSdkPath != null)
        return downloadedReSharperSdkPath!!
    }

    private fun getRdLibDirectory(): File {
        val rdlib = File(getRiderSdkRoot(),"lib/rd")
        assert(rdlib.isDirectory)
        return rdlib
    }

    fun getRiderModelJar(): File {
        val jarFile = File(getRdLibDirectory(), "rider-model.jar").canonicalFile
        assert(jarFile.isFile)
        return jarFile
    }
}
