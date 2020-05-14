package com.jetbrains.rider.plugins.unity.diff

import com.intellij.diff.contents.DiffContent
import com.intellij.diff.contents.FileContent
import com.intellij.diff.merge.MergeRequest
import com.intellij.diff.merge.ThreesideMergeRequest
import com.intellij.diff.tools.external.ExternalDiffSettings
import com.intellij.diff.tools.external.ExternalDiffToolUtil
import com.intellij.execution.ExecutionException
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import java.io.IOException

class UnityYamlAutomaticExternalMergeTool: AutomaticExternalMergeTool {
    private val LOG =
        Logger.getInstance(UnityYamlAutomaticExternalMergeTool::class.java)

    @Throws(ExecutionException::class, IOException::class)
    override fun showRequest(project: Project?, request: MergeRequest) {
        val settings = ExternalDiffSettings()
        settings.isMergeTrustExitCode = false
        settings.mergeExePath = "/Applications/Unity/Hub/Editor/2020.1.0b8/Unity.app/Contents/Tools/UnityYAMLMerge"
        settings.mergeParameters = "merge -p %3 %2 %1 %4"
        ExternalDiffToolUtil.executeMerge(
            project,
            settings,
            request as ThreesideMergeRequest
        )
    }

    override fun canShow(request: MergeRequest): Boolean {
        if (request is ThreesideMergeRequest) {
            val outputContent = request.outputContent
            if (!canProcessOutputContent(outputContent)) return false
            val contents = request.contents
            if (contents.size != 3) return false
            for (content in contents) {
                if (!ExternalDiffToolUtil.canCreateFile(content!!)) return false
            }
            return true
        }
        return false
    }

    private fun canProcessOutputContent(content: DiffContent): Boolean {
        return content is FileContent && content.file.isInLocalFileSystem
            && (content.file.extension =="unity" || content.file.extension == "prefab")
    }
}