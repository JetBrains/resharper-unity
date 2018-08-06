package com.jetbrains.rider.plugins.unity.util

import com.google.gson.Gson
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project

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

            val path = project.baseDir.findFileByRelativePath("Library/EditorInstance.json")
            return if (path == null || !path.exists()) {
                Pair(EditorInstanceJsonStatus.Missing, null)
            }
            else {
                val gson = Gson()
                try {
                    path.inputStream.reader().use {
                        Pair(EditorInstanceJsonStatus.Valid, gson.fromJson(it, EditorInstanceJson::class.java))
                    }
                }
                catch (t: Throwable) {
                    logger.error("Error loading EditorInstance.json", t)
                    Pair(EditorInstanceJsonStatus.Error, null)
                }
            }
        }
    }
}