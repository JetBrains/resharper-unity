package com.jetbrains.rider.plugins.unity.util

import com.intellij.openapi.project.Project
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
    val finder = UnityInstallationFinder.getInstance(project)
    val args = mutableListOf(finder.getApplicationPath().toString(), "-projectPath", project.basePath.toString(), "-debugCodeOptimization")
    val riderPath = Restarter.getIdeStarter()?.path
    if (riderPath!=null)
    {
        val originArgs = mutableListOf("-riderPath", riderPath)
        args.addAll(originArgs)
    }
    return args
}

fun getUnityWithProjectArgsAndDebugCodeOptimization(project: Project) : MutableList<String> {
    val args = getUnityWithProjectArgs(project)
    args.add("-debugCodeOptimization")
    return args
}