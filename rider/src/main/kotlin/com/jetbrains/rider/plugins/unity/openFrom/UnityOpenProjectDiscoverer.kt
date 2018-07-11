package com.jetbrains.rider.plugins.unity.openFrom

import com.google.gson.Gson
import com.intellij.openapi.application.ApplicationManager
import com.jetbrains.rider.diagnostics.doActivity
import com.jetbrains.rider.plugins.unity.restClient.ProjectState
import com.jetbrains.rider.util.idea.getLogger
import java.io.InputStreamReader
import java.net.ConnectException
import java.net.URL
import java.nio.charset.Charset

class UnityOpenProjectDiscoverer {
    private val logger = getLogger<UnityOpenProjectDiscoverer>()

    fun start(onFound: (project: OpenUnityProject) -> Unit, onDone: () -> Unit) {
        ApplicationManager.getApplication().executeOnPooledThread {

            // 38000 is just a magic number. Each instance of the Unity editor will try to listen on 38000, and if it's
            // already in use, will increment and try again. Ideally, we'd just start at 38000 and go until we had our
            // first failure, but that doesn't handle the case where two instances are started and the first is closed.
            // Let's limit to 10 instances
            for (port in 38000..38010) {

                val url = "http://localhost:$port/unity/projectstate"
                logger.doActivity("GET $url") {
                    try {
                        // Note that we don't cancel anything here. If we cancel and close sockets, it can crash Unity
                        URL(url).openStream().use {
                            val reader = InputStreamReader(it, Charset.forName("UTF-8"))
                            val gson = Gson()
                            val projectState = gson.fromJson<ProjectState>(reader, ProjectState::class.java)
                            onFound(OpenUnityProject(port, projectState))
                        }
                    } catch (e: ConnectException) {
                        // Most likely connection refused because there's no-one listening. Ignore and continue
                    }
                    // TODO: Best way to handle error? Might need to tell user to re-import assets
                }
            }

            onDone()
        }
    }
}