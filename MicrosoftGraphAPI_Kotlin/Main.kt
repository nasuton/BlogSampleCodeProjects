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

    val resolvedSiteId = getSiteId(graphClient, siteId)
    if (resolvedSiteId != null) {
        val resolvedListId = getLisdId(graphClient, resolvedSiteId,listDisplayName)
        if (resolvedListId != null) {
            getListItems(graphClient, resolvedSiteId, resolvedListId)
        } else {
            println("Failed to retrieve List ID.")
        }
    } else {
        println("Failed to retrieve Site ID.")
    }
}

fun getSiteId(graphClient: GraphServiceClient, siteId: String): String? {
    val site = graphClient.sites().bySiteId(siteId).get()
    println("Site ID   : ${site?.id}")
    println("Site Name : ${site?.displayName}")
    println("Web URL   : ${site?.webUrl}")
    return site?.id
}

fun getLisdId(graphClient: GraphServiceClient, siteId: String, listId: String): String? {
    val list = graphClient.sites().bySiteId(siteId).lists().byListId(listId).get()
    println("List ID   : ${list?.id}")
    println("List Name : ${list?.displayName}")
    println("Web URL   : ${list?.webUrl}")
    return list?.id
}

fun getListItems(graphClient: GraphServiceClient, siteId: String, listId: String) {
    val items = graphClient.sites().bySiteId(siteId).lists().byListId(listId).items().get { requestConfiguration ->
        requestConfiguration.queryParameters?.expand = arrayOf("fields")
    }

    items?.value?.forEachIndexed { index, item ->
        println("=== Item ${index + 1} (ID: ${item.id}) ===")
        val fields = item.fields
        if (fields != null) {
            fields.additionalData.forEach { (key, value) ->
                println("  $key : $value")
            }
        } else {
            println("  (fields is null)")
        }
    }
}
