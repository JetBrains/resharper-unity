package com.jetbrains.rider.plugins.unity.run

import com.intellij.execution.configurations.CommandLineTokenizer
import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.process.ProcessInfo
import com.intellij.execution.util.ExecUtil
import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.NlsSafe
import com.intellij.openapi.util.SystemInfo
import com.intellij.openapi.util.io.FileUtil
import com.intellij.openapi.util.text.StringUtil
import com.intellij.xdebugger.XDebuggerManager
import com.jetbrains.rider.plugins.unity.util.EditorInstanceJson
import com.jetbrains.rider.plugins.unity.util.EditorInstanceJsonStatus
import com.jetbrains.rider.run.configurations.remote.RemoteConfiguration
import java.io.File
import java.nio.charset.StandardCharsets
import java.nio.file.Paths

/**
 * Simple data about a Unity process
 *
 * @param projectName   The name of the project the process is running
 * @param instanceName  A display name for the process. For an editor helper, this will be the role, such as `AssetImportWorker0`. For
 * virtual players, this will be the display name entered by the user, or the instance ID if not available.
 * @param instanceId    The instance ID of a virtual player, `null` otherwise. E.g. `mppmca3577a6`
 */
data class UnityLocalProcessExtraDetails(@NlsSafe val projectName: String?, @NlsSafe val instanceName: String?, @NlsSafe val instanceId: String?)

fun ProcessInfo.toUnityProcess(extraDetails: UnityLocalProcessExtraDetails?): UnityLocalProcess {
    return when {
        extraDetails?.instanceId != null -> {
            UnityVirtualPlayer(executableName, extraDetails.instanceName ?: extraDetails.instanceId,
                               extraDetails.instanceId, pid, extraDetails.projectName)
        }
        extraDetails?.instanceName != null -> UnityEditorHelper(executableName, extraDetails.instanceName, pid, extraDetails.projectName)
        else -> UnityEditor(executableName, pid, extraDetails?.projectName)
    }
}

object UnityRunUtil {
    private val logger = Logger.getInstance(UnityRunUtil::class.java)

    fun isUnityEditorProcess(processInfo: ProcessInfo): Boolean {
        val name = processInfo.executableDisplayName

        // For symlinks
        val canonicalName = if (processInfo.executableCannonicalPath.isPresent) {
            Paths.get(processInfo.executableCannonicalPath.get()).fileName.toString()
        }
        else name

        logger.debug("isUnityEditorProcess: '$name', '$canonicalName'")

        // Based on Unity's own VS Code debugger, we simply look for "Unity" or "Unity Editor". Java's
        // ProcessInfo#executableDisplayName is the executable name with `.exe` removed. This matches the behaviour of
        // .NET's Process.ProcessName
        // "Unity_s.debug" is a Linux only process that's packaged with Unity and contains some debug information. See RIDER-97262
        // https://github.com/Unity-Technologies/MonoDevelop.Debugger.Soft.Unity/blob/9f116ee5d344bce5888e838a75ded418bd7852c7/UnityProcessDiscovery.cs#L155
        return (name.equals("Unity", true)
                || name.equals("Unity Editor", true)
                || name.equals("Unity_s.debug", true)
                || canonicalName.equals("unity", true)
                || canonicalName.equals("Unity Editor", true)
                || canonicalName.equals("Unity_s.debug", true)
               )
    }

    fun isValidUnityEditorProcess(pid: Int, processList: Array<out ProcessInfo>): Boolean {
        logger.trace("Checking Unity Process, current pid: $pid. Process count: ${processList.size}")
        return processList.any { it.pid == pid && isUnityEditorProcess(it) }
    }

    fun getUnityProcessInfo(processInfo: ProcessInfo, project: Project): UnityLocalProcessExtraDetails? {
        return getAllUnityProcessInfo(listOf(processInfo), project)[processInfo.pid]
    }

    fun getAllUnityProcessInfo(processList: List<ProcessInfo>, project: Project): Map<Int, UnityLocalProcessExtraDetails> {
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
        val processInfoMap = mutableMapOf<Int, UnityLocalProcessExtraDetails>()

        processList.forEach {
            try {
                val projectName = getProjectNameFromEditorInstanceJson(it, project)
                parseProcessInfoFromCommandLine(it, projectName).let { n -> processInfoMap[it.pid] = n }
            }
            catch (t: Throwable) {
                logger.warn("Error fetching Unity process info: ${it.commandLine}", t)
            }
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
        }
        else null
    }

    private fun parseProcessInfoFromCommandLine(processInfo: ProcessInfo, canonicalProjectName: String?): UnityLocalProcessExtraDetails {
        var projectPath: String? = null
        var projectName = canonicalProjectName
        var name: String? = null
        var umpProcessRole: String? = null
        var umpWindowTitle: String? = null
        var vpId: String? = null

        val tokens = tokenizeCommandLine(processInfo)
        var i = 0
        while (i < tokens.size) {
            val token = tokens[i++]
            if (i < tokens.size - 1) {
                if (projectPath == null && (token.equals("-projectPath", true) || token.equals("-createProject", true))) {
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

                    projectPath = lastValid
                }
                else if (token.equals("-name", true) && i < tokens.size - 1) {
                    name = StringUtil.unquoteString(tokens[i++])
                }
                else if (token.equals("-ump-process-role", true) && i < tokens.size - 1) {
                    umpProcessRole = StringUtil.unquoteString(tokens[i++])
                }
                else if (token.equals("-ump-window-title", true) && i < tokens.size - 1) {
                    umpWindowTitle = StringUtil.unquoteString(tokens[i++])
                }
            }
            else {
                // For virtual players, we could also look at `-editor-mode`, `--virtual-project-clone`, `-mainProcessId` and possibly
                // `-library-redirect` and `-readonly`, but we don't need the information, they would just be indicators that this is a
                // virtual player
                if (token.startsWith("-vpId=", true)) {
                    vpId = token.substringAfter("=")
                }
            }
        }

        if (projectPath != null) {
            projectName = getMainProjectNameFromPath(StringUtil.unquoteString(projectPath), vpId)
        }

        return UnityLocalProcessExtraDetails(projectName, name ?: umpWindowTitle ?: umpProcessRole, vpId)
    }

    private fun tokenizeCommandLine(processInfo: ProcessInfo): List<String> {
        return tokenizeQuotedCommandLine(processInfo)
               ?: tokenizeUnquotedCommandLine(processInfo)
    }

    private fun tokenizeQuotedCommandLine(processInfo: ProcessInfo): List<String>? {
        return getQuotedCommandLine(processInfo)?.let {
            val tokens = mutableListOf<String>()
            val tokenizer = CommandLineTokenizer(it)
            while (tokenizer.hasMoreTokens())
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
                }
                catch (t: Throwable) {
                    logger.warn("Error while quoting command line: ${processInfo.commandLine}", t)
                }
                return null
            }
            else -> null
        }
    }

    private fun getMainProjectNameFromPath(projectPath: String, virtualPlayerId: String?): String {
        val path = if (virtualPlayerId != null) {
            projectPath.removeSuffix("/").removeSuffix("Library/VP/$virtualPlayerId")
        }
        else projectPath
        return Paths.get(path).fileName.toString()
    }

    private fun fillProjectNamesFromWorkingDirectory(processList: List<ProcessInfo>,
                                                     projectNames: MutableMap<Int, UnityLocalProcessExtraDetails>) {
        // Windows requires reading process memory. Unix is so much nicer.
        if (SystemInfo.isWindows) return

        try {
            val processIds = processList.filter { !projectNames.containsKey(it.pid) }.joinToString(",") { it.pid.toString() }
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
                    val cwd = stdout[i + 2].substring(1)
                    val projectName = getMainProjectNameFromPath(cwd, null)

                    // We might have found the project name for this process from the command line. Even if we had, the
                    // working directory should be the same as the command line project name
                    val process = projectNames[pid]
                    projectNames[pid] = process?.copy(projectName = projectName) ?: UnityLocalProcessExtraDetails(projectName, null, null)
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
}
