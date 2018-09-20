package com.jetbrains.rider.plugins.unity.util

import com.google.gson.Gson
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VfsUtil
import java.io.FileNotFoundException

enum class EditorInstanceJsonStatus {
    Missing,
    Error,
    Outdated,
    Valid
}

data class EditorInstanceJson(val process_id: Int, val version: String) {

    companion object {
        private val logger = Logger.getInstance(EditorInstanceJson::class.java)

        fun load(project: Project): Pair<EditorInstanceJsonStatus, EditorInstanceJson?> {

            // Read from VFS, so we can make use of caching, but make sure it's up to date
            val file = project.refreshAndFindFile("Library/EditorInstance.json")
            return if (file == null || !file.exists()) {
                Pair(EditorInstanceJsonStatus.Missing, null)
            }
            else {
                val gson = Gson()
                try {
                    val text = VfsUtil.loadText(file)
                    val json = gson.fromJson(text, EditorInstanceJson::class.java)
                    val status = if (json == null) {
                        logger.error("Empty EditorInstance.json")
                        EditorInstanceJsonStatus.Error
                    }
                    else
                        EditorInstanceJsonStatus.Valid
                    Pair(status, json)
                }
                catch (e: FileNotFoundException) {
                    logger.error("EditorInstance.json missing, after VFS exists check passed? Continuing as though missing", e)
                    Pair(EditorInstanceJsonStatus.Missing, null)
                }
                catch (t: Throwable) {
                    logger.error("Error loading EditorInstance.json. Continuing as though missing.", t)
                    Pair(EditorInstanceJsonStatus.Error, null)
                }
            }
        }
    }
}