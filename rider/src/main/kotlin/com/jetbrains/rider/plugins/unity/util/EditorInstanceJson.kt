package com.jetbrains.rider.plugins.unity.util

import com.google.gson.Gson
import com.intellij.execution.process.ProcessInfo
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.Key
import com.intellij.openapi.vfs.*
import com.jetbrains.rider.plugins.unity.run.UnityRunUtil
import com.jetbrains.rider.projectDir
import java.io.FileReader
import java.io.IOException
import java.nio.file.Paths

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
        private val LISTENER_KEY: Key<VirtualFileListener> = Key("Unity::EditorInstanceJson::Listener")

        fun getInstance(project: Project): EditorInstanceJson {
            addFileListener(project)

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
            // Canonical path will always be true for a Rider project
            val file = Paths.get(project.projectDir.canonicalPath!!, "Library/EditorInstance.json").toFile()
            if (!file.exists()) {
                return empty(EditorInstanceJsonStatus.Missing)
            }

            return try {
                val contents = Gson().fromJson(FileReader(file), EditorInstanceJsonContents::class.java)
                EditorInstanceJson(EditorInstanceJsonStatus.Valid, contents)
            } catch (e: IOException) {
                logger.error("Error reading EditorInstance.json", e)
                empty(EditorInstanceJsonStatus.Error)
            } catch (t: Throwable) {
                logger.error("Error parsing EditorInstance.json", t)
                empty(EditorInstanceJsonStatus.Error)
            }
        }

        private fun empty(status: EditorInstanceJsonStatus) = EditorInstanceJson(status, null)

        private fun addFileListener(project: Project) {

            var listener = project.getUserData(LISTENER_KEY)
            if (listener == null) {
                listener = object: VirtualFileListener {
                    override fun contentsChanged(event: VirtualFileEvent) = resetEditorInstanceJson(event)
                    override fun fileCreated(event: VirtualFileEvent) = resetEditorInstanceJson(event)
                    override fun fileDeleted(event: VirtualFileEvent) = resetEditorInstanceJson(event)

                    private fun resetEditorInstanceJson(event: VirtualFileEvent) {
                        if (isEditorInstanceJson(event.file)) {
                            project.putUserData(INSTANCE_KEY, null)
                        }
                    }

                    private fun isEditorInstanceJson(file: VirtualFile?): Boolean {
                        return file != null && file.name == "EditorInstance.json" && isLibraryFolder(file.parent)
                    }

                    private fun isLibraryFolder(file: VirtualFile?): Boolean {
                        return file != null && file.name == "Library" && file.parent == project.projectDir
                    }
                }

                VirtualFileManager.getInstance().addVirtualFileListener(listener, project)

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
