package com.jetbrains.rider.unity.test.cases

import com.jetbrains.rider.test.framework.testData.IRiderTestDataMarker
import java.nio.file.Path

@Suppress("unused")
object RiderTestDataMarker : IRiderTestDataMarker {
  override val testDataFromRoot: Path
    get() = Path.of("src/test/testData")

  override val pluginDirectoryInUltimate: Path
    get() = Path.of("dotnet/Plugins/ReSharperUnity/rider")
}