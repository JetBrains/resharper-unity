package model.rider

import com.jetbrains.rider.generator.nova.*
import com.jetbrains.rider.model.nova.ide.*
import com.jetbrains.rider.generator.nova.PredefinedType.*

@Suppress("unused")
object RdUnityModel : Ext(SolutionModel.Solution) {
    init {
        map("data", string, string)
    }
}