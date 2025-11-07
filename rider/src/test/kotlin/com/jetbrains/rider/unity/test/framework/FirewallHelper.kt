package com.jetbrains.rider.unity.test.framework

import com.jetbrains.rider.test.framework.frameworkLogger
import java.util.concurrent.TimeUnit

object FirewallHelper {

    enum class FirewallAction(val flag: String) {
        ADD("--add"),
        REMOVE("--remove"),
        GET_GLOBAL_STATE("--getglobalstate")
    }

    private const val FIREWALL_BINARY = "/usr/libexec/ApplicationFirewall/socketfilterfw"

    fun isFirewallEnabled(): Boolean {
        val processBuilder = ProcessBuilder(
            "bash", "-c",
            "$FIREWALL_BINARY ${FirewallAction.GET_GLOBAL_STATE} | grep -q 'Firewall is enabled'"
        )
        val process = processBuilder.start()

        try {
            if (!process.waitFor(30, TimeUnit.SECONDS)) {
                process.destroy()
                frameworkLogger.info("Timeout checking firewall state, return false")
                return false
            }

            return process.exitValue() == 0
        }
        catch (e: Exception) {
            frameworkLogger.info("Error checking firewall state: ${e.message}")
            return false
        }
        finally {
            if (process.isAlive) {
                process.destroyForcibly()
            }
        }
    }

    private fun manageFirewallRule(action: FirewallAction, appPath: String) {
        if (!isFirewallEnabled()) {
            frameworkLogger.info("Firewall is disabled, skipping configuration: ${action.flag} $appPath")
            return
        }

        val command = listOf(
            FIREWALL_BINARY,
            action.flag,
            appPath
        )

        val processBuilder = ProcessBuilder(command).inheritIO()
        val process = processBuilder.start()

        try {
            if (!process.waitFor(1, TimeUnit.MINUTES) || process.exitValue() != 0) {
                frameworkLogger.warn("Failed to manage firewall with command: ${command.joinToString(" ")}")
            }
        }
        catch (e: Exception) {
            frameworkLogger.warn("Failed to manage firewall with command: ${command.joinToString(" ")}", e)
        }
        finally {
            if (process.isAlive) {
                process.destroyForcibly()
            }
        }
    }

    fun addAllowRuleToFirewall(appPath: String) {
        manageFirewallRule(FirewallAction.ADD, appPath)
    }

    fun removeRuleFromFirewall(appPath: String) {
        manageFirewallRule(FirewallAction.REMOVE, appPath)
    }
}