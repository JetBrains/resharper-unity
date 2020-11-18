package com.jetbrains.rider.plugins.unity.util

import com.intellij.openapi.project.Project
import com.intellij.openapi.util.text.StringUtil
import com.intellij.util.Restarter

fun convertPidToDebuggerPort(port: Int) = convertPidToDebuggerPort(port.toLong())

fun convertPidToDebuggerPort(port: Long): Int {
    return (port % 1000).toInt() + 56000
}

fun addPlayModeArguments(args : MutableList<String>) {
    args.add("-executeMethod")
    args.add("JetBrains.Rider.Unity.Editor.StartUpMethodExecutor.EnterPlayMode")
}

fun getUnityArgs(project: Project):MutableList<String>
{
    val executable = UnityInstallationFinder.getInstance(project).getApplicationExecutablePath().toString()
    return mutableListOf<String>(executable)
}

fun MutableList<String>.withRiderPath() : MutableList<String> {
    val riderPath = Restarter.getIdeStarter()?.path
    if (riderPath != null) {
        this.addAll(mutableListOf("-riderPath", riderPath))
    }
    return this
}

fun MutableList<String>.withDebugCodeOptimization() : MutableList<String> {
    this.add("-debugCodeOptimization")
    return this
}

fun MutableList<String>.withProjectPath(project: Project) : MutableList<String> {
    this.addAll(mutableListOf("-projectPath", project.basePath.toString()))
    return this
}

fun MutableList<String>.toProgramParameters() : String {
    return StringUtil.join(this, "\n")
}