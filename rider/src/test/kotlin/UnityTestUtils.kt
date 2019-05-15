import com.intellij.openapi.project.Project
import com.jetbrains.rider.test.base.BaseTestWithShell
import com.jetbrains.rider.test.framework.combine
import com.jetbrains.rider.test.scriptingApi.refreshFileSystem
import java.io.File

fun CopyUnityDll(project : Project, activeSolutionDirectory : File) {
    BaseTestWithShell.testDataDirectory.combine("tools").combine("UnityEngine.dll").copyTo(activeSolutionDirectory.combine("UnityEngine.dll"))
    refreshFileSystem(project)
}