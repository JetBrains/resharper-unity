package com.jetbrains.rider.plugins.unity.util

import com.google.gson.Gson
import com.intellij.execution.process.ProcessInfo
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.Key
import com.intellij.openapi.vfs.AsyncFileListener
import com.intellij.openapi.vfs.AsyncFileListener.ChangeApplier
import com.intellij.openapi.vfs.VirtualFileManager
import com.intellij.openapi.vfs.newvfs.events.VFileEvent
import com.jetbrains.rider.plugins.unity.run.UnityRunUtil
import com.jetbrains.rider.projectView.solutionDirectory
import java.io.File
import java.io.FileReader
import java.io.IOException

enum class EditorInstanceJsonStatus {
    Missing,
    Error,
    Outdated,
    Valid
}

data class EditorInstanceJson(val status: EditorInstanceJsonStatus, val contents: EditorInstanceJsonContents?) {
    companion object {
        private val logger = Logger.getInstance(EditorInstanceJson::class.java)
        private val INSTANCE_KEY: Key<EditorInstanceJson> = Key("Unity::EditorInstanceJson")
        private val LISTENER_KEY: Key<AsyncFileListener> = Key("Unity::EditorInstanceJson::Listener")
        private const val editorInstanceJsonRelPath = "Library/EditorInstance.json"

        fun getInstance(project: Project): EditorInstanceJson {
            initFileListener(project)

            var editorInstanceJson = project.getUserData(INSTANCE_KEY)
            if (editorInstanceJson != null)
                return editorInstanceJson

            editorInstanceJson = load(project)
            if (editorInstanceJson.status != EditorInstanceJsonStatus.Valid) {
                project.putUserData(INSTANCE_KEY, editorInstanceJson)
            }
            return editorInstanceJson
        }

        private fun load(project: Project): EditorInstanceJson {
            if (project.isDefault) // RIDER-51997 RunConfiguration templates from Welcome screen
                return empty(EditorInstanceJsonStatus.Missing)

            // Canonical path will always be true for a Rider project
            val file = project.solutionDirectory.resolve(editorInstanceJsonRelPath)
            if (!file.exists()) {
                return empty(EditorInstanceJsonStatus.Missing)
            }

            return try {
                FileReader(file).use {
                    val contents = Gson().fromJson(it, EditorInstanceJsonContents::class.java)
                    EditorInstanceJson(EditorInstanceJsonStatus.Valid, contents)
                }
            } catch (e: IOException) {
                logger.error("Error reading EditorInstance.json", e)
                empty(EditorInstanceJsonStatus.Error)
            } catch (t: Throwable) {
                logger.error("Error parsing EditorInstance.json", t)
                empty(EditorInstanceJsonStatus.Error)
            }
        }

        private fun empty(status: EditorInstanceJsonStatus) = EditorInstanceJson(status, null)

        private fun initFileListener(project: Project) {
            val fullEditorInstancePath = project.solutionDirectory.resolve(editorInstanceJsonRelPath)

            var listener = project.getUserData(LISTENER_KEY)
            if (listener == null) {
                listener = object: AsyncFileListener {
                    override fun prepareChange(events: MutableList<out VFileEvent>): ChangeApplier? {
                        if (events.any { isEditorInstanceJson(it.path) }) {
                            return object: ChangeApplier {
                                override fun afterVfsChange() = project.putUserData(INSTANCE_KEY, null)
                            }
                        }

                        return null
                    }

                    private fun isEditorInstanceJson(path: String): Boolean {
                        return fullEditorInstancePath == File(path)
                    }
                }

                VirtualFileManager.getInstance().addAsyncFileListener(listener, project)

                project.putUserData(LISTENER_KEY, listener)
            }
        }
    }

    fun validateStatus(processList: Array<out ProcessInfo>): EditorInstanceJsonStatus {
        if (status == EditorInstanceJsonStatus.Valid && contents != null
            && !UnityRunUtil.isValidUnityEditorProcess(contents.process_id, processList)) {
            return EditorInstanceJsonStatus.Outdated
        }
        return status
    }

    data class EditorInstanceJsonContents(val process_id: Int, val version: String)
}
