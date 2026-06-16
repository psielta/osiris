package com.osiris.mobile.core.result

/**
 * A business error surfaced to the UI. [fieldErrors] maps a (camelCase) field name to its message;
 * an empty map means the error is global and should be shown as a snackbar.
 */
data class OsirisError(
    val message: String,
    val fieldErrors: Map<String, String> = emptyMap(),
)

sealed interface OsirisResult<out T> {
    data class Success<T>(val value: T) : OsirisResult<T>
    data class Failure(val error: OsirisError) : OsirisResult<Nothing>
}
