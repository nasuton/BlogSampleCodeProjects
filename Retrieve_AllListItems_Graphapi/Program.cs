try {
    string client_id = "YourClientId";
    string tenant_id = "YourTenantId";
    string clientSecret = "YourClientSecret";
    string siteId = "YourSiteDomain.sharepoint.com:/sites/YourSiteName"; 
    var m365Helper = new M365Helper(client_id, tenant_id, clientSecret, siteId);
    var listItems = await m365Helper.GetListAllItems<ListItemFields>(
        "YourListName", 
        "Title");
    
} catch (Exception ex) {
    Console.WriteLine($"Error: {ex.Message}");
}