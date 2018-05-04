package model.rider

import com.jetbrains.rider.generator.nova.Ext
import com.jetbrains.rider.generator.nova.PredefinedType.string
import com.jetbrains.rider.generator.nova.map
import com.jetbrains.rider.generator.nova.nullable
import com.jetbrains.rider.generator.nova.property
import com.jetbrains.rider.model.nova.ide.SolutionModel
import org.jetbrains.kotlin.ir.expressions.IrConstKind

@Suppress("unused")
object RdUnityModel : Ext(SolutionModel.Solution) {
    private val UnitTestLaunchPreference = enum {
        +"NUnit"
        +"EditMode"
    }

    init {
        map("data", string, string)
        property("unitTestPreference", UnitTestLaunchPreference.nullable)
    }

}