package com.jetbrains.rider.plugins.unity.diff

import com.intellij.diff.DiffManagerEx
import com.intellij.diff.DiffRequestFactory
import com.intellij.diff.contents.DiffContent
import com.intellij.diff.contents.FileContent
import com.intellij.diff.merge.MergeCallback
import com.intellij.diff.merge.MergeRequest
import com.intellij.diff.merge.MergeResult
import com.intellij.diff.merge.ThreesideMergeRequest
import com.intellij.diff.merge.external.AutomaticExternalMergeTool
import com.intellij.diff.tools.external.ExternalDiffSettings
import com.intellij.diff.tools.external.ExternalDiffSettings.ExternalToolGroup.MERGE_TOOL
import com.intellij.diff.tools.external.ExternalDiffToolUtil
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.SystemInfo
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.util.io.delete
import com.intellij.util.io.exists
import com.intellij.util.io.readBytes
import com.jetbrains.rd.util.reactive.hasTrueValue
import com.jetbrains.rd.util.reactive.valueOrThrow
import com.jetbrains.rdclient.util.idea.toIOFile
import com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.yaml.UnityYamlFileType
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.util.UnityInstallationFinder
import com.jetbrains.rider.projectView.solution
import java.nio.file.Paths

class UnityYamlAutomaticExternalMergeTool: AutomaticExternalMergeTool {
    companion object {
        private val myLogger = Logger.getInstance(UnityYamlAutomaticExternalMergeTool::class.java)
    }

    override fun show(project: Project?, request: MergeRequest) {
        project ?: return

        val externalMergeTool = ExternalDiffSettings.ExternalTool()
        externalMergeTool.isMergeTrustExitCode = true
        externalMergeTool.groupName = MERGE_TOOL
        val appDataPath = UnityInstallationFinder.getInstance(project).getApplicationContentsPath() ?: return
        val extension = when {
            SystemInfo.isWindows -> ".exe"
            else -> ""
        }

        val tempDir = System.getProperty("java.io.tmpdir")
        val premergedBase = Paths.get(tempDir).resolve("premergedBase_" + request.hashCode())
        val premergedRight = Paths.get(tempDir).resolve("premergedRight_" + request.hashCode())

        try {
            externalMergeTool.isMergeTrustExitCode = true
            externalMergeTool.programPath = appDataPath.resolve("Tools/UnityYAMLMerge" + extension).toString()
            val mergeParameters = project.solution.frontendBackendModel.backendSettings.mergeParameters.valueOrThrow
            if (mergeParameters.contains(" -p "))
                externalMergeTool.argumentPattern = "$mergeParameters $premergedBase $premergedRight"
            else
                externalMergeTool.argumentPattern = mergeParameters

            myLogger.info("PreMerge with ${externalMergeTool.programPath} ${externalMergeTool.argumentPattern}")

            if (!tryExecuteMerge(project, externalMergeTool, request as ThreesideMergeRequest)) {
                if (premergedBase.exists() && premergedRight.exists()) {
                    myLogger.info("PreMerge partially successful. Call ShowMergeBuiltin on pre-merged.")
                    val output: VirtualFile = (request.outputContent as FileContent).file
                    val byteContents = listOf(output.toIOFile().readBytes(), premergedBase.readBytes(), premergedRight.readBytes())
                    val preMerged = DiffRequestFactory.getInstance().createMergeRequest(project, output, byteContents, request.title, request.contentTitles)
                    MergeCallback.retarget(request, preMerged)

                    DiffManagerEx.getInstance().showMergeBuiltin(project, preMerged)
                } else {
                    myLogger.info("PreMerge unsuccessful. Call ShowMergeBuiltin.")
                    DiffManagerEx.getInstance().showMergeBuiltin(project, request)
                }
            }
        } finally {
            if (premergedBase.exists()) premergedBase.delete()
            if (premergedRight.exists()) premergedRight.delete()
        }
    }

    private fun tryExecuteMerge(project: Project?, externalMergeTool: ExternalDiffSettings.ExternalTool, request: ThreesideMergeRequest): Boolean {
        // see reference impl "com.intellij.diff.tools.external.ExternalDiffToolUtil#executeMerge"
        request.onAssigned(true)
        try {
            if (ExternalDiffToolUtil.tryExecuteMerge(project, externalMergeTool, request, null)) {
                myLogger.info("Merge with external tool was fully successful. Apply result.")
                request.applyResult(MergeResult.RESOLVED)
                return true
            }

            return false
        } catch (e: Exception) {
            myLogger.error("UnityYamlMerge failed.", e)
            return false
        } finally {
            request.onAssigned(false)
        }
    }

    override fun canShow(project: Project?, request: MergeRequest): Boolean {
        project ?: return false

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