package com.jetbrains.rider.unity.test.cases.integrationTests

import com.intellij.openapi.util.registry.RegistryManager
import com.jetbrains.rd.platform.diagnostics.LogTraceScenario
import com.jetbrains.rider.debugger.settings.DotNetDebuggerSettings
import com.jetbrains.rider.diagnostics.LogTraceScenarios
import com.jetbrains.rider.test.annotations.RiderTestTimeout
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.annotations.UnityTestSettings
import com.jetbrains.rider.test.annotations.report.ChecklistItems
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.annotations.report.Severity
import com.jetbrains.rider.test.annotations.report.SeverityLevel
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.debugger.disableTargetInvokeWithWatches
import com.jetbrains.rider.test.debugger.enableTargetInvokeWithWatches
import com.jetbrains.rider.test.enums.UnityVersion
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import com.intellij.util.application
import com.jetbrains.rider.test.scriptingApi.DebugTestExecutionContext
import com.jetbrains.rider.test.scriptingApi.immediateContext
import com.jetbrains.rider.test.scriptingApi.removeAllBreakpoints
import com.jetbrains.rider.test.scriptingApi.resumeSession
import com.jetbrains.rider.test.scriptingApi.toggleBreakpoint
import com.jetbrains.rider.test.scriptingApi.waitForPause
import com.jetbrains.rider.test.scriptingApi.withOpenedEvaluateEditor
import com.jetbrains.rider.unity.test.framework.api.attachDebuggerToUnityEditorAndPlay
import com.jetbrains.rider.unity.test.framework.base.IntegrationTestWithUnityProjectBase
import org.junit.jupiter.api.AfterEach
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test
import java.util.concurrent.TimeUnit

/**
 * RIDER-102087: evaluating `SystemAPI.Query<...>()` in the debugger.
 *
 * Every method in the `SystemAPI.Query` chain is source-generator scaffolding declared as
 * `throw InternalCompilerInterface.ThrowCodeGenException()`, so the expression cannot be executed as written -
 * it is lowered onto the real query API before evaluation. This test pins that end to end: without the
 * lowering the evaluation fails with `InvalidOperationException` from the codegen stub.
 *
 * The query in the test data takes two type arguments deliberately: that is what real DOTS code looks like,
 * and a lowering that only handles a single type argument does not fire here at all.
 *
 * Only the supported chain is covered. A modifier the lowering leaves alone - `WithChangeFilter` and friends -
 * throws from the codegen stub as it should, but the debugger worker reports that as a process-level error and
 * the test framework fails any test whose worker logged one, so the negative case cannot be pinned from here.
 */
@Subsystem(SubsystemConstants.UNITY_DEBUG)
@Feature("Debug Unity Dots")
@Severity(SeverityLevel.CRITICAL)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@Solution("UnityDotsDebug/Project")
@RiderTestTimeout(5, unit = TimeUnit.MINUTES)
@Tag(TeamCityTags.Plugins.Unity.Integration)
abstract class DotsSystemApiQueryEvaluationTest : IntegrationTestWithUnityProjectBase() {
    override val traceScenarios: Set<LogTraceScenario>
        get() = super.traceScenarios + LogTraceScenarios.Debugger + LogTraceScenarios.MonoDebuggerConnection

    private val queries = listOf(
        // The whole point of the ticket: this used to throw from the source-generator stub.
        "SystemAPI.Query<RefRW<LocalTransform>, RefRO<RotationSpeed>>()",
        // The single type argument form has to keep working too.
        "SystemAPI.Query<RefRW<LocalTransform>>()",
        // `WithEntityAccess` only changes the shape of the tuple the foreach yields, so it must not stop the
        // lowering from firing.
        "SystemAPI.Query<RefRW<LocalTransform>, RefRO<RotationSpeed>>().WithEntityAccess()",
        // `WithNone`/`WithAny` have to reach the query as separate constraints rather than being folded into the
        // required components - a lowering that ignores them would return the same entities as the unfiltered
        // query above (`WithNone` drops the single match to 0, `WithAny` keeps it at 1).
        "SystemAPI.Query<RefRW<LocalTransform>>().WithNone<RotationSpeed>()",
        "SystemAPI.Query<RefRW<LocalTransform>>().WithAny<RotationSpeed>()",
    )

    @Test // Evaluate SystemAPI.Query inside a DOTS system
    @ChecklistItems(["Evaluation/SystemAPI.Query in DOTS"])
    fun evaluateSystemApiQuery() {
        attachDebuggerToUnityEditorAndPlay(
            {
                RegistryManager.getInstance().get("rider.debugger.softdebugger.enable.burst.compatibility").setValue(true)
                // The lowered expression is a method call, so without this every evaluation below answers
                // "Implicit evaluation is disabled" instead of running the query.
                DotNetDebuggerSettings.instance.enableTargetInvokeWithWatches()
                toggleBreakpoint("QueryTransformSystem.cs", 15)
            },
            {
                setCustomRegexToMask()

                waitForPause()

                // The lowering is a backend expression preprocessor now, so it only fires for sandbox-backed
                // evaluations - unlike the old debugger-worker rewriter it does nothing for a bare expression
                // string. Cover both surfaces the preprocessor feeds.
                printlnIndented("=== Immediate window ===")
                immediateContext {
                    // Warm up the immediate window: the very first evaluation after the sandbox is created runs
                    // "cold", and the short-lived `Allocator.Temp` array a query lowers to can be reclaimed before
                    // its element is presented. A throwaway primitive evaluation absorbs that so the queries below
                    // present reliably.
                    evaluate("1 + 1")
                    queries.forEach { evaluate(it) }
                }

                printlnIndented("=== Evaluate expression ===")
                queries.forEach { query -> evaluateInEditor(query) }

                resumeSession()
            }, testGoldFile)
    }

    /**
     * Evaluates through the Evaluate/watch editor, which is a sandbox-backed `DotNetExpression` (so the backend
     * preprocessor runs). The text is set directly instead of typed - typing a generic expression char by char
     * triggers auto-close of `<`/`(` and corrupts it.
     */
    private fun DebugTestExecutionContext.evaluateInEditor(expression: String) {
        withOpenedEvaluateEditor(evaluate = true) {
            application.invokeAndWait {
                application.runWriteAction {
                    document.setText(expression)
                }
            }
        }
    }

    private fun DebugTestExecutionContext.setCustomRegexToMask() {
        dumpProfile.customRegexToMask["<id>"] = Regex("\\((\\d+:\\d+)\\)")
        dumpProfile.customRegexToMask["<float_value>"] = Regex("-?\\d+\\.*\\d*f")
    }

    @AfterEach
    fun clearAllBreakpoints() {
        removeAllBreakpoints()
        DotNetDebuggerSettings.instance.disableTargetInvokeWithWatches()
    }

    // Mirrors DotsDebuggerTest: solution build throws an error on the code generation phase.
    override fun buildSolutionAfterUnityStarts() {
    }

    // Mirrors DotsDebuggerTest: checkSwea hangs for unknown reason.
    override fun checkSwea() {
    }
}

@RiderTestTimeout(5, unit = TimeUnit.MINUTES)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@UnityTestSettings(unityVersion = UnityVersion.V6_2)
class DotsSystemApiQueryEvaluationTestUnity6_2 : DotsSystemApiQueryEvaluationTest()

@RiderTestTimeout(5, unit = TimeUnit.MINUTES)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@UnityTestSettings(unityVersion = UnityVersion.V6_3)
class DotsSystemApiQueryEvaluationTestUnity6_3 : DotsSystemApiQueryEvaluationTest()
