package com.jetbrains.rider.plugins.unity.settings.fileLayout

import com.intellij.javaee.ResourceRegistrar
import com.intellij.javaee.StandardResourceProvider
import com.intellij.openapi.module.Module
import com.intellij.openapi.vfs.VfsUtil
import com.intellij.psi.PsiFile
import com.intellij.psi.PsiManager
import com.intellij.psi.xml.XmlFile
import com.intellij.xml.XmlSchemaProvider
import com.jetbrains.rider.settings.fileLayout.filelayoutXmlSchema.FileLayoutConstants

private const val Namespace = "urn:schemas-jetbrains-com:member-reordering-patterns-unity"
private const val SchemaLocation = "schemas/fileLayout/unityFileLayout.xsd"

private class AdditionalFileLayoutStandardResourceProvider : StandardResourceProvider {
    override fun registerResources(registrar: ResourceRegistrar) {
        registrar.addStdResource(Namespace, SchemaLocation, AdditionalFileLayoutStandardResourceProvider::class.java.classLoader)
    }
}

private class AdditionalFileLayoutSchemaProvider : XmlSchemaProvider() {
    override fun isAvailable(file: XmlFile): Boolean {
        // Only for files ending with `.filelayout`, as per the C# File Layout options page. We create an editor showing
        // a file called "dummy.filelayout" in the options page to get syntax highlighting
        return file.originalFile.virtualFile?.extension.equals(FileLayoutConstants.FILE_EXTENSION, true)
    }

    override fun getSchema(url: String, module: Module?, baseFile: PsiFile): XmlFile? {
        module ?: return null
        val resource = AdditionalFileLayoutSchemaProvider::class.java.classLoader.getResource(SchemaLocation)
        val file = VfsUtil.findFileByURL(resource) ?: return null

        val psiFile = PsiManager.getInstance(module.project).findFile(file)
        return psiFile?.copy() as XmlFile
    }
}

