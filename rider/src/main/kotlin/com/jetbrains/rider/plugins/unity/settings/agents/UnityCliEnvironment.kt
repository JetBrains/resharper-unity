package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.execution.configurations.PathEnvironmentVariableUtil
import com.intellij.openapi.components.Service
import com.intellij.openapi.components.service
import com.intellij.util.EnvironmentUtil
import com.intellij.util.SystemProperties
import org.jetbrains.annotations.TestOnly
import java.io.IOException
import java.nio.file.Files
import java.nio.file.Path

@Service(Service.Level.APP)
class UnityCliEnvironment private constructor(
    val findOnPath: (String) -> Path?,
    val environmentVariable: (String) -> String?,
    val userHome: () -> Path,
    val isExecutable: (Path) -> Boolean,
    val realPath: (Path) -> Path,
) {
    @Suppress("unused")
    constructor() : this(
        findOnPath = { PathEnvironmentVariableUtil.findFirst(it) },
        environmentVariable = { EnvironmentUtil.getValue(it) },
        userHome = { Path.of(SystemProperties.getUserHome()) },
        isExecutable = { Files.isExecutable(it) },
        realPath = ::readRealPath,
    )

    companion object {
        fun getInstance(): UnityCliEnvironment = service()

        @TestOnly
        fun environmentAnswering(
            findOnPath: (String) -> Path? = { null },
            environmentVariable: (String) -> String? = { null },
            userHome: () -> Path = { Path.of("/home/tester") },
            isExecutable: (Path) -> Boolean = { false },
            realPath: (Path) -> Path = { it },
        ): UnityCliEnvironment =
            UnityCliEnvironment(findOnPath, environmentVariable, userHome, isExecutable, realPath)
    }
}

// An absent path has no real path, so the caller keeps the one it asked about.
private fun readRealPath(path: Path): Path =
    try {
        path.toRealPath()
    }
    catch (_: IOException) {
        path
    }
