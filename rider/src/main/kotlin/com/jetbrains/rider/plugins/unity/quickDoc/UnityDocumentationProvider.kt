package com.jetbrains.rider.plugins.unity.quickDoc

import com.intellij.lang.documentation.DocumentationProvider
import com.intellij.openapi.project.Project
import com.intellij.psi.PsiElement
import com.intellij.psi.PsiManager
import com.intellij.util.io.exists
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.plugins.unity.util.UnityInstallationFinder
import com.jetbrains.rider.projectView.solution
import java.nio.file.Path

class UnityDocumentationProvider() : DocumentationProvider {

    override fun getUrlFor(p0: PsiElement?, p1: PsiElement?): MutableList<String>? {
        val project = p0?.project
        val context = project?.solution?.rdUnityModel?.externalDocContext?.valueOrNull
        if (context != null && !context.isNullOrBlank())
            return arrayListOf(getUrlForContext(context, project))
        return null
    }

    override fun getQuickNavigateInfo(p0: PsiElement?, p1: PsiElement?): String? = null
    override fun getDocumentationElementForLookupItem(p0: PsiManager?, p1: Any?, p2: PsiElement?): PsiElement? = null
    override fun generateDoc(p0: PsiElement?, p1: PsiElement?): String? = null
    override fun getDocumentationElementForLink(p0: PsiManager?, p1: String?, p2: PsiElement?): PsiElement? = null

    private fun getUrlForContext(context: String, project: Project): String {
        // We know context will be a fully qualified type or method name, starting
        // with either `UnityEngine.` or `UnityEditor.`
        // TODO: Use the current version for the fallback online search
        // E.g. https://docs.unity3d.com/2017.4/Documentation/ScriptReference/30_search.html?q=...
        val keyword = stripPrefix(context)
        val documentationRoot = getLocalDocumentationRoot(project)
        return getFileUri(documentationRoot, "ScriptReference/$keyword.html")
            ?: getFileUri(documentationRoot, "ScriptReference/${keyword.replace('.', '-')}.html")
            ?: "https://docs.unity3d.com/ScriptReference/30_search.html?q=$keyword"
    }

    private fun stripPrefix(context: String): String {
        // 12 for `UnityEngine.` or `UnityEditor.`
        return context.drop(12)
    }

    private fun getFileUri(documentationRoot: Path?, htmlPath: String): String? {
        if (documentationRoot == null)
            return null
        val path = documentationRoot.resolve(htmlPath)
        if (path.exists()) {
            return path.toUri().toASCIIString()
        }
        return null
    }

    private fun getLocalDocumentationRoot(project:Project): Path? {
        return UnityInstallationFinder.getInstance(project).getDocumentationRoot()
    }
}