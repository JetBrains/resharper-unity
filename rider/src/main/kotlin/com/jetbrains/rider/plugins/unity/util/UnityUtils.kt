package com.jetbrains.rider.plugins.unity.util

import com.intellij.openapi.project.Project
import com.intellij.util.Restarter
import com.intellij.util.execution.ParametersListUtil
import com.jetbrains.rider.projectView.solutionDirectory

fun convertPidToDebuggerPort(port: Int) = convertPidToDebuggerPort(port.toLong())

fun convertPidToDebuggerPort(port: Long): Int {
    return (port % 1000).toInt() + 56000
}

fun addPlayModeArguments(args : MutableList<String>) {
    args.add("-executeMethod")
    args.add("JetBrains.Rider.Unity.Editor.StartUpMethodExecutor.EnterPlayMode")
}

fun getUnityArgs(project: Project):MutableList<String> {
    val executable = UnityInstallationFinder.getInstance(project).getApplicationExecutablePath().toString()
    return mutableListOf(executable)
}

fun MutableList<String>.withRiderPath() : MutableList<String> {
    val riderPath = Restarter.getIdeStarter()?.toFile()?.canonicalPath
    if (riderPath != null) {
        this.addAll(mutableListOf("-riderPath", riderPath))
    }
    return this
}

/**
 * Undocumented commandline argument, which forces Unity to enable `Debug Code Optimization`, even if it has `Release Code Optimization` in its settings.
 */
fun MutableList<String>.withDebugCodeOptimization() : MutableList<String> {
    this.add("-debugCodeOptimization")
    return this
}

fun MutableList<String>.withProjectPath(project: Project) : MutableList<String> {
    this.addAll(mutableListOf("-projectPath", project.solutionDirectory.canonicalPath))
    return this
}

fun MutableList<String>.withProjectPath(projectPath: String) : MutableList<String> {
    this.addAll(mutableListOf("-projectPath", projectPath))
    return this
}

fun MutableList<String>.withBatchMode(): MutableList<String> {
    this.add("-batchmode")
    return this
}

fun MutableList<String>.withRunTests(): MutableList<String> {
    this.add("-runTests")
    return this
}

fun MutableList<String>.withTestResults(): MutableList<String> {
    this.addAll(listOf("-testResults", "Logs/results.xml"))
    return this
}

fun MutableList<String>.withTestPlatform() : MutableList<String> {
    this.addAll(listOf("-testPlatform", "EditMode"))
    return this
}

fun MutableList<String>.toProgramParameters() : String {
    return ParametersListUtil.join(this)
}