package org.m365graphapi

import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertFalse
import kotlin.test.assertTrue

class MainTest {
    @Test
    fun `runApp prints site list and items when all data is available`() {
        val outputs = mutableListOf<String>()
        val dataSource = FakeGraphDataSource(
            site = SiteInfo(
                id = "site-123",
                displayName = "Demo Site",
                webUrl = "https://example.com/sites/demo"
            ),
            list = ListInfo(
                id = "list-456",
                displayName = "TestList",
                webUrl = "https://example.com/sites/demo/lists/Test"
            ),
            items = listOf(
                ListItemInfo(
                    id = "item-1",
                    fields = linkedMapOf(
                        "Title" to "Alpha",
                        "Count" to 3
                    )
                ),
                ListItemInfo(
                    id = "item-2",
                    fields = null
                )
            )
        )

        val result = runApp(dataSource, "ドメイン.sharepoint.com:/sites/Test", "TestList") { outputs += it }

        assertTrue(result)
        assertEquals(
            listOf(
                "Site ID   : site-123",
                "Site Name : Demo Site",
                "Web URL   : https://example.com/sites/demo",
                "List ID   : list-456",
                "List Name : StatusHistory",
                "Web URL   : https://example.com/sites/demo/lists/TestList",
                "=== Item 1 (ID: item-1) ===",
                "  Title : Alpha",
                "  Count : 3",
                "=== Item 2 (ID: item-2) ===",
                "  (fields is null)"
            ),
            outputs
        )
        assertEquals("ドメイン.sharepoint.com:/sites/Test", dataSource.sitePathRequested)
        assertEquals("site-123", dataSource.listSiteIdRequested)
        assertEquals("TestList", dataSource.listDisplayNameRequested)
        assertEquals("site-123", dataSource.itemsSiteIdRequested)
        assertEquals("list-456", dataSource.itemsListIdRequested)
    }

    @Test
    fun `runApp stops when site cannot be resolved`() {
        val outputs = mutableListOf<String>()
        val dataSource = FakeGraphDataSource(site = null)

        val result = runApp(dataSource, "site-path", "Testリスト") { outputs += it }

        assertFalse(result)
        assertEquals(listOf("Failed to retrieve Site ID."), outputs)
        assertEquals("site-path", dataSource.sitePathRequested)
        assertEquals(null, dataSource.listSiteIdRequested)
        assertEquals(null, dataSource.itemsSiteIdRequested)
    }

    @Test
    fun `runApp stops when list cannot be resolved`() {
        val outputs = mutableListOf<String>()
        val dataSource = FakeGraphDataSource(
            site = SiteInfo(
                id = "site-123",
                displayName = "Demo Site",
                webUrl = "https://example.com/sites/demo"
            ),
            list = null
        )

        val result = runApp(dataSource, "site-path", "Testリスト") { outputs += it }

        assertFalse(result)
        assertEquals(
            listOf(
                "Site ID   : site-123",
                "Site Name : Demo Site",
                "Web URL   : https://example.com/sites/demo",
                "Failed to retrieve List ID."
            ),
            outputs
        )
        assertEquals("site-path", dataSource.sitePathRequested)
        assertEquals("site-123", dataSource.listSiteIdRequested)
        assertEquals("Testリスト", dataSource.listDisplayNameRequested)
        assertEquals(null, dataSource.itemsSiteIdRequested)
    }
}

private class FakeGraphDataSource(
    private val site: SiteInfo?,
    private val list: ListInfo? = null,
    private val items: List<ListItemInfo> = emptyList()
) : GraphDataSource {
    var sitePathRequested: String? = null
        private set
    var listSiteIdRequested: String? = null
        private set
    var listDisplayNameRequested: String? = null
        private set
    var itemsSiteIdRequested: String? = null
        private set
    var itemsListIdRequested: String? = null
        private set

    override fun getSite(sitePath: String): SiteInfo? {
        sitePathRequested = sitePath
        return site
    }

    override fun getList(siteId: String, listDisplayName: String): ListInfo? {
        listSiteIdRequested = siteId
        listDisplayNameRequested = listDisplayName
        return list
    }

    override fun getListItems(siteId: String, listId: String): List<ListItemInfo> {
        itemsSiteIdRequested = siteId
        itemsListIdRequested = listId
        return items
    }
}

