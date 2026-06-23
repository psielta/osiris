package com.osiris.mobile.android.feature.assistant

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.AnnotatedString
import androidx.compose.ui.text.SpanStyle
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.buildAnnotatedString
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontStyle
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextDecoration
import androidx.compose.ui.text.withStyle
import androidx.compose.ui.unit.dp

private val HeadingRegex = Regex("^#{1,6}\\s+")
private val BulletRegex = Regex("^[-*+]\\s+")
private val OrderedRegex = Regex("^(\\d+)[.)]\\s+")

/**
 * Renders the safe subset of Markdown the assistant produces (bold, italic, inline code, links,
 * headings and bullet/numbered lists) without any external dependency. Each block becomes its own
 * [Text] so lists and paragraphs lay out correctly; inline spans use [AnnotatedString].
 */
@Composable
fun MarkdownText(
    text: String,
    color: Color,
    style: TextStyle,
    modifier: Modifier = Modifier,
) {
    val lines = text.replace("\r\n", "\n").replace("\r", "\n").split("\n")
    val paragraph = StringBuilder()

    Column(modifier = modifier, verticalArrangement = Arrangement.spacedBy(3.dp)) {
        @Composable
        fun flushParagraph() {
            if (paragraph.isNotBlank()) {
                Text(text = parseInline(paragraph.toString().trim()), color = color, style = style)
            }
            paragraph.setLength(0)
        }

        for (raw in lines) {
            val line = raw.trimEnd()
            val trimmed = line.trimStart()
            when {
                trimmed.isEmpty() -> flushParagraph()

                HeadingRegex.containsMatchIn(trimmed) -> {
                    flushParagraph()
                    Text(
                        text = parseInline(trimmed.replace(HeadingRegex, "")),
                        color = color,
                        style = style.copy(fontWeight = FontWeight.SemiBold),
                    )
                }

                BulletRegex.containsMatchIn(trimmed) -> {
                    flushParagraph()
                    val indent = line.length - trimmed.length
                    Row {
                        Text(text = if (indent >= 2) "    •  " else "•  ", color = color, style = style)
                        Text(text = parseInline(trimmed.replace(BulletRegex, "")), color = color, style = style)
                    }
                }

                OrderedRegex.containsMatchIn(trimmed) -> {
                    flushParagraph()
                    val number = OrderedRegex.find(trimmed)!!.groupValues[1]
                    Row {
                        Text(text = "$number.  ", color = color, style = style)
                        Text(text = parseInline(trimmed.replace(OrderedRegex, "")), color = color, style = style)
                    }
                }

                else -> {
                    if (paragraph.isNotEmpty()) paragraph.append(' ')
                    paragraph.append(trimmed)
                }
            }
        }
        flushParagraph()
    }
}

private fun parseInline(text: String): AnnotatedString = buildAnnotatedString {
    var i = 0
    while (i < text.length) {
        // Bold: **...**
        if (text.startsWith("**", i)) {
            val end = text.indexOf("**", i + 2)
            if (end != -1) {
                withStyle(SpanStyle(fontWeight = FontWeight.Bold)) { append(text.substring(i + 2, end)) }
                i = end + 2
                continue
            }
        }
        // Inline code: `...`
        if (text[i] == '`') {
            val end = text.indexOf('`', i + 1)
            if (end != -1) {
                withStyle(SpanStyle(fontFamily = FontFamily.Monospace)) { append(text.substring(i + 1, end)) }
                i = end + 1
                continue
            }
        }
        // Link: [label](url) -> render the label, underlined (no navigation handling).
        if (text[i] == '[') {
            val close = text.indexOf(']', i + 1)
            if (close != -1 && close + 1 < text.length && text[close + 1] == '(') {
                val parenEnd = text.indexOf(')', close + 2)
                if (parenEnd != -1) {
                    withStyle(SpanStyle(textDecoration = TextDecoration.Underline)) {
                        append(text.substring(i + 1, close))
                    }
                    i = parenEnd + 1
                    continue
                }
            }
        }
        // Italic: *...* (single asterisk, non-empty)
        if (text[i] == '*') {
            val end = text.indexOf('*', i + 1)
            if (end > i + 1) {
                withStyle(SpanStyle(fontStyle = FontStyle.Italic)) { append(text.substring(i + 1, end)) }
                i = end + 1
                continue
            }
        }
        append(text[i])
        i++
    }
}
