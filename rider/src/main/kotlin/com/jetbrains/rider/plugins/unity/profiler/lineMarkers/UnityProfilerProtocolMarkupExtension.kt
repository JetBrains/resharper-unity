package com.jetbrains.rider.plugins.unity.profiler.lineMarkers

import com.intellij.openapi.editor.Document
import com.intellij.openapi.util.Key
import com.jetbrains.rd.ide.model.MarkupModelExtension
import com.jetbrains.rd.ide.model.TooltipProviderModel
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rdclient.daemon.IFrontendProtocolMarkupExtension

class UnityProfilerProtocolMarkupExtension : IFrontendProtocolMarkupExtension {
  override fun createExtensions(lifetime: Lifetime, document: Document): List<MarkupModelExtension> {
    return listOf(
      TooltipProviderModel(UNITY_PROFILER_TOOLTIP_PROVIDER_KEY.toString())
    )
  }
}

private val UNITY_PROFILER_TOOLTIP_PROVIDER_KEY = Key<TooltipProviderModel>("UNITY_PROFILER_TOOLTIP_PROVIDER_KEY")
