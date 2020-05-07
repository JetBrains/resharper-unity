package com.jetbrains.rider.plugins.unity

import com.intellij.openapi.project.Project
import com.jetbrains.rd.util.reactive.whenTrue
import com.jetbrains.rdclient.util.idea.LifetimedProjectComponent
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.projectView.solution
import java.nio.file.FileSystems
import java.nio.file.Path
import java.nio.file.Paths
import java.nio.file.StandardWatchEventKinds.ENTRY_CREATE
import java.nio.file.WatchService


class ProtocolInstanceWatcher(project: Project) : LifetimedProjectComponent(project) {
    init {
        project.solution.rdUnityModel.hasUnityReference.whenTrue(componentLifetime) {
            val watchService: WatchService = FileSystems.getDefault().newWatchService()
            val path: Path = Paths.get(project.basePath, "Library")

            path.register(watchService, ENTRY_CREATE)
            var poll: Boolean
            componentLifetime.bracket(opening = {
                poll = true
                while (poll) {
                    val key = watchService.take()
                    for (event in key.pollEvents()) {
                        if (event.context() == path.resolve("ProtocolInstance.json"))
                            println("Event kind : " + event.kind() + " - File : " + event.context())
                    }
                    poll = key.reset()
                }
            }, terminationAction = {
                poll = false
            })
        }
    }
}