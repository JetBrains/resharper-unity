package com.jetbrains.rider.plugins.unity

import com.intellij.openapi.project.Project
import com.intellij.util.io.exists
import com.intellij.util.io.isDirectory
import com.jetbrains.rd.platform.util.application
import com.jetbrains.rd.util.lifetime.isAlive
import com.jetbrains.rd.util.reactive.whenTrue
import com.jetbrains.rdclient.util.idea.LifetimedProjectComponent
import com.jetbrains.rider.isUnityProject
import com.jetbrains.rider.model.RdDelta
import com.jetbrains.rider.model.RdDeltaBatch
import com.jetbrains.rider.model.RdDeltaType
import com.jetbrains.rider.model.fileSystemModel
import com.jetbrains.rider.projectView.solution
import java.nio.file.*
import java.nio.file.StandardWatchEventKinds.ENTRY_MODIFY
import kotlin.concurrent.thread


class ProtocolInstanceWatcher(project: Project) : LifetimedProjectComponent(project) {
    init {
        if (project.isUnityProject()) {
            project.solution.isLoaded.whenTrue(componentLifetime) {
                thread(name = "ProtocolInstanceWatcher") {
                    val watchService: WatchService = FileSystems.getDefault().newWatchService()
                    val libraryPath: Path = Paths.get(project.basePath!!, "Library")

                    if (!(libraryPath.exists() && libraryPath.isDirectory())) // todo: rethink, see com.jetbrains.rider.UnityProjectDiscoverer.Companion.hasUnityFileStructure
                        return@thread

                    libraryPath.register(watchService, ENTRY_MODIFY)

                    it.onTerminationIfAlive {
                        watchService.close() // releases watchService.take()
                    }

                    val watchedFileName = "ProtocolInstance.json"
                    val delta = RdDelta(libraryPath.resolve(watchedFileName).toString(), RdDeltaType.Changed)
                    var key: WatchKey
                    try {
                        while (watchService.take().also { key = it } != null && it.isAlive) {
                            for (event in key.pollEvents()) {
                                if (event.context().toString() == watchedFileName) {
                                    application.invokeLater {
                                        project.solution.fileSystemModel.change.fire(RdDeltaBatch(listOf(delta)))
                                    }
                                }
                            }
                            key.reset()
                        }
                    }
                    catch (ClosedWatchServiceException){} // this is expected on `watchService.close()`

                }
            }
        }
    }
}