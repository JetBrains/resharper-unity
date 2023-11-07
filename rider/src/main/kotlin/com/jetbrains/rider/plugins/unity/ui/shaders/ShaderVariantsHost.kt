package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.components.Service
import com.intellij.openapi.project.Project
import com.jetbrains.rd.platform.util.idea.LifetimedService
import com.jetbrains.rd.util.lifetime.SequentialLifetimes
import com.jetbrains.rd.util.reactive.AddRemove
import com.jetbrains.rd.util.reactive.ViewableSet
import com.jetbrains.rider.plugins.unity.FrontendBackendHost

@Service(Service.Level.PROJECT)
class ShaderVariantsHost(project: Project) : LifetimedService() {
    private val frontendBackendHost = FrontendBackendHost.getInstance(project)

    val enabledShaderKeywords: ViewableSet<String> = ViewableSet()
    private val enabledKeywordsLifetimes: SequentialLifetimes = SequentialLifetimes(serviceLifetime)

    init {
      frontendBackendHost.model.defaultShaderVariant.advise(serviceLifetime) {
          syncKeywords(it.enabledKeywords)
          it.enabledKeywords.advise(enabledKeywordsLifetimes.next()) { event ->
              when (event.kind) {
                  AddRemove.Add -> enabledShaderKeywords.add(event.value)
                  AddRemove.Remove -> enabledShaderKeywords.remove(event.value)
              }
          }
      }
    }

    private fun syncKeywords(newEnabledKeywords: Collection<String>) {
        if (enabledShaderKeywords.size > 0) {
            val unprocessed = HashSet(enabledShaderKeywords)
            for (item in newEnabledKeywords) {
                if (!unprocessed.remove(item))
                    enabledShaderKeywords.add(item)
            }
            for (item in unprocessed)
                enabledShaderKeywords.remove(item)
        }
        else {
            enabledShaderKeywords.addAll(newEnabledKeywords)
        }
    }
}