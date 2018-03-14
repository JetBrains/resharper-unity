package com.jetbrains.rider.plugins.unity.quickDoc

import com.intellij.lang.documentation.DocumentationProvider
import com.intellij.openapi.util.SystemInfo
import com.intellij.psi.PsiElement
import com.intellij.psi.PsiManager
import com.intellij.util.SystemProperties
import com.intellij.util.io.exists
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.projectView.solution
import java.nio.file.Files
import java.nio.file.Path
import java.nio.file.Paths

class UnityDocumentationProvider : DocumentationProvider {

    private val documentationRoot by lazy(this::findFallbackDocumentationRoot)

    override fun getUrlFor(p0: PsiElement?, p1: PsiElement?): MutableList<String>? {
        val data = p0?.project?.solution?.rdUnityModel?.data
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

    private fun findFallbackDocumentationRoot(): Path? {
        return when {
            SystemInfo.isWindows -> {
                // Unity is installed to `C:\Program Files\Unity`, on both 32 bit and 64 bit
                // %PROGRAMFILES% differs if we're a 32 or 64 bit process
                // %PROGRAMFILES(X86)% only exists if we're 64 bit, and is always in the form `C:\Program Files (x86)`
                // %PROGRAMFILESW6432% is always in the form `C:\Program Files`
                val envvar = System.getenv("ProgramW6432") ?: System.getenv("ProgramFiles")
                Paths.get(envvar).resolve("/Unity/Editor/Data/Documentation/en")
            }
            SystemInfo.isMac -> Paths.get("/Applications/Unity/Unity.app/Contents/Documentation/en")
            SystemInfo.isLinux -> {
                // Unity 2017.3 is the first version to add support for local documentation.
                // It will write to /opt if it has write permissions, otherwise, it writes to
                // the user's home directory, but adds the version, e.g. ~/Unity-2017.3.0b1
                val globalDocRoots = getUnityChildDirs(Paths.get("/opt"))
                val homeDocRoots = getUnityChildDirs(Paths.get(SystemProperties.getUserHome()))
                (globalDocRoots + homeDocRoots)
                        .map { it.resolve("Editor/Data/Documentation/en") }
                        .firstOrNull { it.exists() }
            }
            else -> null
        }
    }

    private fun getUnityChildDirs(root: Path): Sequence<Path> = Files.newDirectoryStream(root, "Unity*").asSequence()
}