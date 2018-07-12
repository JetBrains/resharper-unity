package com.jetbrains.rider.plugins.unity.openFrom

import com.google.gson.Gson
import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.util.SystemInfo
import com.jetbrains.rider.diagnostics.doActivity
import com.jetbrains.rider.plugins.unity.restClient.ProjectState
import com.jetbrains.rider.util.idea.getLogger
import java.io.InputStreamReader
import java.net.*
import java.nio.charset.Charset

class UnityOpenProjectDiscoverer {
    private val logger = getLogger<UnityOpenProjectDiscoverer>()

    fun start(onFound: (project: OpenUnityProject) -> Unit, onDone: () -> Unit) {
        ApplicationManager.getApplication().executeOnPooledThread {

            // Note that this requires Unity 5.3.0+
            val gson = Gson()
            try {
                // 38000 is just a magic number. Each instance of the Unity editor will try to listen on 38000, and if it's
                // already in use, will increment and try again. Ideally, we'd just start at 38000 and go until we had our
                // first failure, but that doesn't handle the case where two instances are started and the first is closed.
                // Let's limit to 10 instances
                for (port in 38000..38010) {

                    val url = "http://127.0.0.1:$port/unity/projectstate"
                    logger.doActivity("GET $url") {
                        try {
                            // Note that we don't cancel anything here. If we cancel and close sockets, it can crash Unity
                            val openConnection = URL(url).openConnection()

                            // Set a connection timeout. This is required for Windows, which takes about 1 second to
                            // report connection refused. The best I can find on google is that Windows tries to connect
                            // to IPv6 localhost first, but we're specifying 127.0.0.1, which should mean v4 only.
                            // But this delay is really noticeable, especially since we try a range of ports.
                            if (SystemInfo.isWindows)
                                openConnection.connectTimeout = 200

                            openConnection.inputStream.use {
                                val reader = InputStreamReader(it, Charset.forName("UTF-8"))
                                val projectState = gson.fromJson<ProjectState>(reader, ProjectState::class.java)
                                onFound(OpenUnityProject(port, projectState))
                            }
                        } catch (e: ConnectException) {
                            // Most likely connection refused because there's no-one listening. Ignore and continue
                            println()
                        } catch (e: SocketTimeoutException) {
                            // Again, we don't care if it times out. This is likely on Windows, see above
                        }
                        // TODO: Best way to handle error? Might need to tell user to re-import assets
                    }
                }
            }
            finally {
                onDone()
            }
        }
    }
}