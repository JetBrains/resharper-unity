package com.jetbrains.rider.plugins.unity.explorer

import com.jetbrains.rider.projectView.views.SolutionViewPaneBase
import com.jetbrains.rider.projectView.views.actions.ShowAllFilesActionBase

class UnityShowAllFilesAction : ShowAllFilesActionBase() {
    override fun isApplicable(pane: SolutionViewPaneBase) = pane is UnityExplorer
}