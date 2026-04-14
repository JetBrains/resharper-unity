package model.lib

import com.jetbrains.rd.generator.nova.*
import com.jetbrains.rd.generator.nova.PredefinedType.*
import com.jetbrains.rd.generator.nova.csharp.CSharp50Generator
import com.jetbrains.rd.generator.nova.kotlin.Kotlin11Generator

object Library : Root() {

    init {
        setting(Kotlin11Generator.Namespace, "com.jetbrains.rider.plugins.unity.model")
        setting(CSharp50Generator.Namespace, "JetBrains.Rider.Model.Unity")
    }

    override val isLibrary = true

    val UnityEditorState = enum {
        +"Disconnected"
        +"Idle"
        +"Play"
        +"Pause"
        +"Refresh"
    }

    // This is a structdef because the values do not change during the lifetime of the application
    // (Note that the struct might be set and populate via heuristics, not necessarily a running instance of Unity)
    val UnityApplicationData = structdef {
        field("applicationPath", string)
        field("applicationContentsPath", string)
        field("applicationVersion", string)
        field("editorLogPath", string.nullable).documentation = "Editor log path. Will be null when Unity protocol is not connected"
        field("playerLogPath", string.nullable).documentation = "Player log path. Will be null when Unity protocol is not connected"
        field("unityProcessId", int.nullable).documentation = "Used by the test runner and the frontend uses it in a call " +
            "to AllowSetForegroundWindow to allow Unity to bring itself to the foreground, e.g. when opening an .asmdef file." +
            "Will be null when the Unity protocol is not connected"
    }

    val UnityApplicationSettings = aggregatedef("UnityApplicationSettings") {
        property("scriptCompilationDuringPlay", enum("ScriptCompilationDuringPlay") {
            +"RecompileAndContinuePlaying"
            +"RecompileAfterFinishedPlaying"
            +"StopPlayingAndRecompile"
        })
    }

    val UnityProjectSettings = aggregatedef("UnityProjectSettings") {
        property("buildLocation", string).documentation =
            "Path to the executable of the last built Standalone player, if it exists. Can be empty"
    }

    val RunMethodData = structdef {
        field("assemblyName", string)
        field("typeName", string)
        field("methodName", string)
    }

    val RunMethodResult = structdef {
        field("success", bool)
        field("message", string)
        field("stackTrace", string)
    }

    val PlayControls = aggregatedef("PlayControls") {
        property("play", bool)
        property("pause", bool)
        signal("step", void)
    }

    private val LogEvent = structdef {
        field("time", long)
        field("type", enum("LogEventType") {
            +"Error"
            +"Warning"
            +"Message"
        })
        field("mode", enum("LogEventMode") {
            +"Edit"
            +"Play"
        })
        field("message", string)
        field("stackTrace", string)
    }

    val ConsoleLogging = aggregatedef("ConsoleLogging") {
        sink("onConsoleLogEvent", LogEvent)
        property("lastPlayTime", long)
        property("lastInitTime", long)
    }

    val ProfilingData = structdef {
        field("enterPlayMode", bool)
        field("unityProfilerApiPath", string)
        field("needRestartScripts", bool)
    }

    val ProfilerThread = structdef {
        field("index", int)
        field("name", string)
    }

    val ProfilerSnapshotRequest = structdef {
        field("frameIndex", int)
        field("thread", ProfilerThread)
    }

    val UnityProfilerRecordInfo = structdef {
        field("firstFrameId", int)
        field("lastFrameId", int)
        field("firstFrameNs", ulong)
        field("lastFrameNs", ulong)
    }

    val SelectionState = structdef {
        field("selectedFrameIndex", int)
        field("selectedThread", Library.ProfilerThread)
    }

    val MainFrameTimingsAndThreads = structdef {
        field("samples", immutableList(
            structdef("timingInfo") {
                    field("frameId", int)
                    field("ms", float)
                }).nullable
        )
        field("threads", immutableList(ProfilerThread).nullable)
    }

    // region MCP Profiler types

    val McpOverviewRequest = structdef {
        field("thresholdMs", double)
        field("limit", int)
        field("sortBy", string)  // "duration" | "time" | "memory"
    }

    val McpOverviewResponse = structdef {
        field("filePath", string)
        field("totalFrames", int)
        field("startFrameId", int)
        field("framesWritten", int)
        field("averageMs", double)
        field("p50Ms", double)
        field("p95Ms", double)
        field("p99Ms", double)
    }

    val McpHotspotEntry = structdef {
        field("qualifiedName", string)
        field("path", string)
        field("totalDurationMs", double)
        field("callCount", int)
        field("totalMemoryBytes", long)
    }

    val McpCallstackEntry = structdef {
        field("qualifiedName", string)
        field("path", string)
        field("durationMs", double)
        field("framePercentage", double)
        field("memoryAllocationBytes", long)
        field("depth", int)
        field("childrenCount", int)
        field("callCount", int)
        field("isTarget", bool)
    }

    val McpFrameAnalysisRequest = structdef {
        field("frameIndex", int)
        field("threadName", string)
        field("focusOn", string.nullable)
        field("sortBy", string)  // "duration" | "memory"
        field("limit", int)
    }

    val McpFrameAnalysisResponse = structdef {
        field("frameIndex", int)
        field("threadName", string)
        field("frameDurationMs", double)
        field("totalAllocBytes", long)
        field("totalSampleCount", int)
        field("hotspots", immutableList(McpHotspotEntry))
        field("callstack", immutableList(McpCallstackEntry).nullable)
    }

    val McpCrossFrameHotspot = structdef {
        field("qualifiedName", string)
        field("totalDurationMs", double)
        field("callCount", int)
        field("avgDurationMs", double)
        field("maxSingleDurationMs", double)
        field("totalMemoryBytes", long)
        field("framesPresent", int)
    }

    val McpBatchAnalyzeRequest = structdef {
        field("startFrame", int)       // -1 = recording start
        field("limit", int)
        field("thresholdMs", double)
        field("threadName", string)
        field("snapshotLimit", int)
        field("minSampleDurationMs", double)
        field("sortBy", string)  // "duration" | "memory"
    }

    val McpBatchAnalyzeResponse = structdef {
        field("filePath", string)
        field("framesAnalyzed", int)
        field("snapshotsFetched", int)
        field("threadName", string)
        field("totalDurationMs", double)
        field("totalAllocBytes", long)
        field("topHotspots", immutableList(McpCrossFrameHotspot))
    }

    // endregion
}