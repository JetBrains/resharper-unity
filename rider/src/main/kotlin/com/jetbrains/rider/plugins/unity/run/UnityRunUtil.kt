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

    fun getUnityProcessProjectName(processInfo: ProcessInfo, project: Project): String? {
        return getUnityProcessProjectNames(listOf(processInfo), project)[processInfo.pid]
    }

    fun getUnityProcessProjectNames(processList: List<ProcessInfo>, project: Project): Map<Int, String> {
        // We have several options to get the project name (and directory, for future use):
        // 1) Match pid with EditorInstance.json - it's the current project
        // 2) If the editor was started from Unity Hub, use the -projectPath or -createProject parameters
        // 3) If we're on Mac/Linux, use `lsof -a -p {pid},{pid},{pid} -d cwd -Fn` to get the current working directory
        //    This is the project directory. (It's better performance to fetch these all at once)
        //    Getting current directory on Windows is painful and requires JNI to read process memory
        //    E.g. https://stackoverflow.com/questions/16110936/read-other-process-current-directory-in-c-sharp
        // 4) Scrape the main window title. This is fragile, as the format changes, and can easily break with hyphens in
        //    project or scene names. It also doesn't give us the project path. And it doesn't work on Mac/Linux

        // We might have to call external processes. Make sure we're running in the background
        assertNotDispatchThread()
        val projectNames = mutableMapOf<Int, String>()

        processList.forEach {
            val projectName = getProjectNameFromEditorInstanceJson(it, project) ?:
                getProjectNameFromCommandLine(it)
            projectName?.let { name -> projectNames[it.pid] = name }
        }

        if (projectNames.size != processList.size) {
            fillProjectNamesFromWorkingDirectory(processList, projectNames)
        }

        return projectNames
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

    private fun getProjectNameFromCommandLine(processInfo: ProcessInfo): String? {
        // Make sure the command line we're using is properly quoted, if possible
        getQuotedCommandLine(processInfo)?.let {
            val tokenizer = CommandLineTokenizer(processInfo.commandLine)
            while (tokenizer.hasMoreTokens()) {
                val token = tokenizer.nextToken()
                if (token.equals("-projectPath", true) || token.equals("-createPath", true)) {
                    return getProjectNameFromPath(StringUtil.unescapeStringCharacters(tokenizer.nextToken()))
                }
            }
        }

        // Try to parse the unquoted command line, coping with a -projectPath or -createPath that might contain spaces
        // and/or hyphens. Split the command line at argument boundaries, e.g. a hyphen followed by a non-whitespace
        // char, with leading whitespace, or at the start of the string. Each split string should be an arg and an
        // argvalue (lookahead means we keep the delimiter). If the path contains an embedded space-hyphen-nonspace
        // sequence, we need to join segments until we're sure we've got the longest possible path.
        // There is a pathological edge case of two directories with the same name but one with a suffix that matches
        // the next command line argument. If we take the longest path, we can get the wrong one. E.g.
        // "/home/Unity/project1" and "/home/Unity/project1 -useHub" would confuse things. I think this is so unlikely
        // as to be happily ignored
        // This assumes that all arguments begin with a hyphen, and there are no standalone arguments. Empirically, this
        // is true
        val commandLineArgs = processInfo.commandLine.split("(^|\\s)(?=-[^\\s])".toRegex())
        var i = 0
        do {
            if (commandLineArgs[i].startsWith("-projectPath", ignoreCase = true)
                || commandLineArgs[i].startsWith("-createProject", ignoreCase = true)) {
                val whitespace = commandLineArgs[i].indexOf(' ')
                if (whitespace == -1) continue   // Weird if true
                var path = commandLineArgs[i].substring(whitespace + 1)

                var lastValid = if (File(path).isDirectory) path else ""
                while (i < commandLineArgs.size - 1) {
                    path += " " + commandLineArgs[++i]
                    if (File(path).isDirectory) {
                        lastValid = path
                    }
                }

                return getProjectNameFromPath(lastValid)
            }

            i++
        } while (i < commandLineArgs.size)

        return null
    }

    private fun getQuotedCommandLine(processInfo: ProcessInfo): String? {
        return when {
            SystemInfo.isWindows -> processInfo.commandLine
            SystemInfo.isMac -> null
            SystemInfo.isUnix -> {
                try {
                    // ProcessListUtil.getProcessListOnUnix already reads /proc/{pid}/cmdline, but doesn't quote
                    // arguments that contain spaces, which makes it much harder to parse
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

    private fun fillProjectNamesFromWorkingDirectory(processList: List<ProcessInfo>, projectNames: MutableMap<Int, String>) {
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

                    projectNames[pid] = cwd
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