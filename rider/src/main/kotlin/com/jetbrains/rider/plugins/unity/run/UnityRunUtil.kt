package com.jetbrains.rider.plugins.unity.run

import com.intellij.execution.ProgramRunnerUtil
import com.intellij.execution.configurations.CommandLineTokenizer
import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.executors.DefaultDebugExecutor
import com.intellij.execution.process.ProcessInfo
import com.intellij.execution.runners.ExecutionEnvironmentBuilder
import com.intellij.execution.util.ExecUtil
import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.SystemInfo
import com.intellij.openapi.util.io.FileUtil
import com.intellij.openapi.util.text.StringUtil
import com.intellij.xdebugger.XDebuggerManager
import com.jetbrains.rider.plugins.unity.run.attach.UnityAttachProcessConfiguration
import com.jetbrains.rider.plugins.unity.util.EditorInstanceJson
import com.jetbrains.rider.plugins.unity.util.EditorInstanceJsonStatus
import com.jetbrains.rider.plugins.unity.util.convertPidToDebuggerPort
import com.jetbrains.rider.run.configurations.remote.RemoteConfiguration
import java.io.File
import java.nio.charset.StandardCharsets
import java.nio.file.Paths

data class UnityProcessInfo(val projectName: String?, val roleName: String?)

object UnityRunUtil {
    private val logger = Logger.getInstance(UnityRunUtil::class.java)

    fun isUnityEditorProcess(processInfo: ProcessInfo): Boolean {
        logger.trace("Checking Unity Process: isUnityEditorProcess")
        val name = processInfo.executableDisplayName
        logger.trace("Checking Unity Process, name: $name")
        var execPathName = ""
        if (processInfo.executableCannonicalPath.isPresent)
            execPathName = Paths.get(processInfo.executableCannonicalPath.get()).fileName.toString() // for the case of symlink

        logger.trace("Checking Unity Process, execPathName: $execPathName")

        val result = (name.startsWith("Unity", true)
            || name.contains("Unity.app")
            || execPathName.equals("Unity", true))
            && !name.contains("UnityDebug")
            && !name.contains("UnityShader")
            && !name.contains("UnityHelper")
            && !name.contains("Unity Helper")
            && !name.contains("Unity Hub")
            && !name.contains("UnityCrashHandler")
            && !name.contains("UnityPackageManager")
            && !name.contains("Unity.Licensing.Client")
            && !name.contains("UnityDownloadAssistant")
            && !name.contains("unityhub", true)
        logger.trace("Checking Unity Process, result: $result")
        return result
    }

    fun isValidUnityEditorProcess(pid: Int, processList: Array<out ProcessInfo>): Boolean {
        logger.trace("Checking Unity Process, current pid: $pid")
        logger.trace("Checking Unity Process, processCount: ${processList.count()}")
        return processList.any { it.pid == pid && isUnityEditorProcess(it) }
    }

    fun getUnityProcessInfo(processInfo: ProcessInfo, project: Project): UnityProcessInfo? {
        return getAllUnityProcessInfo(listOf(processInfo), project)[processInfo.pid]
    }

    fun getAllUnityProcessInfo(processList: List<ProcessInfo>, project: Project): Map<Int, UnityProcessInfo> {
        // We might have to call external processes. Make sure we're running in the background
        assertNotDispatchThread()

        // We have several options to get the project name (and maybe directory, for future use):
        // 1) Match pid with EditorInstance.json - it's the current project
        // 2) If the editor was started from Unity Hub, use the -projectPath or -createProject parameters
        // 3) If we're on Mac/Linux, use `lsof -a -p {pid},{pid},{pid} -d cwd -Fn` to get the current working directory
        //    This is the project directory. (It's better performance to fetch these all at once)
        //    Getting current directory on Windows is painful and requires JNI to read process memory
        //    E.g. https://stackoverflow.com/questions/16110936/read-other-process-current-directory-in-c-sharp
        // 4) Scrape the main window title. This is fragile, as the format changes, and can easily break with hyphens in
        //    project or scene names. It also doesn't give us the project path. And it doesn't work on Mac/Linux
        val processInfoMap = mutableMapOf<Int, UnityProcessInfo>()

        processList.forEach {
            val projectName = getProjectNameFromEditorInstanceJson(it, project)
            parseProcessInfoFromCommandLine(it, projectName)?.let { n -> processInfoMap[it.pid] = n }
        }

        // If we failed to get project name from the command line, try and get it from the working directory
        if (processInfoMap.size != processList.size || processInfoMap.any { it.value.projectName == null }) {
            fillProjectNamesFromWorkingDirectory(processList, processInfoMap)
        }

        return processInfoMap
    }

    private fun assertNotDispatchThread() {
        val application = ApplicationManager.getApplication()
        if (application != null && application.isInternal) {
            if (application.isDispatchThread) {
                throw RuntimeException("Access not allowed on event dispatch thread")
            }
        }
    }

    private fun getProjectNameFromEditorInstanceJson(processInfo: ProcessInfo, project: Project): String? {
        val editorInstanceJson = EditorInstanceJson.getInstance(project)
        return if (editorInstanceJson.status == EditorInstanceJsonStatus.Valid && editorInstanceJson.contents?.process_id == processInfo.pid) {
            project.name
        } else null
    }

    private fun parseProcessInfoFromCommandLine(processInfo: ProcessInfo, canonicalProjectName: String?): UnityProcessInfo? {
        var projectName = canonicalProjectName
        var name: String? = null
        var umpProcessRole: String? = null
        var umpWindowTitle: String? = null

        val tokens = tokenizeCommandLine(processInfo)
        var i = 0
        while (i < tokens.size - 1) {   // -1 for the argument + argument value
            val token = tokens[i++]
            if (projectName == null && (token.equals("-projectPath", true) || token.equals("-createProject", true))) {
                // For an unquoted command line, the next token isn't guaranteed to be the whole path. If the path
                // contains a space-hyphen-char (e.g. `-projectPath /Users/matt/Projects/space game -is great -yeah`)
                // they will be split as multiple tokens. Concatenate subsequent tokens until we have the longest valid
                // path. Note that the arguments and values are all separated by a single space. Any other whitespace
                // is still part of the string

                var path = tokens[i++]
                var lastValid = if (File(path).isDirectory) path else ""
                var j = i
                while (j < tokens.size) {
                    path += " " + tokens[j++]
                    if (File(path).isDirectory) {
                        lastValid = path
                        i = j
                    }
                }

                projectName = getProjectNameFromPath(StringUtil.unquoteString(lastValid))
            }
            else if (token.equals("-name", true)) {
                name = StringUtil.unquoteString(tokens[i++])
            }
            else if (token.equals("-ump-process-role", true)) {
                umpProcessRole = StringUtil.unquoteString(tokens[i++])
            }
            else if (token.equals("-ump-window-title", true)) {
                umpWindowTitle = StringUtil.unquoteString(tokens[i++])
            }
        }

        if (projectName == null && name == null && umpWindowTitle == null && umpProcessRole == null) {
            return null
        }

        return UnityProcessInfo(projectName, name ?: umpWindowTitle ?: umpProcessRole)
    }

    private fun tokenizeCommandLine(processInfo: ProcessInfo): List<String> {
        return tokenizeQuotedCommandLine(processInfo)
            ?: tokenizeUnquotedCommandLine(processInfo)
    }

    private fun tokenizeQuotedCommandLine(processInfo: ProcessInfo): List<String>? {
        return getQuotedCommandLine(processInfo)?.let {
            val tokens = mutableListOf<String>()
            val tokenizer = CommandLineTokenizer(it)
            while(tokenizer.hasMoreTokens())
                tokens.add(tokenizer.nextToken())
            tokens
        }
    }

    private fun tokenizeUnquotedCommandLine(processInfo: ProcessInfo): List<String> {
        // Split the command line into arguments
        // We assume an argument starts with a hyphen and has no whitespace in the name. Empirically, this is true
        // So split on ^- or \s-
        // Each chunk should now be an arg and an argvalue, e.g. `-name Foo`
        // Split on the first whitespace. The argument value should be correct, but might require concatenating if
        // the value pathologically contains a \s-[^\s] sequence
        // E.g. `-createProject /Users/matt/my interesting -project` would split into the following tokens:
        // "-createProject" "/Users/matt/my interesting" "-project"
        // The single whitespace between arguments and between the argument and value is not captured, so must be added
        // back if concatenating
        val tokens = mutableListOf<String>()
        processInfo.commandLine.split("(^|\\s)(?=-[^\\s])".toRegex()).forEach {
            val whitespace = it.indexOf(' ')
            if (whitespace == -1) {
                tokens.add(it)
            }
            else {
                tokens.add(it.substring(0, whitespace))
                tokens.add(it.substring(whitespace + 1))
            }
        }
        return tokens
    }

    private fun getQuotedCommandLine(processInfo: ProcessInfo): String? {
        return when {
            SystemInfo.isWindows -> processInfo.commandLine // Already quoted correctly
            SystemInfo.isMac -> null    // We can't add quotes, and can't easily get an unquoted version
            SystemInfo.isUnix -> {
                try {
                    // ProcessListUtil.getProcessListOnUnix already reads /proc/{pid}/cmdline, but doesn't quote
                    // arguments that contain spaces. https://youtrack.jetbrains.com/issue/IDEA-229022
                    val procfsCmdline = File("/proc/${processInfo.pid}/cmdline")
                    val cmdlineString = String(FileUtil.loadFileBytes(procfsCmdline), StandardCharsets.UTF_8)
                    val cmdlineParts = StringUtil.split(cmdlineString, "\u0000")
                    return cmdlineParts.joinToString(" ") {
                        var s = it
                        if (!StringUtil.isQuotedString(s)) {
                            if (s.contains('\\')) {
                                s = StringUtil.escapeBackSlashes(s)
                            }
                            if (s.contains('\"')) {
                                s = StringUtil.escapeQuotes(s)
                            }
                            if (s.contains(' ')) {
                                s = StringUtil.wrapWithDoubleQuote(s)
                            }
                        }
                        return@joinToString s
                    }
                } catch (t: Throwable) {
                    logger.warn("Error while quoting command line: ${processInfo.commandLine}", t)
                }
                return null
            }
            else -> null
        }
    }

    private fun getProjectNameFromPath(projectPath: String): String = Paths.get(projectPath).fileName.toString()

    private fun fillProjectNamesFromWorkingDirectory(processList: List<ProcessInfo>, projectNames: MutableMap<Int, UnityProcessInfo>) {
        // Windows requires reading process memory. Unix is so much nicer.
        if (SystemInfo.isWindows) return

        try {
            val processIds = processList.joinToString(",") { it.pid.toString() }
            val command = when {
                SystemInfo.isMac -> "/usr/sbin/lsof"
                else -> "/usr/bin/lsof"
            }
            val output = ExecUtil.execAndGetOutput(GeneralCommandLine(command, "-a", "-p", processIds, "-d", "cwd", "-Fn"))
            if (output.exitCode == 0) {
                val stdout = output.stdoutLines

                // p{PID}
                // fcwd
                // n{CWD}
                for (i in 0 until stdout.size step 3) {
                    val pid = stdout[i].substring(1).toInt()
                    val cwd = getProjectNameFromPath(stdout[i + 2].substring(1))

                    projectNames[pid] = UnityProcessInfo(cwd, projectNames[pid]?.roleName)
                }
            }
        }
        catch (t: Throwable) {
            logger.warn("Error fetching current directory", t)
        }
    }

    fun isDebuggerAttached(host: String, port: Int, project: Project): Boolean {
        val debuggerManager = XDebuggerManager.getInstance(project)
        return debuggerManager.debugSessions.any {
            val profile = it.runProfile
            return if (profile is RemoteConfiguration) {
                // Note that this is best effort - host values must match exactly. "localhost" and "127.0.0.1" are not the same
                profile.address == host && profile.port == port
            }
            else false
        }
    }

    fun attachToLocalUnityProcess(pid: Int, project: Project) {
        val port = convertPidToDebuggerPort(pid)
        attachToUnityProcess("127.0.0.1", port, "Unity Editor", project, true)
    }

    fun attachToUnityProcess(host: String, port: Int, playerId: String, project: Project, isEditor: Boolean) {
        val configuration = UnityAttachProcessConfiguration(host, port, playerId, isEditor)
        val environment = ExecutionEnvironmentBuilder
            .create(project, DefaultDebugExecutor.getDebugExecutorInstance(), configuration)
            .build()
        ProgramRunnerUtil.executeConfiguration(environment, false, true)
    }
}
