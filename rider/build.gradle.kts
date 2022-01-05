import com.jetbrains.rd.generator.gradle.RdGenExtension
import com.jetbrains.rd.generator.gradle.RdGenTask
import com.jetbrains.rider.plugins.gradle.BackendPaths
import com.jetbrains.rider.plugins.gradle.buildServer.initBuildServer
import com.jetbrains.rider.plugins.gradle.tasks.DotNetBuildTask
import com.jetbrains.rider.plugins.gradle.tasks.GenerateDotNetSdkPathPropsTask
import com.jetbrains.rider.plugins.gradle.tasks.GenerateNuGetConfig
import com.ullink.gradle.nunit.NUnit
import groovy.xml.XmlParser
import org.gradle.api.tasks.testing.logging.TestExceptionFormat
import org.jetbrains.changelog.ChangelogPluginExtension
import org.jetbrains.intellij.tasks.IntelliJInstrumentCodeTask
import org.jetbrains.intellij.tasks.PatchPluginXmlTask
import org.jetbrains.intellij.tasks.PrepareSandboxTask
import org.jetbrains.kotlin.gradle.tasks.KotlinCompile

plugins {
    id("idea")
    id("com.jetbrains.rdgen") version "2021.3.4"
    id("com.ullink.nuget") version "2.23"
    id("com.ullink.nunit") version "2.4"
    id("me.filippov.gradle.jvm.wrapper") version "0.10.0"
    id("org.jetbrains.changelog") version "1.2.1"
    id("org.jetbrains.intellij") // version in rider/buildSrc/build.gradle.kts
    id("org.jetbrains.grammarkit") version "2021.1.3"
    kotlin("jvm") version "1.4.20"
}

repositories {
    maven { setUrl("https://cache-redirector.jetbrains.com/maven-central") }
}

apply {
    plugin("kotlin")
}

val repoRoot = projectDir.parentFile!!
val isWindows = System.getProperty("os.name").toLowerCase().startsWith("win")
val bundledRiderSdkRoot = File(projectDir, "build/rider")  // SDK from TC configuration/artifacts
val bundledMavenArtifacts = File(projectDir, "build/maven-artifacts")
val productVersion = extra["productVersion"].toString()
val maintenanceVersion = extra["maintenanceVersion"].toString()
val pluginVersion = "$productVersion.$maintenanceVersion"
val buildCounter = extra["BuildCounter"].toString()
val buildServer = initBuildServer(gradle)
val releaseConfiguration = "Release"
val buildConfiguration = if (buildServer.isAutomatedBuild) {
    releaseConfiguration
} else {
    extra["BuildConfiguration"]
}
val isReleaseBuild = buildConfiguration == releaseConfiguration
val warningsAsErrors = extra["warningsAsErrors"].toString().toLowerCase().toBoolean()
val modelSrcDir = File(repoRoot, "rider/protocol/src/main/kotlin/model")
val hashBaseDir = File(repoRoot, "rider/build/rdgen")
val skipDotnet = extra["skipDotnet"].toString().toLowerCase().toBoolean()
val runTests = extra["RunTests"].toString().toLowerCase().toBoolean()
val backend = BackendPaths(project, logger, repoRoot, productVersion).apply {
    extra["backend"] = this
}
val dotnetDllFiles = files(
    "../resharper/build/rider-unity/bin/$buildConfiguration/net472/JetBrains.ReSharper.Plugins.Unity.dll",
    "../resharper/build/rider-unity/bin/$buildConfiguration/net472/JetBrains.ReSharper.Plugins.Unity.pdb",
    "../resharper/build/rider-unity/bin/$buildConfiguration/net472/JetBrains.ReSharper.Plugins.Json.dll",
    "../resharper/build/rider-unity/bin/$buildConfiguration/net472/JetBrains.ReSharper.Plugins.Json.pdb",
    "../resharper/build/rider-unity/bin/$buildConfiguration/net472/JetBrains.ReSharper.Plugins.Json.Rider.dll",
    "../resharper/build/rider-unity/bin/$buildConfiguration/net472/JetBrains.ReSharper.Plugins.Json.Rider.pdb",
    "../resharper/build/rider-unity/bin/$buildConfiguration/net472/JetBrains.ReSharper.Plugins.Yaml.dll",
    "../resharper/build/rider-unity/bin/$buildConfiguration/net472/JetBrains.ReSharper.Plugins.Yaml.pdb"
)

val debuggerDllFiles = files(
    "../resharper/build/debugger/bin/$buildConfiguration/net472/JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.dll",
    "../resharper/build/debugger/bin/$buildConfiguration/net472/JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.pdb"
)

val helperExeFiles = files(
    "../resharper/build/ios-list-usb-devices/bin/$buildConfiguration/net5.0/JetBrains.Rider.Unity.ListIosUsbDevices.dll",
    "../resharper/build/ios-list-usb-devices/bin/$buildConfiguration/net5.0/JetBrains.Rider.Unity.ListIosUsbDevices.pdb",
    "../resharper/build/ios-list-usb-devices/bin/$buildConfiguration/net5.0/JetBrains.Rider.Unity.ListIosUsbDevices.runtimeconfig.json"
)

val helperExeNetFxFiles = files(
    "../resharper/build/ios-list-usb-devices/bin/$buildConfiguration/net472/JetBrains.Rider.Unity.ListIosUsbDevices.exe",
    "../resharper/build/ios-list-usb-devices/bin/$buildConfiguration/net472/JetBrains.Rider.Unity.ListIosUsbDevices.pdb"
)

val unityEditorDllFiles = files(
    "../unity/build/EditorPlugin/bin/$buildConfiguration/net35/JetBrains.Rider.Unity.Editor.Plugin.Repacked.dll",
    "../unity/build/EditorPlugin/bin/$buildConfiguration/net35/JetBrains.Rider.Unity.Editor.Plugin.Repacked.pdb",
    "../unity/build/EditorPluginUnity56/bin/$buildConfiguration/net35/JetBrains.Rider.Unity.Editor.Plugin.Unity56.Repacked.dll",
    "../unity/build/EditorPluginUnity56/bin/$buildConfiguration/net35/JetBrains.Rider.Unity.Editor.Plugin.Unity56.Repacked.pdb",
    "../unity/build/EditorPluginFull/bin/$buildConfiguration/net35/JetBrains.Rider.Unity.Editor.Plugin.Full.Repacked.dll",
    "../unity/build/EditorPluginFull/bin/$buildConfiguration/net35/JetBrains.Rider.Unity.Editor.Plugin.Full.Repacked.pdb",
    "../unity/build/EditorPluginNet46/bin/$buildConfiguration/net472/JetBrains.Rider.Unity.Editor.Plugin.Net46.Repacked.dll",
    "../unity/build/EditorPluginNet46/bin/$buildConfiguration/net472/JetBrains.Rider.Unity.Editor.Plugin.Net46.Repacked.pdb"
)

version = "${pluginVersion}.$buildCounter"

java {
    sourceCompatibility = JavaVersion.VERSION_11
    targetCompatibility = JavaVersion.VERSION_11
}

sourceSets {
    main {
        java {
            srcDir("src/main/gen/kotlin")
        }
        resources {
            srcDir("src/main/gen/resources")
        }
    }
}

idea {
    module {
        generatedSourceDirs.add(file("src/main/gen/kotlin"))
        resourceDirs.add(file("src/main/gen/resources"))
    }
}

intellij {
    pluginName.set("rider-unity")
    type.set("RD")
    // Download a version of Rider to compile and run with. Either set `version` to
    // 'LATEST-TRUNK-SNAPSHOT' or 'LATEST-EAP-SNAPSHOT' or a known version.
    // This will download from www.jetbrains.com/intellij-repository/snapshots or
    // www.jetbrains.com/intellij-repository/releases, respectively.
    // http://jetbrains-com-mirror.labs.intellij.net/intellij-repository/snapshots/
    // Note that there's no guarantee that these are kept up-to-date
    // version = 'LATEST-TRUNK-SNAPSHOT'
    // If the build isn't available in intellij-repository, use an installed version via `localPath`
    // localPath.set('/Users/matt/Library/Application Support/JetBrains/Toolbox/apps/Rider/ch-1/171.4089.265/Rider EAP.app/Contents')
    // localPath.set("D:\\RiderSDK")

    if (bundledRiderSdkRoot.exists()) {
        localPath.set(bundledRiderSdkRoot.canonicalPath)
    } else {
        version.set("${productVersion}-SNAPSHOT")
    }
    intellijRepository.set("https://cache-redirector.jetbrains.com/intellij-repository")

    // Sources aren't available for Rider
    downloadSources.set(false)

    plugins.set(listOf("rider-plugins-appender", "CSS", "yaml", "dotCover"))
}

configure<ChangelogPluginExtension> {
    path.set("../CHANGELOG.md")
    headerParserRegex.set("\\d+\\.\\d+(\\.\\d+)?.*".toRegex())
}

logger.lifecycle("Version=$version")
logger.lifecycle("BuildConfiguration=$buildConfiguration")

tasks {
    val backendGroup = "backend"
    val ciGroup = "ci"
    val protocolGroup = "protocol"
    val testGroup = "verification"

    buildSearchableOptions {
        enabled = isReleaseBuild
    }

    withType<IntelliJInstrumentCodeTask> {
        // For SDK from local folder you also need to manually download maven-artefacts folder
        // from SDK build artefacts on TC and put it into build folder.
        if (bundledMavenArtifacts.exists()) {
            logger.lifecycle("Use ant compiler artifacts from local folder: $bundledMavenArtifacts")
            compilerClassPathFromMaven.set(
                bundledMavenArtifacts.walkTopDown()
                    .filter { it.extension == "jar" && !it.name.endsWith("-sources.jar") }
                    .toList()
                    + File("${ideaDependency.get().classes}/lib/3rd-party-rt.jar")
                    + File("${ideaDependency.get().classes}/lib/util.jar")
            )
        } else {
            logger.lifecycle("Use ant compiler artifacts from maven")
        }
    }

    runIde {
        // Match Rider's default heap size of 1.5Gb (default for runIde is 512Mb)
        maxHeapSize = "1500m"
    }

    named<Wrapper>("wrapper") {
        gradleVersion = "7.2"
        distributionType = Wrapper.DistributionType.BIN
    }

    val patchPluginXml by named<PatchPluginXmlTask>("patchPluginXml") {
        changeNotes.set(
            """
        <body>
        <p><b>New in $pluginVersion</b></p>
        <p>
        ${changelog.get(pluginVersion).toHTML()}
        </p>
        <p>See the <a href="https://github.com/JetBrains/resharper-unity/blob/net221/CHANGELOG.md">CHANGELOG</a> for more details and history.</p>
        </body>""".trimIndent()
        )
    }

    val validatePluginXml by registering {
        group = ciGroup
        dependsOn(patchPluginXml)
        val pluginXml = File(repoRoot, "rider/src/main/resources/META-INF/plugin.xml")
        if (!pluginXml.isFile) throw GradleException("plugin.xml must be a valid file")

        inputs.file(pluginXml)
        outputs.file(pluginXml)

        doLast {
            val parsed = XmlParser().parse(pluginXml).text()
            if (parsed.isEmpty()) throw GradleException("plugin.xml cannot be empty")

            val rawBytes = pluginXml.readBytes()
            if (rawBytes.isEmpty()) throw GradleException("plugin.xml cannot be empty")
            if (rawBytes.any { it < 0 }) throw GradleException("plugin.xml cannot contain invalid bytes")

            logger.lifecycle("$pluginXml.path is valid XML and contains only US-ASCII symbols, bytes: $rawBytes.length")
        }
    }

    processResources {
        dependsOn(validatePluginXml)
        copy {
            from("../common/dictionaries/unity.dic")
            into("src/main/gen/resources/com/jetbrains/rider/plugins/unity/spellchecker/")
        }
    }

    create("setDotNetVersionTcParam") {
        group = ciGroup
        doLast {
            println("##teamcity[setParameter name='DotNetVersion' value='$version']")
        }
    }

    val publishCiBuildNumber by registering {
        group = ciGroup
        doLast {
            buildServer.setBuildNumber(version.toString())
        }
    }

    val generateLibModel by registering(RdGenTask::class) {
        group = protocolGroup
        (extensions.getByName("params") as RdGenExtension).apply {
            // Always store models in their own folder, so the hash is only looking at the files we generate
            val backendCsOutDir = File(repoRoot, "resharper/build/generated/Model/Lib")
            val unityEditorCsOutDir = File(repoRoot, "unity/build/generated/Model/Lib")
            val frontendKtOutDir =
                File(repoRoot, "rider/src/main/gen/kotlin/com/jetbrains/rider/plugins/unity/model/lib")
            verbose = project.gradle.startParameter.logLevel == LogLevel.INFO
                || project.gradle.startParameter.logLevel == LogLevel.DEBUG
            classpath({ backend.getRiderModelJar() })
            sources("$modelSrcDir/lib")
            hashFolder = "$hashBaseDir/lib"
            packages = "model.lib"

            // Library is used as backend in backendUnityModel and backend in frontendBackendModel, so needs to be both
            // asis and reversed. I.e. symmetric
            generator {
                language = "csharp"
                transform = "symmetric"
                root = "model.lib.Library"
                directory = backendCsOutDir.canonicalPath
            }
            // Library is used as unity in backendUnityModel, so has reversed perspective
            generator {
                language = "csharp"
                transform = "reversed"
                root = "model.lib.Library"
                directory = unityEditorCsOutDir.canonicalPath
            }
            // Library is used as frontend in frontendBackendModel, so has same perspective. Generate as-is
            generator {
                language = "kotlin"
                transform = "asis"
                root = "model.lib.Library"
                directory = frontendKtOutDir.canonicalPath
            }
        }
    }

    val generateFrontendBackendModel by registering(RdGenTask::class) {
        group = protocolGroup
        dependsOn(generateLibModel)
        (extensions.getByName("params") as RdGenExtension).apply {
            // Always store models in their own folder, so the hash is only looking at the files we generate
            val backendCsOutDir = File(repoRoot, "resharper/build/generated/Model/FrontendBackend")
            val frontendKtOutDir =
                File(repoRoot, "rider/src/main/gen/kotlin/com/jetbrains/rider/plugins/unity/model/frontendBackend")
            verbose = project.gradle.startParameter.logLevel == LogLevel.INFO
                || project.gradle.startParameter.logLevel == LogLevel.DEBUG
            classpath({ backend.getRiderModelJar() })
            sources(modelSrcDir)
            hashFolder = "$hashBaseDir/frontendBackend"
            packages = "model.frontendBackend"

            generator {
                language = "kotlin"
                transform = "asis"
                root = "com.jetbrains.rider.model.nova.ide.IdeRoot"
                directory = frontendKtOutDir.absolutePath
            }

            generator {
                language = "csharp"
                transform = "reversed"
                root = "com.jetbrains.rider.model.nova.ide.IdeRoot"
                directory = backendCsOutDir.absolutePath
            }
        }
    }

    val generateBackendUnityModel by registering(RdGenTask::class) {
        group = protocolGroup
        dependsOn(generateLibModel)
        (extensions.getByName("params") as RdGenExtension).apply {
            // Always store models in their own folder, so the hash is only looking at the files we generate
            val backendCsOutDir = File(repoRoot, "resharper/build/generated/Model/BackendUnity")
            val unityEditorCsOutDir = File(repoRoot, "unity/build/generated/Model/BackendUnity")
            verbose = project.gradle.startParameter.logLevel == LogLevel.INFO
                || project.gradle.startParameter.logLevel == LogLevel.DEBUG
            classpath({ backend.getRiderModelJar() })
            sources(modelSrcDir)
            hashFolder = "$hashBaseDir/backendUnity"
            packages = "model.backendUnity"

            generator {
                language = "csharp"
                transform = "asis"
                root = "model.backendUnity.BackendUnityModel"
                directory = backendCsOutDir.canonicalPath
            }

            generator {
                language = "csharp"
                transform = "reversed"
                root = "model.backendUnity.BackendUnityModel"
                directory = unityEditorCsOutDir.canonicalPath
            }
        }
    }

    val generateDebuggerWorkerModel by registering(RdGenTask::class) {
        group = protocolGroup
        (extensions.getByName("params") as RdGenExtension).apply {
            // Always store models in their own folder, so the hash is only looking at the files we generate
            val backendCsOutDir = File(repoRoot, "resharper/build/generated/Model/DebuggerWorker")
            val frontendKtOutDir =
                File(repoRoot, "rider/src/main/gen/kotlin/com/jetbrains/rider/plugins/unity/model/debuggerWorker")
            verbose = project.gradle.startParameter.logLevel == LogLevel.INFO
                || project.gradle.startParameter.logLevel == LogLevel.DEBUG
            classpath({ backend.getRiderModelJar() })
            sources(modelSrcDir)
            hashFolder = "$hashBaseDir/debuggerWorker"
            packages = "model.debuggerWorker"

            generator {
                language = "kotlin"
                transform = "asis"
                root = "com.jetbrains.rider.model.nova.debugger.main.DebuggerRoot"
                directory = frontendKtOutDir.absolutePath
            }

            generator {
                language = "csharp"
                transform = "reversed"
                root = "com.jetbrains.rider.model.nova.debugger.main.DebuggerRoot"
                directory = backendCsOutDir.absolutePath
            }
        }
    }

    val generateModels by registering {
        group = protocolGroup
        description = "Generates protocol models."
        dependsOn(generateFrontendBackendModel, generateBackendUnityModel, generateDebuggerWorkerModel)
    }

    named<KotlinCompile>("compileKotlin") {
        dependsOn(generateModels)
        kotlinOptions {
            jvmTarget = "11"
            allWarningsAsErrors = warningsAsErrors
        }
    }

    named<KotlinCompile>("compileTestKotlin") {
        kotlinOptions {
            jvmTarget = "11"
            allWarningsAsErrors = warningsAsErrors
        }
    }

    val prepareRiderBuildProps by registering(GenerateDotNetSdkPathPropsTask::class) {
        group = backendGroup
        dotNetSdkPath = { backend.getDotNetSdkPath() }
    }

    val prepareNuGetConfig by registering(GenerateNuGetConfig::class) {
        group = backendGroup
        dependsOn(prepareRiderBuildProps)
        dotNetSdkPath = { backend.getDotNetSdkPath() }
    }

    val buildReSharperHostPlugin by registering(DotNetBuildTask::class) {
        group = backendGroup
        description = "Builds the full ReSharper backend plugin solution"
        dependsOn(prepareNuGetConfig, generateModels)
        onlyIf {
            skipDotnet.not()
        }
        buildFile.set(backend.resharperHostPluginSolution)
    }
    val buildUnityEditorPlugin by registering(DotNetBuildTask::class) {
        group = backendGroup
        description = "Builds the Unity editor plugin"
        dependsOn(prepareNuGetConfig, generateModels)
        onlyIf {
            skipDotnet.not()
        }
        buildFile.set(backend.unityPluginSolution)
    }

    val packReSharperPlugin by registering(com.ullink.NuGetPack::class) {
        group = backendGroup
        onlyIf { isWindows } // non-windows builds are just for running tests, and agents don't have `mono` installed. NuGetPack requires `mono` though.
        description = "Packs resulting DLLs into a NuGet package which is an R# extension."
        dependsOn(buildReSharperHostPlugin)

        val changelogNotes = changelog.get(pluginVersion).withFilter { line ->
            !line.startsWith("- Rider:") && !line.startsWith("- Unity editor:")
        }.toPlainText().trim().let {
            // There's a bug in the changelog plugin that adds extra newlines on Windows, possibly
            // due to Unix/Windows line ending mismatch.
            // Remove this hack once JetBrains/gradle-changelog-plugin#8 is fixed
            if (isWindows) {
                it.replace("\n\n", "\r\n")
            } else {
                it
            }
        }
        val releaseNotes = """New in $pluginVersion
                            
$changelogNotes
                            
See CHANGELOG.md in the JetBrains/resharper-unity GitHub repo for more details and history.""".let {
            if (isWindows) {
                it.replace("&quot;", "\\\"")
            }
            else {
                it.replace("&quot;", "\"")
            }
        }

        // The command line to call nuget pack passes properties as a semi-colon delimited string
        // We can't have HTML encoded entities (e.g. &quot;)
        if (releaseNotes.contains(";")) throw GradleException("Release notes cannot semi-colon")

        setNuspecFile(File(backend.backendRoot, "resharper-unity/src/resharper-unity.resharper.nuspec").canonicalPath)
        setDestinationDir(File(backend.backendRoot, "build/distributions/$buildConfiguration").canonicalPath)
        packageAnalysis = false
        packageVersion = version
        properties = mapOf(
            "Configuration" to buildConfiguration,
            "ReleaseNotes" to releaseNotes
        )
        doFirst {
            buildServer.progress("Packing: ${nuspecFile.name}")
        }
    }

    val nunitReSharperJson by registering(NUnit::class) {
        group = testGroup
        shadowCopy = false
        outputs.upToDateWhen { false }
        val buildDir = File(repoRoot, "resharper/build")
        val testDll =
            File(buildDir, "Json.Tests/bin/$buildConfiguration/net472/JetBrains.ReSharper.Plugins.Json.Tests.dll")
        testAssemblies = listOf(testDll)
    }

    val nunitReSharperYaml by registering(NUnit::class) {
        group = testGroup
        shadowCopy = false
        outputs.upToDateWhen { false }
        val buildDir = File(repoRoot, "resharper/build")
        val testDll =
            File(buildDir, "Yaml.Tests/bin/$buildConfiguration/net472/JetBrains.ReSharper.Plugins.Yaml.Tests.dll")
        testAssemblies = listOf(testDll)
    }

    val nunitReSharperUnity by registering(NUnit::class) {
        group = testGroup
        shadowCopy = false
        outputs.upToDateWhen { false }
        useX86 = true
        val buildDir = File(repoRoot, "resharper/build")
        val testDll = File(
            buildDir,
            "resharper-unity/bin/$buildConfiguration/net472/JetBrains.ReSharper.Plugins.Unity.Tests.ReSharper.dll"
        )
        testAssemblies = listOf(testDll)
    }

    val nunitRiderUnity by registering(NUnit::class) {
        group = testGroup
        shadowCopy = false
        outputs.upToDateWhen { false }
        useX86 = true
        val buildDir = File(repoRoot, "resharper/build")
        val testDll = File(
            buildDir,
            "rider-unity/bin/$buildConfiguration/net472/JetBrains.ReSharper.Plugins.Unity.Tests.Rider.dll"
        )
        testAssemblies = listOf(testDll)
    }

    val runNunit by registering {
        group = testGroup
        // nunit3 defaults to running test assemblies in parallel, which causes problems with shared access to databases
        // The nunit plugin doesn't have the ability to disable this, so we'll do it long hand...
        dependsOn(
            buildReSharperHostPlugin,
            buildUnityEditorPlugin,
            nunitReSharperJson,
            nunitReSharperYaml,
            nunitReSharperUnity,
            nunitRiderUnity
        )
    }

    // It might be better to make it top-level task that is called separately, e.g. gradle buildPlugin nunit
    // (and we could get rid of RunTests then, too)
    if (runTests) {
        runNunit.get().shouldRunAfter(buildReSharperHostPlugin, buildUnityEditorPlugin)
        buildReSharperHostPlugin.get().finalizedBy(runNunit)
    }

    val publishCiBackendArtifacts by registering {
        group = ciGroup
        inputs.files(packReSharperPlugin.get().outputs)
        doLast {
            buildServer.publishArtifact(packReSharperPlugin.get().packageFile)
        }
    }

    val publishCiBuildData by registering {
        group = ciGroup
        dependsOn(publishCiBuildNumber, publishCiBackendArtifacts)
    }

    if (buildServer.isAutomatedBuild) {
        named("buildPlugin") {
            finalizedBy(publishCiBuildData)
        }
    }

    // TODO: Remove this!
    // Workaround for a bug in the SDK which isn't preserving the permissions for lib/ReSharperHost/[macosx|linux]-x64/dotnet/dotnet
    val fixSdkPermissions by registering {
        doLast {
            if (!isWindows) {
                logger.lifecycle("==============================================================================")
                logger.lifecycle("Temporary fix for file permissions. Resetting executable permission for:")
                logger.lifecycle(backend.getDotNetSdkPath().toString() + "/../ReSharperHost/linux-x64/dotnet/dotnet")
                logger.lifecycle(backend.getDotNetSdkPath().toString() + "/../ReSharperHost/macosx-x64/dotnet/dotnet")
                logger.lifecycle("==============================================================================")

                project.exec {
                    commandLine("chmod", "+x", backend.getDotNetSdkPath().toString() + "/../ReSharperHost/linux-x64/dotnet/dotnet")
                }
                project.exec {
                    commandLine("chmod", "+x", backend.getDotNetSdkPath().toString() + "/../ReSharperHost/macos-x64/dotnet/dotnet")
                }
            }
        }
    }

    withType<PrepareSandboxTask> {
        // Default dependsOn includes the standard Java build/jar task
        dependsOn(buildReSharperHostPlugin, buildUnityEditorPlugin, fixSdkPermissions)

        // Have dependent tasks use upToDateWhen { project.buildServer.automatedBuild etc. }
        //inputs.files(buildRiderPlugin.outputs)
        //inputs.files(buildUnityEditorPlugin.packedPath)

        // Backend:
        // Copy unity editor plugin repacked file to `rider-unity/EditorPlugin`
        // Copy JetBrains.ReSharper.Plugins.Unity.dll to `rider-unity/dotnet`
        // Copy annotations to `rider-unity/dotnet/Extensions/JetBrains.Unity/annotations`

        // Frontend:
        // Copy projectTemplates to `rider-unity/projectTemplates`
        doLast {
            dotnetDllFiles.forEach { if (!it.exists()) error("File $it does not exist") }
            debuggerDllFiles.forEach { if (!it.exists()) error("File $it does not exist") }
            helperExeFiles.forEach { if (!it.exists()) error("File $it does not exist") }
            helperExeNetFxFiles.forEach { if (!it.exists()) error("File $it does not exist") }
            unityEditorDllFiles.forEach { if (!it.exists()) error("File $it does not exist") }
        }

        val pluginName = intellij.pluginName.get()

        dotnetDllFiles.forEach { from(it) { into("${pluginName}/dotnet") } }
        debuggerDllFiles.forEach { from(it) { into("${pluginName}/dotnetDebuggerWorker") } }
        unityEditorDllFiles.forEach { from(it) { into("${pluginName}/EditorPlugin") } }

        // This folder name allows RiderEnvironment.getBundledFile(file, pluginClass = this.class) to work
        // Helper apps must be net5.0 for Mac/Linux, but we don't yet bundle netcore for Windows, so fall back to
        // netfx. Get rid of the netfx folder as soon as we can
        helperExeFiles.forEach { from(it) { into("${pluginName}/DotFiles") } }
        helperExeNetFxFiles.forEach { from(it) { into("${pluginName}/DotFiles/netfx") } }

        from("../resharper/resharper-unity/src/annotations") {
            into("${pluginName}/dotnet/Extensions/com.intellij.resharper.unity/annotations")
        }
        from("projectTemplates") { into("${pluginName}/projectTemplates") }
    }

    withType<Test> {
        useTestNG()
        if (project.hasProperty("integrationTests")) {
            val testsType = project.property("integrationTests").toString()
            if (testsType == "include") {
                include("integrationTests/**")
            } else if (testsType == "exclude") {
                exclude("integrationTests/**")
            }
        }
        testLogging {
            showStandardStreams = true
            exceptionFormat = TestExceptionFormat.FULL
        }
    }
}
