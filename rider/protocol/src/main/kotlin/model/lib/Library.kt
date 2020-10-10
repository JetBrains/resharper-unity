package model.lib

import com.jetbrains.rd.generator.nova.PredefinedType.*
import com.jetbrains.rd.generator.nova.Root
import com.jetbrains.rd.generator.nova.csharp.CSharp50Generator
import com.jetbrains.rd.generator.nova.field
import com.jetbrains.rd.generator.nova.kotlin.Kotlin11Generator
import com.jetbrains.rd.generator.nova.setting

object Library : Root() {

    init {
        setting(Kotlin11Generator.Namespace, "com.jetbrains.rider.model.unity")
        setting(CSharp50Generator.Namespace, "JetBrains.Rider.Model.Unity")
    }

    val LogEvent = structdef {
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
}