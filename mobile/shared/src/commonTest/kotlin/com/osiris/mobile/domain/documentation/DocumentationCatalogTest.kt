package com.osiris.mobile.domain.documentation

import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertNotNull
import kotlin.test.assertTrue

class DocumentationCatalogTest {
    @Test
    fun catalogContainsAllPublishedGuides() {
        val slugs = DocumentationCatalog.guides.map { it.slug }

        assertEquals(
            listOf(
                "getting-started",
                "dashboard",
                "categories",
                "accounts",
                "cards",
                "purchases",
                "statements",
                "bills",
                "reports",
            ),
            slugs,
        )
    }

    @Test
    fun accountGuideKeepsMarkdownContent() {
        val guide = assertNotNull(DocumentationCatalog.find("accounts"))

        assertEquals("Guia simples de contas", guide.title)
        assertTrue(guide.markdown.contains("# Guia simples de contas"))
        assertTrue(guide.markdown.contains("Saldo inicial e saldo atual"))
    }
}
