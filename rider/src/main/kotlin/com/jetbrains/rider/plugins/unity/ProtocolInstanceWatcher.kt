package com.jetbrains.rider.plugins.unity

import com.intellij.openapi.application.EDT
import com.intellij.openapi.project.Project
import com.intellij.openapi.startup.ProjectActivity
import com.intellij.util.application
import com.jetbrains.rd.util.lifetime.isAlive
import com.jetbrains.rd.util.reactive.whenTrue
import com.jetbrains.rider.model.RdDelta
import com.jetbrains.rider.model.RdDeltaBatch
import com.jetbrains.rider.model.RdDeltaType
import com.jetbrains.rider.model.fileSystemModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.projectView.solutionDirectory
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.nio.file.ClosedWatchServiceException
import java.nio.file.FileSystems
import java.nio.file.StandardWatchEventKinds.ENTRY_CREATE
import java.nio.file.StandardWatchEventKinds.ENTRY_MODIFY
import java.nio.file.WatchKey
import java.nio.file.WatchService
import kotlin.concurrent.thread
import kotlin.io.path.isDirectory


class ProtocolInstanceWatcher : ProjectActivity {
    override suspend fun execute(project: Project) {
        withContext(Dispatchers.EDT) {
            val lifetime = UnityProjectLifetimeService.getLifetime(project)
            project.solution.isLoaded.whenTrue(lifetime) { lt ->
                if (project.isUnityProject.getCompletedOr(false)) {
                    thread(name = "ProtocolInstanceWatcher") {
                        val watchService: WatchService = FileSystems.getDefault().newWatchService()
                        val libraryPath = project.solutionDirectory.resolve("Library").toPath()

                        if (!(libraryPath.isDirectory())) // todo: rethink, see com.jetbrains.rider.UnityProjectDiscoverer.Companion.hasUnityFileStructure
                            return@thread

                        libraryPath.register(watchService, ENTRY_CREATE, ENTRY_MODIFY)

                        lt.onTerminationIfAlive {
                            watchService.close() // releases watchService.take()
                        }

                        val watchedFileName = "ProtocolInstance.json"
                        val delta = RdDelta(libraryPath.resolve(watchedFileName).toString(), RdDeltaType.Changed)
                        var key: WatchKey
                        try {
                            while (watchService.take().also { watchKey -> key = watchKey } != null && lt.isAlive) {
                                for (event in key.pollEvents()) {
                                    val context = event.context() ?: continue
                                    if (context.toString() == watchedFileName) {
                                        application.invokeLater {
                                            project.solution.fileSystemModel.change.fire(RdDeltaBatch(listOf(delta)))
                                        }
                                    }
                                }
                                key.reset()
                            }
                        }
                        catch (_: ClosedWatchServiceException) {
                        } // this is expected on `watchService.close()`
                    }
                }
            }
        }
    }
}