package model.rider

import com.jetbrains.rider.model.nova.ide.SolutionModel
import com.jetbrains.rider.generator.nova.*
import com.jetbrains.rider.generator.nova.PredefinedType.*

@Suppress("unused")
object RdUnityModel : Ext(SolutionModel.Solution) {
    private val UnitTestLaunchPreference = enum {
        +"NUnit"
        +"EditMode"
    }

    init {
        map("data", string, string)
        property("unitTestPreference", UnitTestLaunchPreference.nullable)
        property("hideDataBaseToolWindow", bool)
        property("hideSolutionConfiguration", bool)
    }

}