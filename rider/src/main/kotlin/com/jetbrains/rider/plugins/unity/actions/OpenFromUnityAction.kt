package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.progress.ProgressIndicator
import com.intellij.openapi.progress.ProgressManager
import com.intellij.openapi.progress.Task
import com.intellij.openapi.vfs.VfsUtil
import com.jetbrains.rider.plugins.unity.openFrom.OpenFromUnityDialog
import com.jetbrains.rider.plugins.unity.openFrom.OpenUnityProject
import com.jetbrains.rider.plugins.unity.openFrom.UnityOpenProjectDiscoverer
import com.jetbrains.rider.plugins.unity.restClient.Island
import com.jetbrains.rider.projectView.SolutionManager
import com.jetbrains.rider.util.idea.getLogger
import org.apache.commons.io.FilenameUtils
import java.io.File
import java.nio.charset.Charset
import java.security.MessageDigest

// Bonus credit - use the REST API to install the plugin, so that it immediately gets loaded?
//   ProjectState will do AssetDatabase.Refresh, but I'm not sure if that's enough to cause a dll to get reloaded
//   MoveAsset might do this?
// There is also /unity/scripts with a request of "recompile"
// But the problem is that at least 2018.1 requires an Origin header for a POST, and I can't work out what value to use

class OpenFromUnityAction: AnAction() {
    companion object {
        private val logger = getLogger<OpenFromUnityAction>()
    }

    override fun actionPerformed(event: AnActionEvent) {
        val projectDiscoverer = UnityOpenProjectDiscoverer()
        val dialog = OpenFromUnityDialog(projectDiscoverer)
        if (dialog.showAndGet()) {
            val unityProject = dialog.selectedUnityProject ?: return
            if (!unityProject.solutionFile.exists()) {
                logger.debug("Generating Unity solution and project files: ${unityProject.solutionFile}")
                generateProjectFiles(unityProject)
            }

            val vfsFile = VfsUtil.findFileByIoFile(unityProject.solutionFile, true) ?: return
            logger.debug("Loading existing Unity solution: ${vfsFile.canonicalPath}")
            SolutionManager.openExistingSolution(null, true, vfsFile)
        }
    }

    private fun generateProjectFiles(unityProject: OpenUnityProject) {
        ProgressManager.getInstance().run(object: Task.Modal(null, "Generating project files", false) {
            override fun run(indicator: ProgressIndicator) {
                val segmentSize = 1.0 / (unityProject.projectState.islands.size + 1)
                indicator.isIndeterminate = false
                indicator.fraction = segmentSize

                indicator.text = "Generating ${unityProject.projectName}.sln"

                val projectGuids = generateSolutionFile(unityProject)

                for ((index, island) in unityProject.projectState.islands.withIndex()) {
                    indicator.fraction = segmentSize * (index + 1)
                    indicator.text = "Generating ${island.name}.csproj"
                    generateProjectFile(unityProject, projectGuids, island)
                }
            }
        })
    }

    private fun generateSolutionFile(unityProject: OpenUnityProject): Map<String, String> {
        return unityProject.solutionFile.writer(Charset.forName("UTF-8")).use {
            val projectGuids = mutableMapOf<String, String>()

            // I think this file needs to be in CRLF format. I should have tested that before I went to the trouble of
            // writing it like this
            it.append("\r\n" +
                      "Microsoft Visual Studio Solution File, Format Version 12.00\r\n" +
                      "# JetBrains Rider\r\n")

            for (island in unityProject.projectState.islands) {
                if (!shouldGenerate(island)) continue

                val projectGuid = createProjectGuid(unityProject.projectName, island.name)
                projectGuids[island.name] = projectGuid

                it.append("Project(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\") = \"${unityProject.projectName}\", \"${island.name}.csproj\", \"{$projectGuid}\"\r\n" +
                          "EndProject\r\n")
            }

            it.append("Global\r\n" +
                      "\tGlobalSection(SolutionConfigurationPlatforms) = preSolution\r\n" +
                      "\t\tDebug|Any CPU = Debug|Any CPU\r\n" +
                      "\t\tRelease|Any CPU = Release|Any CPU\r\n" +
                      "\tEndGlobalSection\r\n" +
                      "\tGlobalSection(ProjectConfigurationPlatforms) = postSolution\r\n")
            for (island in unityProject.projectState.islands) {
                val guid = projectGuids[island.name] ?: continue
                it.append("\t\t{$guid}.Debug|Any CPU.ActiveCfg = Debug|Any CPU\r\n" +
                          "\t\t{$guid}.Debug|Any CPU.Build.0 = Debug|Any CPU\r\n" +
                          "\t\t{$guid}.Release|Any CPU.ActiveCfg = Release|Any CPU\r\n" +
                          "\t\t{$guid}.Release|Any CPU.Build.0 = Release|Any CPU\r\n")
            }

            it.append("\tEndGlobalSection\r\n" +
                      "\tGlobalSection(SolutionProperties) = preSolution\r\n" +
                      "\t\tHideSolutionNode = FALSE\r\n" +
                      "\tEndGlobalSection\r\n" +
                      "EndGlobal")

            projectGuids
        }
    }

    private fun generateProjectFile(unityProject: OpenUnityProject, projectGuids: Map<String, String>, island: Island) {
        val guid = projectGuids[island.name] ?: return

        File(unityProject.projectState.basedirectory, island.name + ".csproj").writer(Charset.forName("UTF-8")).use {
            val defines = island.defines.toHashSet()

            it.appendln("<?xml version=\"1.0\" encoding=\"utf-8\"?>")
            it.appendln("<Project ToolsVersion=\"4.0\" DefaultTargets=\"Build\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">")

            // We don't know C# language level or target framework. Make best guesses - these files will be overwritten
            // by the plugin the first time it loads, or the first time we try and view a file from Unity
            var fx = "4.5"
            var langVersion: String? = null
            if (defines.contains("CSHARP_7_OR_LATER")) {
                fx = "4.7.1"
                langVersion = "7"
            }
            else if (defines.contains("NET_4_6")) {
                fx = "4.6"
                langVersion = "6"
            }

            if (langVersion != null) {
                it.appendln("  <PropertyGroup>")
                it.appendln("    <LangVersion>$langVersion</LangVersion>")
                it.appendln("  </PropertyGroup>")
            }

            val constants = defines.joinToString(";")

            // Note that we get the RootNamespace wrong here
            it.appendln("""
  <PropertyGroup>
    <Configuration Condition=" '$(Configuration)' == '' ">Debug</Configuration>
    <Platform Condition=" '$(Platform)' == '' ">AnyCPU</Platform>
    <ProductVersion>10.0.20506</ProductVersion>
    <SchemaVersion>2.0</SchemaVersion>
    <RootNamespace></RootNamespace>
    <ProjectGuid>{$guid}</ProjectGuid>
    <OutputType>Library</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <AssemblyName>${island.name}</AssemblyName>
    <TargetFrameworkVersion>$fx</TargetFrameworkVersion>
  </PropertyGroup>
  <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>Temp\bin\Debug\</OutputPath>
    <DefineConstants>$constants</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
    <NoWarn>0169</NoWarn>
    <AllowUnsafeBlocks>True</AllowUnsafeBlocks>
  </PropertyGroup>
  <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' ">
    <DebugType>pdbonly</DebugType>
    <Optimize>true</Optimize>
    <OutputPath>Temp\bin\Release\</OutputPath>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
    <NoWarn>0169</NoWarn>
    <AllowUnsafeBlocks>True</AllowUnsafeBlocks>
  </PropertyGroup>
  <PropertyGroup>
    <NoConfig>true</NoConfig>
    <NoStdLib>true</NoStdLib>
    <AddAdditionalExplicitAssemblyReferences>false</AddAdditionalExplicitAssemblyReferences>
    <ImplicitlyExpandNETStandardFacades>false</ImplicitlyExpandNETStandardFacades>
    <ImplicitlyExpandDesignTimeFacades>false</ImplicitlyExpandDesignTimeFacades>
  </PropertyGroup>
  <PropertyGroup>
    <ProjectTypeGuids>{E097FAD1-6243-4DAD-9C02-E9B9EFC3FFC1};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}</ProjectTypeGuids>
  </PropertyGroup>""")

            it.appendln("  <ItemGroup>")
            for (file in island.files) {
                val action = if (file.endsWith(".cs")) "Compile" else "None"
                it.appendln("""
    <$action Include="$file" />
                """)
            }
            it.appendln("  </ItemGroup>")

            it.appendln("  <ItemGroup>")
            for (reference in island.references) {
                val assembly = FilenameUtils.getBaseName(reference)
                it.appendln("""
    <Reference Include="$assembly">
      <HintPath>$reference</HintPath>
    </Reference>""")
            }
            it.appendln("  </ItemGroup>")

            it.appendln("""
  <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.Targets" />
  <Target Name="GenerateTargetFrameworkMonikerAttribute" />
</Project>""")
        }
    }

    private fun shouldGenerate(island: Island): Boolean {
        if (island.language == "C#") {
            for (fileName in island.files) {
                // Make sure all of the files exist. We might get projects for non-embedded packages
                val file = File(island.basedirectory, fileName)
                if (!file.exists())
                    return false
            }
            return true
        }

        return false
    }

    // If this GUID is deterministic, Rider will update changed projects in-place, rather than unloading and reloading
    // Unity creates a GUID string from an MD5 hash of the {projectName}{assemblyName}"salt"
    // See https://github.com/Unity-Technologies/UnityCsReference/blob/48d4a3b11405d6301bf839351553bd2b08338fd9/Editor/Mono/VisualStudioIntegration/SolutionSynchronizer.cs#L713-L749
    private fun createProjectGuid(unityProjectName:String, assemblyName: String): String {
        val hashInput = unityProjectName + assemblyName + "salt"
        val digest = MessageDigest.getInstance("MD5")
        // NOTE: Unity uses Encoding.Default, which is ANSI, but machine dependent...
        val md5hash = digest.digest(hashInput.toByteArray(Charset.forName("UTF-8")))
        val stringBuilder = StringBuilder()
        for (byte in md5hash) {
            stringBuilder.append(String.format("%02X", byte))
        }
        val hash = stringBuilder.toString()
        return hash.substring(0, 8) + "-" + hash.substring(8, 12) + "-" + hash.substring(12, 16) + "-" + hash.substring(16, 20) + "-" + hash.substring(20, 32)
    }
}

