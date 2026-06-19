package com.jetbrains.rider.unity.test.cases

import com.intellij.openapi.vfs.VfsUtil
import com.jetbrains.rider.ideaInterop.find.scopes.RiderSolutionScope
import com.jetbrains.rider.projectView.solutionDirectoryPath
import com.jetbrains.rider.test.OpenSolutionParams
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.annotations.report.Issue
import com.jetbrains.rider.test.annotations.report.Severity
import com.jetbrains.rider.test.annotations.report.SeverityLevel
import com.jetbrains.rider.test.base.PerTestSettingsTestBase
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.scriptingApi.withSolution
import org.testng.Assert.assertTrue
import org.testng.annotations.Test

@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity solution scope")
@Severity(SeverityLevel.CRITICAL)
@Issue("RIDER-139698, RIDER-117479")
@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
@Solution("RiderSample")
class UnityAssetSolutionScopeTest : PerTestSettingsTestBase() {

    @Test(description = "Unity Scenes must be inside RiderSolutionScope so CodeVision popup doesn't filter them out")
    fun assetSceneIsInSolutionScope() {
        withSolution("RiderSample", OpenSolutionParams().apply { waitForCaches = true }) {
            val solutionRoot = VfsUtil.findFile(project.solutionDirectoryPath, true)!!
            val sceneFile = solutionRoot.findFileByRelativePath("Assets/Scenes/SampleScene.unity")!!
            val scope = RiderSolutionScope(project, withExternalItems = false)
            assertTrue(scope.contains(sceneFile),
                "Asset file ${sceneFile.path} must be in Solution scope (otherwise ShowUsages popup labels usages 'out of scope Solution')")
        }
    }
}