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
import org.jetbrains.changelog.Changelog
import org.jetbrains.changelog.ChangelogPluginExtension
import org.jetbrains.intellij.tasks.IntelliJInstrumentCodeTask
import org.jetbrains.intellij.tasks.PatchPluginXmlTask
import org.jetbrains.intellij.tasks.PrepareSandboxTask
import org.jetbrains.kotlin.gradle.tasks.KotlinCompile

plugins {
    id("idea")
    // Version is configured in gradle.properties
    id("com.jetbrains.rdgen")
    id("com.ullink.nuget") version "2.23"
    id("com.ullink.nunit") version "2.8"
    id("me.filippov.gradle.jvm.wrapper") version "0.14.0"
    id("org.jetbrains.changelog") version "2.0.0"
    id("org.jetbrains.intellij") // version in rider/buildSrc/build.gradle.kts
    id("org.jetbrains.grammarkit") version "2022.3"
    kotlin("jvm") // version comes from buildSrc
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
val productMonorepoDir = backend.getProductMonorepoRoot()
val monorepoPreGeneratedRootDir by lazy { productMonorepoDir?.resolve("Plugins/_Unity.Pregenerated") ?: error("Building not in monorepo") }
val monorepoPreGeneratedFrontendDir by lazy {  monorepoPreGeneratedRootDir.resolve("Frontend") }
val monorepoPreGeneratedBackendDir by lazy {  monorepoPreGeneratedRootDir.resolve("BackendModel") }
val monorepoPreGeneratedUnityDir by lazy {  monorepoPreGeneratedRootDir.resolve("UnityModel") }
val dotnetDllFiles = files(
    "../resharper/build/Unity/bin/$buildConfiguration/net472/JetBrains.ReSharper.Plugins.Unity.dll",
    "../resharper/build/Unity/bin/$buildConfiguration/net472/JetBrains.ReSharper.Plugins.Unity.pdb",
    "../resharper/build/Unity/bin/$buildConfiguration/net472/JetBrains.ReSharper.Plugins.Unity.Rider.dll",
    "../resharper/build/Unity/bin/$buildConfiguration/net472/JetBrains.ReSharper.Plugins.Unity.Rider.pdb",
    "../resharper/build/Unity/bin/$buildConfiguration/net472/JetBrains.ReSharper.Plugins.Unity.Shaders.dll",
    "../resharper/build/Unity/bin/$buildConfiguration/net472/JetBrains.ReSharper.Plugins.Unity.Shaders.pdb",
    "../resharper/build/Unity/bin/$buildConfiguration/net472/JetBrains.ReSharper.Plugins.Json.dll",
    "../resharper/build/Unity/bin/$buildConfiguration/net472/JetBrains.ReSharper.Plugins.Json.pdb",
    "../resharper/build/Unity/bin/$buildConfiguration/net472/JetBrains.ReSharper.Plugins.Json.Rider.dll",
    "../resharper/build/Unity/bin/$buildConfiguration/net472/JetBrains.ReSharper.Plugins.Json.Rider.pdb",
    "../resharper/build/Unity/bin/$buildConfiguration/net472/JetBrains.ReSharper.Plugins.Yaml.dll",
    "../resharper/build/Unity/bin/$buildConfiguration/net472/JetBrains.ReSharper.Plugins.Yaml.pdb",
    "../resharper/build/Unity/bin/$buildConfiguration/net472/JetBrains.ReSharper.Plugins.Yaml.Rider.dll",
    "../resharper/build/Unity/bin/$buildConfiguration/net472/JetBrains.ReSharper.Plugins.Yaml.Rider.pdb"
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
    sourceCompatibility = JavaVersion.VERSION_17
    targetCompatibility = JavaVersion.VERSION_17
}

sourceSets {
    main {
        java {
            srcDir("src/main/gen")
            srcDir("src/main/rdgen/kotlin")
        }
        resources {
            srcDir("src/main/rdgen/resources")
        }
    }
}

idea {
    module {
        generatedSourceDirs.add(file("src/main/rdgen/kotlin"))
        resourceDirs.add(file("src/main/rdgen/resources"))
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

    plugins.set(listOf("rider.intellij.plugin.appender", "com.intellij.css", "yaml", "dotCover"))
}

configure<ChangelogPluginExtension> {
    val regex = """^((0|[1-9]\d*)\.(0|[1-9]\d*)(\.\d+)?).*$|^Unreleased.*$""".toRegex()
    version.set(regex.matchEntire(project.version.toString())?.groups?.get(1)?.value)
    path.set("${project.projectDir}/../CHANGELOG.md")
    headerParserRegex.set(regex)
}

fun getChangelogItem(): Changelog.Item {
    return changelog.getOrNull(pluginVersion)
        ?: if (changelog.has(changelog.unreleasedTerm.get())) changelog.getUnreleased() else null
        ?: changelog.getLatest()
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
        gradleVersion = "7.6"
        distributionType = Wrapper.DistributionType.BIN
    }

    val patchPluginXml by named<PatchPluginXmlTask>("patchPluginXml") {
        changeNotes.set(
            """
        <body>
        <p><b>New in $pluginVersion</b></p>
        <p>
        ${changelog.renderItem(getChangelogItem(), Changelog.OutputType.HTML)}
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

            logger.lifecycle("$pluginXml.path is valid XML and contains only US-ASCII symbols, bytes: ${rawBytes.size}")
        }
    }

    processResources {
        dependsOn(validatePluginXml)
        copy {
            from("../common/dictionaries/unity.dic")
            into("src/main/rdgen/resources/com/jetbrains/rider/plugins/unity/spellchecker/")
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

    fun getRdModelsSources(): List<File> {
        val productsHome = backend.getProductMonorepoRoot()
        assert(productsHome != null) { "Monorepo root not found" }

        return listOf(
            File("$productsHome/Rider/Frontend/model/src"),
            File("$productsHome/Rider/ultimate/remote-dev/rd-ide-model-sources"),
            modelSrcDir)
    }

    fun generateLibModel(monorepo: Boolean) = registering(RdGenTask::class) {
        group = protocolGroup

        if (monorepo && productMonorepoDir == null) {
            doFirst {
                throw GradleException("Building not in monorepo")
            }
            return@registering
        }

        (extensions.getByName("params") as RdGenExtension).apply {
            // Always store models in their own folder, so the hash is only looking at the files we generate

            // *** Out Dirs ***
            val backendCsOutDir =
                if (monorepo) monorepoPreGeneratedBackendDir.resolve("resharper/ModelLib")
                else File(repoRoot, "resharper/build/generated/Model/Lib")
            val unityEditorCsOutDir = 
                if (monorepo) monorepoPreGeneratedUnityDir.resolve("unity/ModelLib")
                else File(repoRoot, "unity/build/generated/Model/Lib")
            val frontendKtOutLayout = "src/main/rdgen/kotlin/com/jetbrains/rider/plugins/unity/model/lib"
            val frontendKtOutDir =
                if (monorepo) monorepoPreGeneratedFrontendDir.resolve(frontendKtOutLayout)
                else File(repoRoot, "rider/$frontendKtOutLayout")

            verbose = project.gradle.startParameter.logLevel == LogLevel.INFO
                || project.gradle.startParameter.logLevel == LogLevel.DEBUG

            // *** Classpath and sources ***
            if (monorepo)
                sources(getRdModelsSources())
            else {
                classpath({ backend.getRiderModelJar() })
                sources(modelSrcDir)
            }

            hashFolder = "$hashBaseDir/lib"
            packages = "model.lib"

            if (!monorepo) {
                // rdgen has a hash file that will handle rebuilds, but we still pay for launching rdgen
                inputs.files(modelSrcDir.resolve("lib/Library.kt"))
                outputs.files(frontendKtOutDir.resolve("Library.Generated.kt"),
                        backendCsOutDir.resolve("Library.Generated.cs"),
                        unityEditorCsOutDir.resolve("Library.Generated.cs"),
                        "$hashFolder/lib/.rdgen")
            }

            // Library is used as backend in backendUnityModel and backend in frontendBackendModel, so needs to be both
            // asis and reversed. I.e. symmetric
            generator {
                language = "csharp"
                transform = "symmetric"
                root = "model.lib.Library"
                directory = backendCsOutDir.canonicalPath
                if (monorepo) generatedFileSuffix = ".Pregenerated"
            }
            // Library is used as unity in backendUnityModel, so has reversed perspective
            generator {
                language = "csharp"
                transform = "reversed"
                root = "model.lib.Library"
                directory = unityEditorCsOutDir.canonicalPath
                if (monorepo) generatedFileSuffix = ".Pregenerated"
            }   
            // Library is used as frontend in frontendBackendModel, so has same perspective. Generate as-is
            generator {
                language = "kotlin"
                transform = "asis"
                root = "model.lib.Library"
                directory = frontendKtOutDir.canonicalPath
                if (monorepo) generatedFileSuffix = ".Pregenerated"
            }
        }
    }

    val generateLibModel by generateLibModel(false)
    val generateLibModelMonorepo by generateLibModel(true)

    fun generateFrontendBackendModel(monorepo: Boolean) = registering(RdGenTask::class) {
        group = protocolGroup

        if (monorepo && productMonorepoDir == null) {
            doFirst {
                throw GradleException("Building not in monorepo")
            }
            return@registering
        }

        if (monorepo) dependsOn(generateLibModelMonorepo)
        else dependsOn(generateLibModel)

        (extensions.getByName("params") as RdGenExtension).apply {
            // Always store models in their own folder, so the hash is only looking at the files we generate

            // *** Out Dirs ***
            val backendCsOutDir =
                if (monorepo) monorepoPreGeneratedBackendDir.resolve("resharper/FrontendBackend")
                else File(repoRoot, "resharper/build/generated/Model/FrontendBackend")
            val frontendKtOutLayout = "src/main/rdgen/kotlin/com/jetbrains/rider/plugins/unity/model/frontendBackend"
            val frontendKtOutDir =
                if (monorepo) monorepoPreGeneratedFrontendDir.resolve(frontendKtOutLayout)
                else File(repoRoot, "rider/$frontendKtOutLayout")

            verbose = project.gradle.startParameter.logLevel == LogLevel.INFO
                || project.gradle.startParameter.logLevel == LogLevel.DEBUG

            // *** Classpath and sources ***
            if (monorepo)
                sources(getRdModelsSources())
            else {
                classpath({ backend.getRiderModelJar() })
                sources(modelSrcDir)
            }

            hashFolder = "$hashBaseDir/frontendBackend"
            packages = "model.frontendBackend"

            if (!monorepo) {
                // rdgen has a hash file that will handle rebuilds, but we still pay for launching rdgen
                inputs.files(modelSrcDir.resolve("lib/Library.kt"),
                        modelSrcDir.resolve("frontendBackend/FrontendBackendModel.kt"))
                outputs.files(frontendKtOutDir.resolve("FrontendBackendModel.Generated.kt"),
                        backendCsOutDir.resolve("FrontendBackendModel.Generated.cs"),
                        "$hashFolder/lib/.rdgen",
                        "$hashFolder/frontendBackend/.rdgen")
            }

            generator {
                language = "kotlin"
                transform = "asis"
                root = "com.jetbrains.rider.model.nova.ide.IdeRoot"
                directory = frontendKtOutDir.absolutePath
                if (monorepo) generatedFileSuffix = ".Pregenerated"
            }

            generator {
                language = "csharp"
                transform = "reversed"
                root = "com.jetbrains.rider.model.nova.ide.IdeRoot"
                directory = backendCsOutDir.absolutePath
                if (monorepo) generatedFileSuffix = ".Pregenerated"
            }
        }
    }

    val generateFrontendBackendModel by generateFrontendBackendModel(false)
    val generateFrontendBackendModelMonorepo by generateFrontendBackendModel(true)

    fun generateBackendUnityModel(monorepo: Boolean) = registering(RdGenTask::class) {
        group = protocolGroup

        if (monorepo && productMonorepoDir == null) {
            doFirst {
                throw GradleException("Building not in monorepo")
            }
            return@registering
        }

        if (monorepo) dependsOn(generateLibModelMonorepo)
        else dependsOn(generateLibModel)

        (extensions.getByName("params") as RdGenExtension).apply {
            // Always store models in their own folder, so the hash is only looking at the files we generate

            // *** Out Dirs ***
            val backendCsOutDir =
                if (monorepo) monorepoPreGeneratedBackendDir.resolve("resharper/BackendUnity")
                else File(repoRoot, "resharper/build/generated/Model/BackendUnity")
            val unityEditorCsOutDir =
                if (monorepo) monorepoPreGeneratedUnityDir.resolve("unity/BackendUnity")
                else File(repoRoot, "unity/build/generated/Model/BackendUnity")

            verbose = project.gradle.startParameter.logLevel == LogLevel.INFO
                || project.gradle.startParameter.logLevel == LogLevel.DEBUG

            // *** Classpath and sources ***
            if (monorepo)
                sources(getRdModelsSources())
            else {
                classpath({ backend.getRiderModelJar() })
                sources(modelSrcDir)
            }

            hashFolder = "$hashBaseDir/backendUnity"
            packages = "model.backendUnity"

            if (!monorepo) {
                // rdgen has a hash file that will handle rebuilds, but we still pay for launching rdgen
                inputs.files(modelSrcDir.resolve("lib/Library.kt"),
                        modelSrcDir.resolve("backendUnity/BackendUnityModel.kt"))
                outputs.files(backendCsOutDir.resolve("BackendUnityModel.Generated.cs"),
                        unityEditorCsOutDir.resolve("BackendUnityModel.Generated.cs"),
                        "$hashFolder/lib/.rdgen",
                        "$hashFolder/backendUnity/.rdgen")
            }

            generator {
                language = "csharp"
                transform = "asis"
                root = "model.backendUnity.BackendUnityModel"
                directory = backendCsOutDir.canonicalPath
                if (monorepo) generatedFileSuffix = ".Pregenerated"
            }

            generator {
                language = "csharp"
                transform = "reversed"
                root = "model.backendUnity.BackendUnityModel"
                directory = unityEditorCsOutDir.canonicalPath
                if (monorepo) generatedFileSuffix = ".Pregenerated"
            }
        }
    }

    val generateBackendUnityModel by generateBackendUnityModel(false)
    val generateBackendUnityModelMonorepo by generateBackendUnityModel(true)

    fun generateDebuggerWorkerModel(monorepo: Boolean) = registering(RdGenTask::class) {
        group = protocolGroup

        if (monorepo && productMonorepoDir == null) {
            doFirst {
                throw GradleException("Building not in monorepo")
            }
            return@registering
        }

        (extensions.getByName("params") as RdGenExtension).apply {
            // Always store models in their own folder, so the hash is only looking at the files we generate

            // *** Out Dirs ***
            val backendCsOutDir =
                if (monorepo) monorepoPreGeneratedBackendDir.resolve("resharper/DebuggerWorker")
                else File(repoRoot, "resharper/build/generated/Model/DebuggerWorker")
            val frontendKtOutLayout = "src/main/rdgen/kotlin/com/jetbrains/rider/plugins/unity/model/debuggerWorker"
            val frontendKtOutDir =
                if (monorepo) monorepoPreGeneratedFrontendDir.resolve(frontendKtOutLayout)
                else File(repoRoot, "rider/$frontendKtOutLayout")

            verbose = project.gradle.startParameter.logLevel == LogLevel.INFO
                || project.gradle.startParameter.logLevel == LogLevel.DEBUG

            // *** Classpath and sources ***
            if (monorepo)
                sources(getRdModelsSources())
            else {
                classpath({ backend.getRiderModelJar() })
                sources(modelSrcDir)
            }

            hashFolder = "$hashBaseDir/debuggerWorker"
            packages = "model.debuggerWorker"

            if (!monorepo) {
                // rdgen has a hash file that will handle rebuilds, but we still pay for launching rdgen
                inputs.files(modelSrcDir.resolve("debuggerWorker/UnityDebuggerWorkerModel.kt"))
                outputs.files(frontendKtOutDir.resolve("UnityDebuggerWorkerModel.Generated.kt"),
                        backendCsOutDir.resolve("UnityDebuggerWorkerModel.Generated.cs"),
                        "$hashFolder/debuggerWorker/.rdgen")
            }

            generator {
                language = "kotlin"
                transform = "asis"
                root = "com.jetbrains.rider.model.nova.debugger.main.DebuggerRoot"
                directory = frontendKtOutDir.absolutePath
                if (monorepo) generatedFileSuffix = ".Pregenerated"
            }

            generator {
                language = "csharp"
                transform = "reversed"
                root = "com.jetbrains.rider.model.nova.debugger.main.DebuggerRoot"
                directory = backendCsOutDir.absolutePath
                if (monorepo) generatedFileSuffix = ".Pregenerated"
            }
        }
    }


    val generateDebuggerWorkerModel by generateDebuggerWorkerModel(false)
    val generateDebuggerWorkerModelMonorepo by generateDebuggerWorkerModel(true)

    val generateModels by registering {
        group = protocolGroup
        description = "Generates protocol models."
        dependsOn(generateFrontendBackendModel, generateBackendUnityModel, generateDebuggerWorkerModel)
    }

    val generateModelsMonorepo by registering {
        group = protocolGroup
        description = "Generates protocol models for monorepo."
        dependsOn(generateFrontendBackendModelMonorepo, generateBackendUnityModelMonorepo, generateDebuggerWorkerModelMonorepo)
    }

    named<KotlinCompile>("compileKotlin") {
        dependsOn(generateModels)
        kotlinOptions {
            freeCompilerArgs = listOf("-Xjvm-default=all")
            jvmTarget = "17"
            allWarningsAsErrors = warningsAsErrors
        }
    }

    named<KotlinCompile>("compileTestKotlin") {
        kotlinOptions {
            jvmTarget = "17"
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

    val packReSharperPlugin by creating(com.ullink.NuGetPack::class) {
        group = backendGroup
        onlyIf { isWindows } // non-windows builds are just for running tests, and agents don't have `mono` installed. NuGetPack requires `mono` though.
        description = "Packs resulting DLLs into a NuGet package which is an R# extension."
        dependsOn(buildReSharperHostPlugin)

        val changelogNotes = changelog.renderItem(getChangelogItem().withFilter { line ->
            !line.startsWith("- Rider:") && !line.startsWith("- Unity editor:")
        }, Changelog.OutputType.PLAIN_TEXT).trim().let {
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

        setNuspecFile(File(backend.backendRoot, "resharper-unity/src/Unity/resharper-unity.resharper.nuspec").canonicalPath)
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
            "Unity.Tests/bin/$buildConfiguration/net472/JetBrains.ReSharper.Plugins.Unity.Tests.dll"
        )
        testAssemblies = listOf(testDll)
    }

    val nunitReSharperUnityRider by registering(NUnit::class) {
        group = testGroup
        shadowCopy = false
        outputs.upToDateWhen { false }
        useX86 = true
        val buildDir = File(repoRoot, "resharper/build")
        val testDll = File(
            buildDir,
            "Unity.Rider.Tests/bin/$buildConfiguration/net472/JetBrains.ReSharper.Plugins.Unity.Rider.Tests.dll"
        )
        testAssemblies = listOf(testDll)
    }


    val runNunit by registering {
        group = testGroup
        // nunit3 defaults to running test assemblies in parallel, which causes problems with shared access to databases
        // The nunit plugin doesn't have the ability to disable this, so we'll do it long hand...
        dependsOn(
            buildReSharperHostPlugin,
            nunitReSharperJson,
            nunitReSharperYaml,
            nunitReSharperUnity,
            nunitReSharperUnityRider
        )
    }

    // It might be better to make it top-level task that is called separately, e.g. gradle buildPlugin nunit
    // (and we could get rid of RunTests then, too)
    if (runTests) {
        runNunit.get().shouldRunAfter(buildReSharperHostPlugin)
        buildReSharperHostPlugin.get().finalizedBy(runNunit)
    }

    val publishCiBackendArtifacts by registering {
        group = ciGroup
        inputs.files(packReSharperPlugin.outputs)
        doLast {
            buildServer.publishArtifact(packReSharperPlugin.packageFile)
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

    withType<PrepareSandboxTask> {
        // Default dependsOn includes the standard Java build/jar task
        dependsOn(buildReSharperHostPlugin)

        // Have dependent tasks use upToDateWhen { project.buildServer.automatedBuild etc. }
        //inputs.files(buildRiderPlugin.outputs)

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

        from("../resharper/resharper-unity/src/Unity/annotations") {
            into("${pluginName}/dotnet/Extensions/com.intellij.resharper.unity/annotations")
        }
        from("projectTemplates") { into("${pluginName}/projectTemplates") }
    }

    withType<Test> {
        useTestNG()

        if (project.hasProperty("ignoreFailures")) { ignoreFailures = true }

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
