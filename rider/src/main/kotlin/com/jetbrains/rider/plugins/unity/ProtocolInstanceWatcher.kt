package com.jetbrains.rider.plugins.unity

import com.intellij.openapi.project.Project
import com.jetbrains.rd.platform.util.application
import com.jetbrains.rd.util.reactive.whenTrue
import com.jetbrains.rdclient.util.idea.LifetimedProjectComponent
import com.jetbrains.rider.isUnityProject
import com.jetbrains.rider.model.RdDelta
import com.jetbrains.rider.model.RdDeltaBatch
import com.jetbrains.rider.model.RdDeltaType
import com.jetbrains.rider.model.fileSystemModel
import com.jetbrains.rider.projectView.solution
import java.nio.file.FileSystems
import java.nio.file.Path
import java.nio.file.Paths
import java.nio.file.StandardWatchEventKinds.ENTRY_CREATE
import java.nio.file.StandardWatchEventKinds.ENTRY_MODIFY
import java.nio.file.WatchService


class ProtocolInstanceWatcher(project: Project) : LifetimedProjectComponent(project) {
    init {
        if (project.isUnityProject()) {
            project.solution.isLoaded.whenTrue(componentLifetime) {
                application.executeOnPooledThread {
                    val watchService: WatchService = FileSystems.getDefault().newWatchService()
                    val libraryPath: Path = Paths.get(project.basePath!!, "Library")

                    libraryPath.register(watchService, ENTRY_MODIFY)
                    var poll: Boolean
                    it.bracket(opening = {
                        poll = true

                        val watchedFileName = "ProtocolInstance.json"
                        val delta = RdDelta(libraryPath.resolve(watchedFileName).toString(), RdDeltaType.Changed)
                        while (poll) {
                            val key = watchService.take()
                            for (event in key.pollEvents()) {
                                if (event.context().toString() == watchedFileName) {
                                    application.invokeLater {
                                        project.solution.fileSystemModel.change.fire(RdDeltaBatch(listOf(delta)))
                                    }
                                }
                            }
                            poll = key.reset()
                        }
                    }, terminationAction = {
                        poll = false
                    })
                }
            }
        }
    }
}