import com.intellij.openapi.project.Project
import com.intellij.openapi.util.SystemInfo
import com.jetbrains.rider.test.base.BaseTestWithShell
import com.jetbrains.rider.test.framework.combine
import com.jetbrains.rider.test.framework.downloadAndExtractArchiveArtifactIntoPersistentCache
import com.jetbrains.rider.test.scriptingApi.refreshFileSystem
import java.io.File



fun downloadUnityDll() : File{
    return downloadAndExtractArchiveArtifactIntoPersistentCache("https://repo.labs.intellij.net/dotnet-rider-test-data/UnityEngine-2018.3-08-01-2019.dll.zip").combine("UnityEngine.dll")
}

fun copyUnityDll(unityDll : File, project : Project, activeSolutionDirectory : File) {
    unityDll.copyTo(activeSolutionDirectory.combine("UnityEngine.dll"))
    refreshFileSystem(project)
}