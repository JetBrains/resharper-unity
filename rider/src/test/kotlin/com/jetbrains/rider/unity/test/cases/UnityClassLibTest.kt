package com.jetbrains.rider.unity.test.cases
import com.intellij.openapi.project.Project
import com.intellij.openapi.rd.createNestedDisposable
import com.intellij.openapi.vfs.newvfs.impl.VfsRootAccess
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rider.test.OpenSolutionParams
import com.jetbrains.rider.test.allure.Subsystem
import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.BaseTestWithSolutionBase
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.framework.closeProjectsWaitForBackendWillBeClosed
import com.jetbrains.rider.test.framework.combine
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.framework.persistAllFilesOnDisk
import com.jetbrains.rider.test.scriptingApi.*
import io.qameta.allure.*
import org.testng.annotations.Test
import java.io.File
import java.time.Duration

@Epic(Subsystem.UNITY_PLUGIN)
@Feature("Unity Class Library template")
@Severity(SeverityLevel.NORMAL)
@TestEnvironment(platform = [PlatformType.MAC_OS_ALL, PlatformType.WINDOWS_ALL]) // requires mono
class UnityClassLibTest : BaseTestWithSolutionBase() {

    private val templateId = TemplateIdWithVersion("JetBrains.Common.Unity.Library.CSharp")
    private val editorGoldFile: File
        get() = File(testCaseGoldDirectory, "${testMethod.name}_opened")

    @Mute("RIDER-98881")
    @Test
    @Description("Test Unity Class Library template")
    fun testUnityClassLibraryTemplate() {
        val params = OpenSolutionParams()
        params.restoreNuGetPackages = true //it's always true in getAndOpenSolution
        params.waitForCaches = true

        val lifetimeDef = Lifetime.Eternal.createNested()
        try {
            val newProject = getAndOpenSolution(lifetimeDef.lifetime, templateId, true, params)

            try {
                executeWithGold(editorGoldFile) {
                    dumpOpenedDocument(it, newProject)
                }

                testProjectModel(testGoldFile, newProject) {
                    dump("Opened", newProject, activeSolutionDirectory, false, false) {} //contains close editors
                }

                // todo: fix UnityEngine.dll reference - either install Unity or from nuget
                checkSwea(newProject)

            } finally {
                closeSolutionAndResetSettings(newProject)
            }
        } finally {
            lifetimeDef.terminate()
        }
    }

    private fun getAndOpenSolution(
            lifetime: Lifetime,
            templateId: TemplateIdWithVersion,
            sameDirectory: Boolean,
            params: OpenSolutionParams
    ): Project {
        closeProjectsWaitForBackendWillBeClosed(Duration.ofSeconds(60), false, false)
        val parameters: HashMap<String, String> = hashMapOf()

        VfsRootAccess.allowRootAccess(lifetime.createNestedDisposable(), testClassDataDirectory.combine("lib", "UnityEngine.dll").absolutePath)
        parameters["PathToUnityEngine"] = testClassDataDirectory.combine("lib", "UnityEngine.dll").absolutePath
        val newProject = createSolutionFromTemplate(templateId, null, activeSolutionDirectory, sameDirectory, null, parameters) { }!!

        newProject.enableBackendAsserts()
        persistAllFilesOnDisk()

        waitForSolution(newProject, params)

        return newProject
    }
}