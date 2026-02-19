package com.jetbrains.rider.plugins.unity.profiler.utils

import com.intellij.openapi.application.runInEdt
import com.intellij.ui.DocumentAdapter
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.isAlive
import com.jetbrains.rd.util.reactive.IProperty
import javax.swing.event.DocumentEvent
import javax.swing.text.JTextComponent

/**
 * Creates a bidirectional binding between a [JTextComponent] and an [IProperty].
 *
 * This utility handles the common pattern of synchronizing a text field with a reactive property,
 * preventing circular update loops through an internal guard flag.
 *
 * @param T the type of text component (e.g., JTextField, SearchTextField)
 * @param textComponent the UI text component to bind
 * @param property the reactive property to bind to
 * @param lifetime controls the lifetime of the binding subscription
 * @param onTextChanged optional callback invoked when text changes from user input (not from property updates)
 */
fun <T : JTextComponent> bindTextFieldToProperty(
    textComponent: T,
    property: IProperty<String>,
    lifetime: Lifetime,
    onTextChanged: ((String) -> Unit)? = null
) {
    var updatingFromProperty = false

    textComponent.document.addDocumentListener(object : DocumentAdapter() {
        override fun textChanged(e: DocumentEvent) {
            if (!updatingFromProperty) {
                val text = textComponent.text
                if (text != property.value) {
                    onTextChanged?.invoke(text)
                }
            }
        }
    })

    property.advise(lifetime) { newValue ->
        if (textComponent.text != newValue) {
            runInEdt {
                if (lifetime.isAlive) {
                    updatingFromProperty = true
                    try {
                        textComponent.text = newValue
                    } finally {
                        updatingFromProperty = false
                    }
                }
            }
        }
    }
}
