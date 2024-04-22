import com.jetbrains.rd.generator.gradle.RdGenTask

plugins {
    // Version is configured in gradle.properties
    id("com.jetbrains.rdgen")
    id("org.jetbrains.kotlin.jvm")
}

repositories {
    maven("https://cache-redirector.jetbrains.com/intellij-dependencies")
    maven("https://cache-redirector.jetbrains.com/maven-central")
}

val isMonorepo = rootProject.projectDir != projectDir.parentFile
val unityRepoRoot: File = projectDir.parentFile.parentFile

sourceSets {
    main {
        kotlin {
            srcDir(unityRepoRoot.resolve("rider/protocol/src/main/kotlin"))
        }
    }
}

data class UnityGeneratorSettings(
    val backendUnityCsOutDir: File,
    val backendUnityEditorCsOutDir: File,
    val debuggerWorkerCsOutDir: File,
    val debuggerWorkerKtOutDir: File,
    val frontendBackendCsOutDir: File,
    val frontendBackendKtOutDir: File,
    val modelLibCsOutDir: File,
    val modelLibEditorCsOutDir: File,
    val modelLibKtOutDir: File,
    val suffix: String
)

val debuggerWorkerKtOutLayout = "com/jetbrains/rider/plugins/unity/model/debuggerWorker"
val frontendBackendKtOutLayout = "com/jetbrains/rider/plugins/unity/model/frontendBackend"
val modelLibKtOutLayout = "com/jetbrains/rider/plugins/unity/model/lib"
val unityGeneratorSettings = if (isMonorepo) {
    val monorepoRoot = buildscript.sourceFile?.parentFile?.parentFile?.parentFile?.parentFile?.parentFile?.parentFile
        ?: error("Cannot find products home")
    check(monorepoRoot.resolve(".ultimate.root.marker").isFile) {
        error("Incorrect location in monorepo: monorepoRoot='$monorepoRoot'")
    }
    val monorepoPreGeneratedRootDir = monorepoRoot.resolve("dotnet/Plugins/_Unity.Pregenerated")
    val monorepoPreGeneratedBackendDir = monorepoPreGeneratedRootDir.resolve("BackendModel")
    val monorepoPreGeneratedUnityDir = monorepoPreGeneratedRootDir.resolve("UnityModel")
    val monorepoPreGeneratedFrontendDir = monorepoPreGeneratedRootDir.resolve("Frontend")
    UnityGeneratorSettings(
        monorepoPreGeneratedBackendDir.resolve("resharper/BackendUnity"),
        monorepoPreGeneratedUnityDir.resolve("unity/BackendUnity"),
        monorepoPreGeneratedBackendDir.resolve("resharper/DebuggerWorker"),
        monorepoPreGeneratedFrontendDir.resolve("src/main/rdgen/kotlin/$debuggerWorkerKtOutLayout"),
        monorepoPreGeneratedBackendDir.resolve("resharper/FrontendBackend"),
        monorepoPreGeneratedFrontendDir.resolve("src/main/rdgen/kotlin/$frontendBackendKtOutLayout"),
        monorepoPreGeneratedBackendDir.resolve("resharper/ModelLib"),
        monorepoPreGeneratedUnityDir.resolve("unity/ModelLib"),
        monorepoPreGeneratedFrontendDir.resolve("src/main/rdgen/kotlin/$modelLibKtOutLayout"),
        ".Pregenerated"
    )
} else {
    UnityGeneratorSettings(
        unityRepoRoot.resolve("resharper/build/generated/Model/BackendUnity"),
        unityRepoRoot.resolve("unity/build/generated/Model/BackendUnity"),
        unityRepoRoot.resolve("resharper/build/generated/Model/DebuggerWorker"),
        unityRepoRoot.resolve("rider/src/generated/kotlin/$debuggerWorkerKtOutLayout"),
        unityRepoRoot.resolve( "resharper/build/generated/Model/FrontendBackend"),
        unityRepoRoot.resolve( "rider/src/generated/kotlin/$frontendBackendKtOutLayout"),
        unityRepoRoot.resolve("resharper/build/generated/Model/Lib"),
        unityRepoRoot.resolve("unity/build/generated/Model/Lib"),
        unityRepoRoot.resolve("rider/src/generated/kotlin/$modelLibKtOutLayout"),
        ""
    )
}

rdgen {
    verbose = true

    generator {
        language = "csharp"
        transform = "asis"
        packages = "model.backendUnity,model.debuggerWorker,model.frontendBackend,model.lib"
        root = "model.backendUnity.BackendUnityModel"
        directory = unityGeneratorSettings.backendUnityCsOutDir.absolutePath
        generatedFileSuffix = unityGeneratorSettings.suffix
    }

    generator {
        language = "csharp"
        transform = "reversed"
        packages = "model.backendUnity,model.debuggerWorker,model.frontendBackend,model.lib"
        root = "model.backendUnity.BackendUnityModel"
        directory = unityGeneratorSettings.backendUnityEditorCsOutDir.absolutePath
        generatedFileSuffix = unityGeneratorSettings.suffix
    }

    generator {
        language = "csharp"
        transform = "reversed"
        packages = "model.backendUnity,model.debuggerWorker,model.frontendBackend,model.lib"
        root = "com.jetbrains.rider.model.nova.debugger.main.DebuggerRoot"
        directory = unityGeneratorSettings.debuggerWorkerCsOutDir.absolutePath
        generatedFileSuffix = unityGeneratorSettings.suffix
    }

    generator {
        language = "kotlin"
        transform = "asis"
        packages = "model.backendUnity,model.debuggerWorker,model.frontendBackend,model.lib"
        root = "com.jetbrains.rider.model.nova.debugger.main.DebuggerRoot"
        directory = unityGeneratorSettings.debuggerWorkerKtOutDir.absolutePath
        generatedFileSuffix = unityGeneratorSettings.suffix
    }

    generator {
        language = "csharp"
        transform = "reversed"
        packages = "model.backendUnity,model.debuggerWorker,model.frontendBackend,model.lib"
        root = "com.jetbrains.rider.model.nova.ide.IdeRoot"
        directory = unityGeneratorSettings.frontendBackendCsOutDir.absolutePath
        generatedFileSuffix = unityGeneratorSettings.suffix
    }

    generator {
        language = "kotlin"
        transform = "asis"
        packages = "model.backendUnity,model.debuggerWorker,model.frontendBackend,model.lib"
        root = "com.jetbrains.rider.model.nova.ide.IdeRoot"
        directory = unityGeneratorSettings.frontendBackendKtOutDir.absolutePath
        generatedFileSuffix = unityGeneratorSettings.suffix
    }

    // Library is used as backend in backendUnityModel and backend in frontendBackendModel, so needs to be both
    // asis and reversed.
    // I.e., symmetric
    generator {
        language = "csharp"
        transform = "symmetric"
        packages = "model.backendUnity,model.debuggerWorker,model.frontendBackend,model.lib"
        root = "model.lib.Library"
        directory = unityGeneratorSettings.modelLibCsOutDir.absolutePath
        generatedFileSuffix = unityGeneratorSettings.suffix
    }

    // The Library is used as unity in backendUnityModel, so has reversed perspective
    generator {
        language = "csharp"
        transform = "reversed"
        packages = "model.backendUnity,model.debuggerWorker,model.frontendBackend,model.lib"
        root = "model.lib.Library"
        directory = unityGeneratorSettings.modelLibEditorCsOutDir.absolutePath
        generatedFileSuffix = unityGeneratorSettings.suffix
    }

    // Library is used as frontend in frontendBackendModel, so has the same perspective. Generate as-is
    generator {
        language = "kotlin"
        transform = "asis"
        packages = "model.backendUnity,model.debuggerWorker,model.frontendBackend,model.lib"
        root = "model.lib.Library"
        directory = unityGeneratorSettings.modelLibKtOutDir.absolutePath
        generatedFileSuffix = unityGeneratorSettings.suffix
    }
}

tasks.withType<RdGenTask> {
    dependsOn(sourceSets["main"].runtimeClasspath)
    classpath(sourceSets["main"].runtimeClasspath)
}

dependencies {
    if (isMonorepo) {
        implementation(project(":rider-model"))
    } else {
        val rdVersion: String by project
        val rdKotlinVersion: String by project

        implementation("com.jetbrains.rd:rd-gen:$rdVersion")
        implementation("org.jetbrains.kotlin:kotlin-stdlib:$rdKotlinVersion")
        implementation(
            project(
                mapOf(
                    "path" to ":",
                    "configuration" to "riderModel"
                )
            )
        )
    }
}
