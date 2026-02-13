using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.Reflection;
using System.Text.Json;

internal class M365Helper
{
    private readonly string[] _scopes = { "https://graph.microsoft.com/.default" };
    private readonly string _clientId;
    private readonly string _tenantId;
    private readonly string _clientSecret;
    private readonly string _siteId;
    private readonly GraphServiceClient _graphClient;

    public M365Helper(string clientId, string tenantId, string clientSecret, string siteId)
    {
        _clientId = clientId;
        _tenantId = tenantId;
        _clientSecret = clientSecret;
        _graphClient = GetServiceClient();
        _siteId = GetSiteId(siteId).Result;
    }

    private GraphServiceClient GetServiceClient()
    {
        var options = new ClientSecretCredentialOptions
        {
            AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
        };

        var clientSecretCredential = new ClientSecretCredential(
            _tenantId, _clientId, _clientSecret, options);

        return new GraphServiceClient(clientSecretCredential, _scopes);
    }

    private async Task<string?> GetSiteId(string siteId)
    {
        var site = await _graphClient.Sites[siteId].GetAsync();
        return site.Id;
    }

    /// <summary>
    /// SharePointリストのアイテムを取得し、指定した型に変換して返却
    /// </summary>
    /// <typeparam name="T">変換先のクラス型</typeparam>
    /// <param name="listDisplayName">リストの表示名</param>
    /// <param name="fields">取得するフィールド名（カンマ区切り）</param>
    /// <returns>変換されたオブジェクトのリスト</returns>
    public async Task<List<T>> GetListAllItems<T>(string listDisplayName, string fields) where T : new()
    {
        var response = await _graphClient.Sites[_siteId].Lists[listDisplayName].Items.GetAsync((requestConfiguration) =>
        {
            requestConfiguration.QueryParameters.Expand = new string[] { $"fields($select={fields})" };
        });

        var listItems = new List<ListItem>();
        var pageIterator = PageIterator<ListItem, ListItemCollectionResponse>.CreatePageIterator(_graphClient, response, item =>
        {
            listItems.Add(item);
            return true;
        });

        await pageIterator.IterateAsync();

        return ConvertToType<T>(listItems);
    }
    
    /// <summary>
    /// ListItemのリストを指定した型のリストに変換
    /// </summary>
    private List<T> ConvertToType<T>(List<ListItem> listItems) where T : new()
    {
        var result = new List<T>();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var idProperty = properties.FirstOrDefault(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase));
        foreach (var item in listItems)
        {
            if (item.Fields?.AdditionalData == null)
                continue;

            var obj = new T();
            // IdプロパティがあればIdを設定
            if (idProperty != null && idProperty.CanWrite)
            {
                SetPropertyValue(obj, idProperty, item.Id);
            }

            foreach (var property in properties)
            {
                // Idプロパティは設定済みのためスキップ
                if (property.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                    continue;
                // プロパティ名と一致するフィールドを探す
                var fieldName = property.Name;
                if (item.Fields.AdditionalData.TryGetValue(fieldName, out var value))
                {
                    SetPropertyValue(obj, property, value);
                }
            }
            result.Add(obj);
        }

        return result;
    }

    /// <summary>
    /// プロパティに値を設定
    /// </summary>
    private void SetPropertyValue<T>(T obj, PropertyInfo property, object? value)
    {
        if (value == null)
            return;
            
        try
        {
            var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            // JsonElementの場合は適切な型に変換
            if (value is JsonElement jsonElement)
            {
                value = ConvertJsonElement(jsonElement, targetType);
            }
            if (value != null && targetType.IsAssignableFrom(value.GetType()))
            {
                property.SetValue(obj, value);
            }
            else if (value != null)
            {
                var convertedValue = Convert.ChangeType(value, targetType);
                property.SetValue(obj, convertedValue);
            }
        }
        catch
        {
            // 変換に失敗した場合は無視
        }
    }

    /// <summary>
    /// JsonElementを指定した型に変換
    /// </summary>
    private object? ConvertJsonElement(JsonElement element, Type targetType)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => targetType == typeof(DateTime) || targetType == typeof(DateTime?)
                ? DateTime.Parse(element.GetString()!)
                : element.GetString(),
            JsonValueKind.Number when targetType == typeof(int) || targetType == typeof(int?) => element.GetInt32(),
            JsonValueKind.Number when targetType == typeof(long) || targetType == typeof(long?) => element.GetInt64(),
            JsonValueKind.Number when targetType == typeof(double) || targetType == typeof(double?) => element.GetDouble(),
            JsonValueKind.Number when targetType == typeof(decimal) || targetType == typeof(decimal?) => element.GetDecimal(),
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString()
        };
    }
}