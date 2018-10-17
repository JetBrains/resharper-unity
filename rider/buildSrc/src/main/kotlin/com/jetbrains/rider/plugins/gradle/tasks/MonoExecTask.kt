package com.jetbrains.rider.plugins.gradle.tasks

import org.apache.tools.ant.taskdefs.condition.Os
import org.apache.tools.ant.taskdefs.condition.Os.FAMILY_WINDOWS
import org.gradle.api.tasks.AbstractExecTask
import org.gradle.api.tasks.TaskAction

open class MonoExecTask: AbstractExecTask<MonoExecTask>(MonoExecTask::class.java) {

    @TaskAction
    override fun exec() {
        if (!Os.isFamily(FAMILY_WINDOWS)) {
            val newCommandLine = commandLine
            newCommandLine.add(0, "mono")
            commandLine = newCommandLine
        }
        super.exec()
    }
}