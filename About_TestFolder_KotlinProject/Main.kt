package org.m365graphapi

import com.azure.identity.ClientSecretCredentialBuilder
import com.microsoft.graph.serviceclient.GraphServiceClient

fun main() {
    val clientId = "YOUR_CLIENT_ID"
    val tenantId = "YOUR_TENANT_ID"
    val clientSecret = "YOUR_CLIENT_SECRET"
    val scopes = arrayOf("https://graph.microsoft.com/.default")
    val siteId = "ドメイン.sharepoint.com:/sites/HogeHugaPiyo"
    val listDisplayName = "Testリスト"

    val credential = ClientSecretCredentialBuilder()
        .clientId(clientId)
        .tenantId(tenantId)
        .clientSecret(clientSecret)
        .build()

    val graphClient = GraphServiceClient(credential, *scopes)

    runApp(GraphServiceGraphDataSource(graphClient), siteId, listDisplayName)
}

data class SiteInfo(
    val id: String?,
    val displayName: String?,
    val webUrl: String?
)

data class ListInfo(
    val id: String?,
    val displayName: String?,
    val webUrl: String?
)

data class ListItemInfo(
    val id: String?,
    val fields: Map<String, Any?>?
)

interface GraphDataSource {
    fun getSite(sitePath: String): SiteInfo?
    fun getList(siteId: String, listDisplayName: String): ListInfo?
    fun getListItems(siteId: String, listId: String): List<ListItemInfo>
}

class GraphServiceGraphDataSource(private val graphClient: GraphServiceClient) : GraphDataSource {
    override fun getSite(sitePath: String): SiteInfo? {
        val site = graphClient.sites().bySiteId(sitePath).get()
        return site?.let {
            SiteInfo(
                id = it.id,
                displayName = it.displayName,
                webUrl = it.webUrl
            )
        }
    }

    override fun getList(siteId: String, listDisplayName: String): ListInfo? {
        val list = graphClient.sites().bySiteId(siteId).lists().byListId(listDisplayName).get()
        return list?.let {
            ListInfo(
                id = it.id,
                displayName = it.displayName,
                webUrl = it.webUrl
            )
        }
    }

    override fun getListItems(siteId: String, listId: String): List<ListItemInfo> {
        val items = graphClient.sites().bySiteId(siteId).lists().byListId(listId).items().get { requestConfiguration ->
            requestConfiguration.queryParameters?.expand = arrayOf("fields")
        }

        return items?.value.orEmpty().map { item ->
            ListItemInfo(
                id = item.id,
                fields = item.fields?.additionalData?.toMap()
            )
        }
    }
}

fun runApp(
    graphDataSource: GraphDataSource,
    sitePath: String,
    listDisplayName: String,
    printer: (String) -> Unit = ::println
): Boolean {
    val site = graphDataSource.getSite(sitePath)
    val resolvedSiteId = site?.id ?: run {
        printer("Failed to retrieve Site ID.")
        return false
    }

    printSite(site, printer)

    val list = graphDataSource.getList(resolvedSiteId, listDisplayName)
    val resolvedListId = list?.id ?: run {
        printer("Failed to retrieve List ID.")
        return false
    }

    printList(list, printer)

    val items = graphDataSource.getListItems(resolvedSiteId, resolvedListId)
    printItems(items, printer)

    return true
}

private fun printSite(site: SiteInfo, printer: (String) -> Unit) {
    printer("Site ID   : ${site.id}")
    printer("Site Name : ${site.displayName}")
    printer("Web URL   : ${site.webUrl}")
}

private fun printList(list: ListInfo, printer: (String) -> Unit) {
    printer("List ID   : ${list.id}")
    printer("List Name : ${list.displayName}")
    printer("Web URL   : ${list.webUrl}")
}

private fun printItems(items: List<ListItemInfo>, printer: (String) -> Unit) {
    items.forEachIndexed { index, item ->
        printer("=== Item ${index + 1} (ID: ${item.id}) ===")
        val fields = item.fields
        if (fields != null) {
            fields.forEach { (key, value) ->
                printer("  $key : $value")
            }
        } else {
            printer("  (fields is null)")
        }
    }
}
