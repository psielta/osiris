package com.osiris.mobile.android.feature.docs

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material3.Card
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.osiris.mobile.android.R
import com.osiris.mobile.domain.documentation.DocumentationCatalog
import com.osiris.mobile.domain.documentation.DocumentationGuide

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun DocumentationListScreen(
    onNavigateBack: () -> Unit,
    onOpenGuide: (String) -> Unit,
) {
    val guides = DocumentationCatalog.guides
    val groups = remember(guides) { guides.groupBy { it.group } }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(stringResource(R.string.docs_title)) },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = stringResource(R.string.back))
                    }
                },
            )
        },
    ) { padding ->
        LazyColumn(
            modifier = Modifier.fillMaxSize().padding(padding),
            contentPadding = PaddingValues(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            item {
                Text(
                    text = stringResource(R.string.docs_intro),
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }

            groups.forEach { (group, groupGuides) ->
                item(key = "group-$group") {
                    Text(
                        text = group,
                        style = MaterialTheme.typography.titleSmall,
                        color = MaterialTheme.colorScheme.primary,
                        modifier = Modifier.padding(top = 8.dp),
                    )
                }

                items(groupGuides, key = { it.slug }) { guide ->
                    DocumentationGuideRow(guide = guide, onClick = { onOpenGuide(guide.slug) })
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun DocumentationDetailScreen(
    slug: String,
    onNavigateBack: () -> Unit,
) {
    val guide = remember(slug) { DocumentationCatalog.find(slug) }
    val blocks = remember(guide?.markdown) { guide?.markdown?.let(::parseMarkdown).orEmpty() }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(guide?.title ?: stringResource(R.string.docs_title)) },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = stringResource(R.string.back))
                    }
                },
            )
        },
    ) { padding ->
        if (guide == null) {
            Box(Modifier.fillMaxSize().padding(padding), contentAlignment = Alignment.Center) {
                Text(
                    text = stringResource(R.string.docs_not_found),
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
            return@Scaffold
        }

        LazyColumn(
            modifier = Modifier.fillMaxSize().padding(padding),
            contentPadding = PaddingValues(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            items(blocks) { block ->
                MarkdownBlockView(block)
            }
        }
    }
}

@Composable
private fun DocumentationGuideRow(
    guide: DocumentationGuide,
    onClick: () -> Unit,
) {
    Card(Modifier.fillMaxWidth().clickable(onClick = onClick)) {
        Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(6.dp)) {
            Text(guide.title, style = MaterialTheme.typography.titleMedium)
            Text(
                text = guide.description,
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
        }
    }
}

@Composable
private fun MarkdownBlockView(block: MarkdownBlock) {
    when (block) {
        is MarkdownBlock.Heading -> Text(
            text = block.text,
            style = when (block.level) {
                1 -> MaterialTheme.typography.headlineSmall
                2 -> MaterialTheme.typography.titleLarge
                else -> MaterialTheme.typography.titleMedium
            },
            color = MaterialTheme.colorScheme.onSurface,
            fontWeight = FontWeight.SemiBold,
            modifier = Modifier.padding(top = if (block.level == 1) 0.dp else 8.dp),
        )

        is MarkdownBlock.Paragraph -> Text(
            text = block.text,
            style = MaterialTheme.typography.bodyMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
        )

        is MarkdownBlock.Bullet -> Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            Text("•", color = MaterialTheme.colorScheme.primary)
            Text(
                text = block.text,
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                modifier = Modifier.weight(1f),
            )
        }

        is MarkdownBlock.Numbered -> Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            Text("${block.number}.", color = MaterialTheme.colorScheme.primary)
            Text(
                text = block.text,
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                modifier = Modifier.weight(1f),
            )
        }

        is MarkdownBlock.Quote -> Card(Modifier.fillMaxWidth()) {
            Text(
                text = block.text,
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.primary,
                modifier = Modifier.padding(16.dp),
            )
        }

        is MarkdownBlock.Table -> MarkdownTable(block.rows)
    }
}

@Composable
private fun MarkdownTable(rows: List<List<String>>) {
    if (rows.isEmpty()) {
        return
    }

    Card(Modifier.fillMaxWidth()) {
        Column(Modifier.padding(12.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
            rows.forEachIndexed { index, row ->
                if (index > 0) {
                    HorizontalDivider()
                }
                Text(
                    text = row.joinToString("  |  "),
                    style = if (index == 0) MaterialTheme.typography.labelLarge else MaterialTheme.typography.bodyMedium,
                    color = if (index == 0) MaterialTheme.colorScheme.onSurface else MaterialTheme.colorScheme.onSurfaceVariant,
                    fontWeight = if (index == 0) FontWeight.SemiBold else FontWeight.Normal,
                )
            }
        }
    }
}

private sealed interface MarkdownBlock {
    data class Heading(val level: Int, val text: String) : MarkdownBlock
    data class Paragraph(val text: String) : MarkdownBlock
    data class Bullet(val text: String) : MarkdownBlock
    data class Numbered(val number: Int, val text: String) : MarkdownBlock
    data class Quote(val text: String) : MarkdownBlock
    data class Table(val rows: List<List<String>>) : MarkdownBlock
}

private fun parseMarkdown(markdown: String): List<MarkdownBlock> {
    val blocks = mutableListOf<MarkdownBlock>()
    val paragraph = mutableListOf<String>()
    val tableRows = mutableListOf<List<String>>()

    fun flushParagraph() {
        if (paragraph.isNotEmpty()) {
            blocks += MarkdownBlock.Paragraph(cleanInline(paragraph.joinToString(" ")))
            paragraph.clear()
        }
    }

    fun flushTable() {
        if (tableRows.isNotEmpty()) {
            blocks += MarkdownBlock.Table(tableRows.toList())
            tableRows.clear()
        }
    }

    markdown.lines().forEach { rawLine ->
        val line = rawLine.trim()

        if (line.isBlank()) {
            flushParagraph()
            flushTable()
            return@forEach
        }

        if (line.startsWith("|") && line.endsWith("|")) {
            flushParagraph()
            val cells = parseTableCells(line)
            if (!isTableSeparator(cells)) {
                tableRows += cells
            }
            return@forEach
        }

        flushTable()

        when {
            line.startsWith("#") -> {
                flushParagraph()
                val level = line.takeWhile { it == '#' }.length
                blocks += MarkdownBlock.Heading(level, cleanInline(line.drop(level).trim()))
            }

            line.startsWith(">") -> {
                flushParagraph()
                blocks += MarkdownBlock.Quote(cleanInline(line.removePrefix(">").trim()))
            }

            line.startsWith("- ") -> {
                flushParagraph()
                blocks += MarkdownBlock.Bullet(cleanInline(line.removePrefix("- ").trim()))
            }

            numberedLineRegex.matches(line) -> {
                flushParagraph()
                val match = numberedLineRegex.matchEntire(line)!!
                blocks += MarkdownBlock.Numbered(
                    number = match.groupValues[1].toInt(),
                    text = cleanInline(match.groupValues[2]),
                )
            }

            else -> paragraph += line
        }
    }

    flushParagraph()
    flushTable()
    return blocks
}

private val numberedLineRegex = Regex("""^(\d+)\.\s+(.+)$""")
private val markdownLinkRegex = Regex("""\[([^\]]+)]\([^)]+\)""")

private fun parseTableCells(line: String): List<String> =
    line.trim('|').split('|').map { cleanInline(it.trim()) }

private fun isTableSeparator(cells: List<String>): Boolean =
    cells.isNotEmpty() && cells.all { cell -> cell.all { it == '-' || it == ':' || it.isWhitespace() } }

private fun cleanInline(value: String): String =
    value
        .replace(markdownLinkRegex, "$1")
        .replace("**", "")
        .replace("`", "")
