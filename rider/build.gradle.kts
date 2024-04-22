import com.ullink.gradle.nunit.NUnit
import groovy.xml.XmlParser
import org.gradle.api.tasks.testing.logging.TestExceptionFormat
import org.jetbrains.changelog.Changelog
import org.jetbrains.changelog.ChangelogPluginExtension

import org.jetbrains.intellij.tasks.InstrumentCodeTask
import org.jetbrains.intellij.tasks.PatchPluginXmlTask
import org.jetbrains.intellij.tasks.PrepareSandboxTask
import org.jetbrains.kotlin.gradle.tasks.KotlinCompile
import java.util.*

plugins {
    id("com.ullink.nuget") version "2.23"
    id("com.ullink.nunit") version "2.8"
    id("me.filippov.gradle.jvm.wrapper")
    id("org.jetbrains.changelog") version "2.0.0"
    id("org.jetbrains.intellij")
    kotlin("jvm")
}

repositories {
    maven("https://cache-redirector.jetbrains.com/intellij-dependencies")
    maven("https://cache-redirector.jetbrains.com/intellij-repository/releases")
    maven("https://cache-redirector.jetbrains.com/intellij-repository/snapshots")
    maven("https://cache-redirector.jetbrains.com/maven-central")
}

apply {
    plugin("kotlin")
}

val repoRoot = projectDir.parentFile!!
val isWindows = System.getProperty("os.name").lowercase(Locale.getDefault()).startsWith("win")
val bundledRiderSdkRoot = File(projectDir, "build/rider")  // SDK from TC configuration/artifacts
val bundledMavenArtifacts = File(projectDir, "build/maven-artifacts")
val productVersion = extra["productVersion"].toString()
val maintenanceVersion = extra["maintenanceVersion"].toString()
val pluginVersion = "$productVersion.$maintenanceVersion"
val buildCounter = extra["BuildCounter"].toString()
val releaseConfiguration = "Release"
val isAutomatedBuild = System.getenv("TEAMCITY_VERSION") != null
val buildConfiguration = if (isAutomatedBuild) {
    releaseConfiguration
} else {
    extra["BuildConfiguration"]
}
val isReleaseBuild = buildConfiguration == releaseConfiguration
val warningsAsErrors = extra["warningsAsErrors"].toString().lowercase(Locale.getDefault()).toBoolean()
val modelSrcDir = File(repoRoot, "rider/protocol/src/main/kotlin/model")
val hashBaseDir = File(repoRoot, "rider/build/rdgen")
val skipDotnet = extra["skipDotnet"].toString().lowercase(Locale.getDefault()).toBoolean()
val runTests = extra["RunTests"].toString().lowercase(Locale.getDefault()).toBoolean()
val isMonorepo = rootProject.projectDir != projectDir

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

val textureDebuggerDllFiles = files(
    "../resharper/build/texture-debugger/bin/$buildConfiguration/net472/JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Presentation.Texture.dll",
    "../resharper/build/texture-debugger/bin/$buildConfiguration/net472/JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Presentation.Texture.pdb"
)

val pausePointDllFiles = files(
    "../resharper/build/pausepoint-helper/bin/$buildConfiguration/net472/JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.PausePoint.Helper.dll",
    "../resharper/build/pausepoint-helper/bin/$buildConfiguration/net472/JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.PausePoint.Helper.pdb"
)

val listIosUsbDevicesFiles = files(
    "../resharper/build/ios-list-usb-devices/bin/$buildConfiguration/net7.0/JetBrains.Rider.Unity.ListIosUsbDevices.dll",
    "../resharper/build/ios-list-usb-devices/bin/$buildConfiguration/net7.0/JetBrains.Rider.Unity.ListIosUsbDevices.pdb",
    "../resharper/build/ios-list-usb-devices/bin/$buildConfiguration/net7.0/JetBrains.Rider.Unity.ListIosUsbDevices.runtimeconfig.json"
)

val unityEditorDllFiles = files(
    "../unity/build/EditorPlugin.SinceUnity.2019.2/bin/$buildConfiguration/netstandard2.0/JetBrains.Rider.Unity.Editor.Plugin.Net46.Repacked.dll",
    "../unity/build/EditorPlugin.SinceUnity.2019.2/bin/$buildConfiguration/netstandard2.0/JetBrains.Rider.Unity.Editor.Plugin.Net46.Repacked.pdb"
)

val rdLibDirectory: () -> File = { file("${tasks.setupDependencies.get().idea.get().classes}/lib/rd") }

val rdModelJarFile: File by lazy {
    val jarFile = File(rdLibDirectory(), "rider-model.jar").canonicalFile
    assert(jarFile.isFile)
    return@lazy jarFile
}

val backendDir = projectDir.parentFile.resolve("resharper")
val resharperHostPluginSolution =  backendDir.resolve("resharper-unity.sln")

version = "${pluginVersion}.$buildCounter"

java {
    sourceCompatibility = JavaVersion.VERSION_17
    targetCompatibility = JavaVersion.VERSION_17
}

sourceSets {
    main {
        java {
            srcDir("src/main/gen")
            srcDir("src/generated/kotlin")
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

    plugins.set(listOf("rider.intellij.plugin.appender", "com.intellij.css", "yaml", "dotCover", "org.intellij.intelliLang"))
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

val riderModel: Configuration by configurations.creating {
    isCanBeConsumed = true
    isCanBeResolved = false
}

artifacts {
    add(riderModel.name, provider {
        val sdkRoot = tasks.setupDependencies.get().idea.get().classes
        sdkRoot.resolve("lib/rd/rider-model.jar").also {
            check(it.isFile) {
                "rider-model.jar is not found at $riderModel"
            }
        }
    }) {
        builtBy(tasks.setupDependencies)
    }
}

tasks {
    val backendGroup = "backend"
    val ciGroup = "ci"
    val testGroup = "verification"

    val dotNetSdkPath by lazy {
        val sdkPath = setupDependencies.get().idea.get().classes.resolve("lib").resolve("DotNetSdkForRdPlugins")
        if (sdkPath.isDirectory.not()) error("$sdkPath does not exist or not a directory")

        println("SDK path: $sdkPath")
        return@lazy sdkPath
    }

    buildSearchableOptions {
        enabled = isReleaseBuild
    }

    withType<InstrumentCodeTask> {
        // For SDK from local folder, you also need to manually download maven-artifacts folder
        // from SDK build artifacts on TC and put it into the build folder.
        if (bundledMavenArtifacts.exists()) {
            logger.lifecycle("Use ant compiler artifacts from local folder: $bundledMavenArtifacts")
            compilerClassPathFromMaven.set(
                bundledMavenArtifacts.walkTopDown()
                    .filter { it.extension == "jar" && !it.name.endsWith("-sources.jar") }
                    .toList()
                    + File("${setupDependencies.get().idea.get().classes}/lib/3rd-party-rt.jar")
                    + File("${ideaDependency.get().classes}/lib/util.jar")
                    + File("${ideaDependency.get().classes}/lib/util-8.jar")
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
        gradleVersion = "8.1"
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
            println("##teamcity[buildNumber '$version']")
        }
    }

    named<KotlinCompile>("compileKotlin") {
        dependsOn(":protocol:rdgen")
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

    val prepareRiderBuildProps = register("prepareRiderBuildProps") {
        val propsFile =
            File("${project.projectDir}/../resharper/build/generated/DotNetSdkPath.generated.props")
        group = backendGroup
        inputs.dir(dotNetSdkPath)
        outputs.file(propsFile)
        doLast {
            val dotNetSdkFile= dotNetSdkPath
            assert(dotNetSdkFile.isDirectory)
            logger.info("Generating :${propsFile.canonicalPath}...")
            project.file(propsFile).writeText("""<Project>
          <PropertyGroup>
            <DotNetSdkPath>${dotNetSdkFile.canonicalPath}</DotNetSdkPath>
          </PropertyGroup>
        </Project>""".trimIndent())
        }
    }

    val prepareNuGetConfig = register("prepareNuGetConfig") {
        val nuGetConfigFile = File("${project.projectDir}/../NuGet.Config")
        dependsOn(prepareRiderBuildProps)
        group = backendGroup
        doLast {
            logger.info("dotNetSdk location: '$dotNetSdkPath'")
            assert(dotNetSdkPath.isDirectory)

            logger.info("Generating :${nuGetConfigFile.canonicalPath}...")
            val nugetConfigText = """<?xml version="1.0" encoding="utf-8"?>
            |<configuration>
            |  <packageSources>
            |    <clear />
            |    <add key="local-dotnet-sdk" value="${dotNetSdkPath.canonicalPath}" />
            |    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
            |  </packageSources>
            |</configuration>
            """.trimMargin()
            nuGetConfigFile.writeText(nugetConfigText)

            logger.info("Generated content:\n$nugetConfigText")

            val sb = StringBuilder("Dump dotNetSdkFile content:\n")
            for(file in dotNetSdkPath.listFiles() ?: emptyArray()) {
                sb.append("${file.canonicalPath}\n")
            }
            logger.info(sb.toString())
        }
    }

    val buildReSharperHostPlugin = register("buildReSharperHostPlugin") {
        group = backendGroup
        description = "Builds the full ReSharper backend plugin solution"
        dependsOn(prepareNuGetConfig, ":protocol:rdgen")
        onlyIf {
            skipDotnet.not()
        }

        doLast {
            val buildConfiguration = project.ext.get("BuildConfiguration").toString()
            val warningsAsErrors = project.ext.get("warningsAsErrors").toString()
            logger.info("Building $buildFile ($buildConfiguration)")

            val dotNetCliPath = projectDir.parentFile.resolve("dotnet-sdk.cmd")
            val verbosity = if (isAutomatedBuild) {
                "normal"
            } else {
                when (project.gradle.startParameter.logLevel) {
                    LogLevel.QUIET -> "quiet"
                    LogLevel.LIFECYCLE -> "minimal"
                    LogLevel.INFO -> "normal"
                    LogLevel.DEBUG -> "detailed"
                    else -> "normal"
                }
            }
            val buildArguments = listOf(
                "build",
                resharperHostPluginSolution.canonicalPath,
                "/p:Configuration=$buildConfiguration",
                "/p:Version=${project.version}",
                "/p:TreatWarningsAsErrors=$warningsAsErrors",
                "/v:$verbosity",
                "/bl:${resharperHostPluginSolution.name + ".binlog"}",
                "/nologo"
            )

            logger.info("dotnet call: '$dotNetCliPath' '$buildArguments' in '$backendDir'")
            project.exec {
                executable = dotNetCliPath.canonicalPath
                args = buildArguments
                workingDir = backendDir
            }
        }
    }

    val packReSharperPlugin by creating(com.ullink.NuGetPack::class) {
        // Don't know the way to rewrite this task in a lazy manner (using registering) because NuGetPack uses `project.afterEvaluate` in its implementation,
        // so it's just a workaround to not download the Rider SDK in the monorepo mode
        if (isMonorepo) {
            doFirst {
                throw GradleException("This task is not expected to be run in the monorepo environment")
            }

            return@creating
        }


        group = backendGroup
        onlyIf { isWindows } // non-windows builds are just for running tests, and agents don't have `mono` installed. NuGetPack requires `mono` though.
        description = "Packs resulting DLLs into a NuGet package which is an R# extension."
        dependsOn(buildReSharperHostPlugin)

        val changelogNotes = changelog.renderItem(getChangelogItem().withFilter { line ->
            !line.startsWith("- Rider:") && !line.startsWith("- Unity editor:")
        }, Changelog.OutputType.PLAIN_TEXT).trim().let {
            // There's a bug in the changelog plugin that adds extra newlines on Windows, possibly
            // due to a Unix / Windows line ending mismatch.
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
            } else {
                it.replace("&quot;", "\"")
            }
        }

        // The command line to call nuget pack passes properties as a semicolon-delimited string
        // We can't have HTML-encoded entities (e.g. &quot;)
        if (releaseNotes.contains(";")) throw GradleException("Release notes cannot semi-colon")

        setNuspecFile(
            File(
                backendDir,
                "resharper-unity/src/Unity/resharper-unity.resharper.nuspec"
            ).canonicalPath
        )
        setDestinationDir(File(backendDir, "build/distributions/$buildConfiguration").canonicalPath)
        packageAnalysis = false
        packageVersion = version
        properties = mapOf(
            "Configuration" to buildConfiguration,
            "ReleaseNotes" to releaseNotes
        )
        doFirst {
            logger.info("Packing: ${nuspecFile.name}")
        }
    }

    val nunitReSharperJson by registering(NUnit::class) {
        nunitVersion = "3.16.2" // newer than default, helps running with net 7
        group = testGroup
        shadowCopy = false
        outputs.upToDateWhen { false }
        val buildDir = File(repoRoot, "resharper/build")
        val testDll =
            File(buildDir, "Json.Tests/bin/$buildConfiguration/net472/JetBrains.ReSharper.Plugins.Json.Tests.dll")
        testAssemblies = listOf(testDll)
    }

    val nunitReSharperYaml by registering(NUnit::class) {
        nunitVersion = "3.16.2" // newer than default, helps running with net 7
        group = testGroup
        shadowCopy = false
        outputs.upToDateWhen { false }
        val buildDir = File(repoRoot, "resharper/build")
        val testDll =
            File(buildDir, "Yaml.Tests/bin/$buildConfiguration/net472/JetBrains.ReSharper.Plugins.Yaml.Tests.dll")
        testAssemblies = listOf(testDll)
    }

    val nunitReSharperUnity by registering(NUnit::class) {
        nunitVersion = "3.16.2" // newer than default, helps running with net 7
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
        nunitVersion = "3.16.2" // newer than default, helps running with net 7
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
        // nunit3 defaults to running test assemblies in parallel, which causes problems with shared access to databases;
        // The nunit plugin can't disable this, so we'll do it long hand...
        dependsOn(
            //buildReSharperHostPlugin,
            nunitReSharperJson,
            nunitReSharperYaml,
            nunitReSharperUnity,
            nunitReSharperUnityRider
        )
    }

    // It might be better to make it a top-level task called separately, e.g., gradle buildPlugin nunit
    // (and we could get rid of RunTests then, too)
    if (runTests) {
        runNunit.get().shouldRunAfter(buildReSharperHostPlugin)
        buildReSharperHostPlugin.configure {
            finalizedBy(runNunit)
        }
    }

    val publishCiBackendArtifacts by registering {
        group = ciGroup
        inputs.files(packReSharperPlugin.outputs)
        doLast {
            println("##teamcity[publishArtifacts '${packReSharperPlugin.packageFile.absolutePath}']")
        }
    }

    val publishCiBuildData by registering {
        group = ciGroup
        dependsOn(publishCiBuildNumber, publishCiBackendArtifacts)
    }

    if (isAutomatedBuild) {
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
        doLast {
            dotnetDllFiles.forEach { if (!it.exists()) error("File $it does not exist") }
            debuggerDllFiles.forEach { if (!it.exists()) error("File $it does not exist") }
            textureDebuggerDllFiles.forEach { if (!it.exists()) error("File $it does not exist") }
            pausePointDllFiles.forEach { if (!it.exists()) error("File $it does not exist") }
            listIosUsbDevicesFiles.forEach { if (!it.exists()) error("File $it does not exist") }
            unityEditorDllFiles.forEach { if (!it.exists()) error("File $it does not exist") }
        }

        val pluginName = intellij.pluginName.get()

        dotnetDllFiles.forEach { from(it) { into("${pluginName}/dotnet") } }
        debuggerDllFiles.forEach { from(it) { into("${pluginName}/dotnetDebuggerWorker") } }
        textureDebuggerDllFiles.forEach { from(it) { into("${pluginName}/DotFiles") } }
        pausePointDllFiles.forEach { from(it) { into("${pluginName}/DotFiles") } }
        listIosUsbDevicesFiles.forEach { from(it) { into("${pluginName}/DotFiles") } }
        unityEditorDllFiles.forEach { from(it) { into("${pluginName}/EditorPlugin") } }

        from("../resharper/resharper-unity/src/Unity/annotations") {
            into("${pluginName}/dotnet/Extensions/com.intellij.resharper.unity/annotations")
        }
    }

    withType<Test>().configureEach {
        useTestNG()

        if (project.hasProperty("ignoreFailures")) {
            ignoreFailures = true
        }

        if (project.hasProperty("integrationTests")) {
            val testsType = project.property("integrationTests").toString()
            if (testsType == "include") {
                include("com/jetbrains/rider/unity/test/cases/integrationTests/**")
            } else if (testsType == "exclude") {
                exclude("com/jetbrains/rider/unity/test/cases/integrationTests/**")
            }
        }
        testLogging {
            showStandardStreams = true
            exceptionFormat = TestExceptionFormat.FULL
        }
    }

    wrapper {
        gradleVersion = "8.7"
        distributionUrl = "https://cache-redirector.jetbrains.com/services.gradle.org/distributions/gradle-${gradleVersion}-bin.zip"
    }
}
