package com.jetbrains.rider.plugins.gradle

import com.jetbrains.rider.plugins.gradle.buildServer.buildServer
import org.gradle.api.Project
import org.gradle.api.logging.Logger
import org.jetbrains.intellij.IntelliJPluginExtension
import java.io.File

class BackendPaths(private val project: Project,
                   private val logger: Logger,
                   private val repositoryRoot: File,
                   private val productVersion: String) {
    private var riderSdkPath: File? = null

    val backendRoot: File
    val unityPluginSolution: File
    val resharperHostPluginSolution: File

    init {
        assert(repositoryRoot.isDirectory)

        val unityRoot = File(repositoryRoot, "unity")
        unityPluginSolution = File(unityRoot, "JetBrains.Rider.Unity.Editor.sln")

        backendRoot = File(repositoryRoot, "resharper")
        resharperHostPluginSolution = File(backendRoot, "resharper-unity.sln")
        assert(resharperHostPluginSolution.isFile)
    }

    private fun getRiderSdkRootPath(): File {
        if (riderSdkPath == null) {
            if (project.buildServer.isAutomatedBuild) {
                riderSdkPath = File(repositoryRoot, "rider/dependencies")
                if (riderSdkPath?.isDirectory == true) {
                    logger.info("Rider SDK bundle found: ${riderSdkPath?.canonicalPath}")
                } else {
                    logger.error("Bundle Rider SDK not found in '$riderSdkPath'. Falling back to public SDK")
                }
            }

            if (riderSdkPath == null || riderSdkPath?.isDirectory == false)
            {
                val intellij = project.extensions.findByType(IntelliJPluginExtension::class.java)!!

                var root = File(repositoryRoot, "rider/build/riderRD-$productVersion-SNAPSHOT")
                val ideaDependencyCachePath = intellij.ideaDependencyCachePath.orNull
                if (ideaDependencyCachePath != null) {
                    root = File(ideaDependencyCachePath)
                }
                if (!root.isDirectory) {
                    // If this assert fires, then you've likely called getRiderSdkPath during configuration
                    // Try to wrap this call in a closure, so that it's evaluated at execution time, once the
                    // intellij dependencies have been downloaded
                    val ideaDependency = intellij.ideaDependency.orNull
                    assert(ideaDependency != null)
                    root = File(ideaDependency?.classes?.absolutePath ?: error("ideaDependency is null"))
                }
                riderSdkPath = root
                logger.info("Rider SDK bundle found: ${root.canonicalPath}")
            }
        }
        assert(riderSdkPath?.isDirectory ?: false)
        return riderSdkPath!!
    }

    fun getDotNetSdkPath(): File {
        val sdkRoot = File(getRiderSdkRootPath(), "lib/DotNetSdkForRdPlugins")
        assert(sdkRoot.isDirectory)
        return sdkRoot
    }

    fun getRdLibDirectory(): File {
        val rdLib = File(getRiderSdkRootPath(),"lib/rd")
        assert(rdLib.isDirectory)
        return rdLib
    }

    fun getRiderModelJar(): File {
        val jarFile = File(getRdLibDirectory(), "rider-model.jar").canonicalFile
        assert(jarFile.isFile)
        return jarFile
    }
}
