package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.execution.configurations.PathEnvironmentVariableUtil
import com.intellij.openapi.components.Service
import com.intellij.openapi.components.service
import com.intellij.util.EnvironmentUtil
import com.intellij.util.SystemProperties
import org.jetbrains.annotations.ApiStatus
import org.jetbrains.annotations.TestOnly
import java.io.IOException
import java.nio.file.Files
import java.nio.file.Path
import java.nio.file.attribute.BasicFileAttributes

@Service(Service.Level.APP)
@ApiStatus.Internal
class UnityCliEnvironment private constructor(
    val findOnPath: (String) -> Path?,
    val environmentVariable: (String) -> String?,
    val userHome: () -> Path,
    val isExecutable: (Path) -> Boolean,
    val realPath: (Path) -> Path,
    val fileStamp: (Path) -> UnityCliFileStamp?,
    val currentTimeMs: () -> Long,
) {
    @Suppress("unused")
    constructor() : this(
        findOnPath = { PathEnvironmentVariableUtil.findFirst(it) },
        environmentVariable = { EnvironmentUtil.getValue(it) },
        userHome = { Path.of(SystemProperties.getUserHome()) },
        isExecutable = { Files.isExecutable(it) },
        realPath = ::readRealPath,
        fileStamp = ::readFileStamp,
        currentTimeMs = System::currentTimeMillis,
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
            fileStamp: (Path) -> UnityCliFileStamp? = { null },
            currentTimeMs: () -> Long = System::currentTimeMillis,
        ): UnityCliEnvironment =
            UnityCliEnvironment(findOnPath, environmentVariable, userHome, isExecutable, realPath, fileStamp, currentTimeMs)
    }
}

/** The size and the modification time of a file. A change in either one means another binary. */
@ApiStatus.Internal
data class UnityCliFileStamp(val size: Long, val modifiedMs: Long)

// An absent path has no real path, so the caller keeps the one it asked about.
private fun readRealPath(path: Path): Path =
    try {
        path.toRealPath()
    }
    catch (_: IOException) {
        path
    }

// Java cannot read the attributes of an MSIX app execution alias, so the alias gets no stamp.
private fun readFileStamp(path: Path): UnityCliFileStamp? =
    try {
        val attributes = Files.readAttributes(path, BasicFileAttributes::class.java)
        if (attributes.isRegularFile) UnityCliFileStamp(attributes.size(), attributes.lastModifiedTime().toMillis()) else null
    }
    catch (_: IOException) {
        null
    }
