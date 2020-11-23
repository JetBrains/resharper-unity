package integrationTests

import com.jetbrains.dotCover.actions.frontendDataContext.RiderDotCoverDataConstantProvider
import org.testng.annotations.Test

class Test {
    @Test
    fun test() {
        RiderDotCoverDataConstantProvider.enterPseudoToolWindowActivatedMode()
    }
}