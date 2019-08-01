import com.intellij.openapi.project.Project
import com.intellij.openapi.util.SystemInfo
import com.jetbrains.rider.test.base.BaseTestWithShell
import com.jetbrains.rider.test.framework.combine
import com.jetbrains.rider.test.framework.downloadAndExtractArchiveArtifactIntoPersistentCache
import com.jetbrains.rider.test.scriptingApi.refreshFileSystem
import java.io.File



fun DownloadUnityDll() : File{
    return downloadAndExtractArchiveArtifactIntoPersistentCache("https://repo.labs.intellij.net/dotnet-rider-test-data/unityengine-2018.3-07-30-2019.dll")
}

fun CopyUnityDll(unityDll : File, project : Project, activeSolutionDirectory : File) {
    unityDll.copyTo(activeSolutionDirectory.combine("UnityEngine.dll"))
    refreshFileSystem(project)
}