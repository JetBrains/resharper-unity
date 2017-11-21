package com.jetbrains.rider

import java.io.Closeable

// TODO: remove after gradle plugin gets Kotlin 1.2
fun <T : Closeable?, R> T.use2(block: (T) -> R): R {
    var closed = false
    try {
        return block(this)
    } catch (e: Exception) {
        closed = true
        try {
            this?.close()
        } catch (closeException: Exception) {
        }
        throw e
    } finally {
        if (!closed) {
            this?.close()
        }
    }
}
