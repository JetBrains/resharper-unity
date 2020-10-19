package com.jetbrains.rider.plugins.unity.diff

import com.intellij.diff.DiffManagerEx
import com.intellij.diff.DiffRequestFactory
import com.intellij.diff.contents.DiffContent
import com.intellij.diff.contents.FileContent
import com.intellij.diff.merge.MergeCallback
import com.intellij.diff.merge.MergeRequest
import com.intellij.diff.merge.ThreesideMergeRequest
import com.intellij.diff.merge.external.AutomaticExternalMergeTool
import com.intellij.diff.tools.external.ExternalDiffSettings
import com.intellij.diff.tools.external.ExternalDiffToolUtil
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.SystemInfo
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.util.io.delete
import com.intellij.util.io.exists
import com.intellij.util.io.readBytes
import com.jetbrains.rd.util.reactive.hasTrueValue
import com.jetbrains.rd.util.reactive.valueOrThrow
import com.jetbrains.rdclient.util.idea.toIOFile
import com.jetbrains.rider.model.unity.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.yaml.UnityYamlFileType
import com.jetbrains.rider.plugins.unity.util.UnityInstallationFinder
import com.jetbrains.rider.projectView.solution
import java.nio.file.Paths

class UnityYamlAutomaticExternalMergeTool: AutomaticExternalMergeTool {
    override fun show(project: Project?, request: MergeRequest) {
        project ?: return

        val settings = ExternalDiffSettings()
        settings.isMergeTrustExitCode = true
        val appDataPath = UnityInstallationFinder.getInstance(project).getApplicationContentsPath() ?: return
        val extension = when {
            SystemInfo.isWindows -> ".exe"
            else -> ""
        }

        val tempDir = System.getProperty("java.io.tmpdir")
        val premergedBase = Paths.get(tempDir).resolve("premergedBase_"+request.hashCode())
        val premergedRight = Paths.get(tempDir).resolve("premergedRight_"+request.hashCode())

        try {
            settings.isMergeTrustExitCode = true
            settings.mergeExePath = appDataPath.resolve("Tools/UnityYAMLMerge" + extension).toString()
            val mergeParameters = project.solution.frontendBackendModel.backendSettings.mergeParameters.valueOrThrow
            if (mergeParameters.contains(" -p "))
                settings.mergeParameters = "$mergeParameters $premergedBase $premergedRight"
            else
                settings.mergeParameters = mergeParameters

            if (!ExternalDiffToolUtil.tryExecuteMerge(project, settings, request as ThreesideMergeRequest, null)){
                if (premergedBase.exists() && premergedRight.exists()){
                    val output: VirtualFile = (request.outputContent as FileContent).file
                    val byteContents = listOf(output.toIOFile().readBytes(), premergedBase.readBytes(), premergedRight.readBytes())
                    val preMerged = DiffRequestFactory.getInstance().createMergeRequest(project, output, byteContents, request.title, request.contentTitles)
                    MergeCallback.retarget(request, preMerged)

                    DiffManagerEx.getInstance().showMergeBuiltin(project, preMerged)
                }
                else
                    DiffManagerEx.getInstance().showMergeBuiltin(project, request)
            }
        }
        finally {
            if (premergedBase.exists()) premergedBase.delete()
            if (premergedRight.exists()) premergedRight.delete()
        }
    }

    override fun canShow(project: Project?, request: MergeRequest): Boolean {
        project?: return false

        if (!project.solution.frontendBackendModel.backendSettings.useUnityYamlMerge.hasTrueValue)
            return false

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
            && content.file.fileType == UnityYamlFileType
    }
}