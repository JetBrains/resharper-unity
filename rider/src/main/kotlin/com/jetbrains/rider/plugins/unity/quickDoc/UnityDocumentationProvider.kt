package com.jetbrains.rider.plugins.unity.quickDoc

import com.intellij.lang.documentation.DocumentationProvider
import com.intellij.openapi.util.SystemInfo
import com.intellij.openapi.vfs.LocalFileSystem
import com.intellij.openapi.vfs.VfsUtil
import com.intellij.psi.PsiElement
import com.intellij.psi.PsiManager
import com.intellij.util.PathUtil
import com.jetbrains.rider.projectView.solution

class UnityDocumentationProvider : DocumentationProvider {

    override fun getUrlFor(p0: PsiElement?, p1: PsiElement?): MutableList<String>? {
        val data = p0?.project?.solution?.customData?.data
        val context = data?.get("UNITY_ExternalDocContext")
        if (!context.isNullOrBlank())
            return arrayListOf(getUrlForContext(context!!))
        return null
    }

    override fun getQuickNavigateInfo(p0: PsiElement?, p1: PsiElement?): String? = null
    override fun getDocumentationElementForLookupItem(p0: PsiManager?, p1: Any?, p2: PsiElement?): PsiElement? = null
    override fun generateDoc(p0: PsiElement?, p1: PsiElement?): String? = null
    override fun getDocumentationElementForLink(p0: PsiManager?, p1: String?, p2: PsiElement?): PsiElement? = null

    private fun getUrlForContext(context: String): String {
        // We know context will be a fully qualified type or method name, starting
        // with either `UnityEngine.` or `UnityEditor.`
        val keyword = stripPrefix(context)
        val documentationRoot = getDocumentationRoot()
        return getFileUri(documentationRoot, "/ScriptReference/$keyword.html")
            ?: getFileUri(documentationRoot, "/ScriptReference/${keyword.replace('.', '-')}.html")
            ?: "https://docs.unity3d.com/ScriptReference/30_search.html?q=$keyword"
    }

    private fun stripPrefix(context: String): String {
        return context.drop(12)
    }

    private fun getDocumentationRoot(): String {
        return when {
            SystemInfo.isWindows -> {
                // Unity is installed to `C:\Program Files\Unity`, on both 32 bit and 64 bit
                // %PROGRAMFILES% differs if we're a 32 or 64 bit process
                // %PROGRAMFILES(X86)% only exists if we're 64 bit, and is always in the form `C:\Program Files (x86)`
                // %PROGRAMFILESW6432% is always in the form `C:\Program Files`
                val envvar = System.getenv("ProgramW6432") ?: System.getenv("ProgramFiles")
                PathUtil.toSystemDependentName(envvar).plus("/Unity/Editor/Data/Documentation/en")
            }
            SystemInfo.isMac -> "/Applications/Unity/Documentation/en"
            SystemInfo.isUnix -> "/opt/Unity/Editor/Data/Documentation/en"
            else -> ""
        }
    }

    private fun getFileUri(documentationRoot: String, htmlPath: String): String? {
        val path = documentationRoot + htmlPath
        val file = LocalFileSystem.getInstance().findFileByPath(path)
        if (file != null && file.exists())
            return VfsUtil.pathToUrl(path)
        return null
    }
}