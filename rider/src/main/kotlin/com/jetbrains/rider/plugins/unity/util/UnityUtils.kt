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

fun getUnityWithProjectArgs(project: Project) : MutableList<String> {
    val executable = UnityInstallationFinder.getInstance(project).getApplicationExecutablePath().toString()
    val args = mutableListOf(executable)
    args.addAll(getProjectArgs(project))
    withRiderPath(args)
    return args
}

private fun withRiderPath(args: MutableList<String>) {
    val riderPath = Restarter.getIdeStarter()?.path
    if (riderPath != null) {
        args.addAll(mutableListOf("-riderPath", riderPath))
    }
}

fun getUnityWithProjectArgsAndDebugCodeOptimization(project: Project) : MutableList<String> {
    val args = getUnityWithProjectArgs(project)
    args.add("-debugCodeOptimization")
    return args
}

fun getProjectArgs(project: Project) : MutableList<String> {
    val args = mutableListOf("-projectPath", project.basePath.toString())
    return args
}

private fun getProjectArgsAndDebugCodeOptimization(project: Project) : MutableList<String> {
    val args = getProjectArgs(project)
    args.add("-debugCodeOptimization")
    return args
}

fun getRawProjectArgsAndDebugCodeOptimization(project: Project) : String {
    return StringUtil.join(getProjectArgsAndDebugCodeOptimization(project), "\n")
}