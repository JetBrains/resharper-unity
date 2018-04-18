package model.rider

import com.jetbrains.rider.generator.nova.Ext
import com.jetbrains.rider.generator.nova.PredefinedType.string
import com.jetbrains.rider.generator.nova.map
import com.jetbrains.rider.model.nova.ide.SolutionModel

@Suppress("unused")
object RdUnityModel : Ext(SolutionModel.Solution) {
    init {
        map("data", string, string)
    }
}