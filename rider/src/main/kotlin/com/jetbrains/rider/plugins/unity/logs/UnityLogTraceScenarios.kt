package com.jetbrains.rider.plugins.unity.logs

import com.jetbrains.rd.platform.diagnostics.LogTraceScenario

class UnityLogTraceScenarios {
    object Unity : LogTraceScenario(
        "#com.jetbrains.rider.plugins.unity",
        "JetBrains.ReSharper.Plugins.Unity"
    )
}