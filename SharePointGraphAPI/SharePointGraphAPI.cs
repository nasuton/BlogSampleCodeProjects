using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// SharePointを操作する(GraphAPI版)
/// </summary>
namespace TestCSharp
{
    class SharePointGraphAPI
    {
        public SharePointGraphAPI()
        {
        }

        private readonly string tenantId = "テナントID";
        private readonly string clientId = "クライアントID";
        private readonly string userName = "認証時に使用するユーザーアドレス(二段階認証が設定されていないもの)";
        private readonly string userPass = "userNameのパスワード";
        // Azure上で設定したAPIアクセス許可
        private string[] scopes { get; } = { "Sites.ReadWrite.All" };

        private GraphServiceClient graphClient = null;

        /// <summary>
        /// GraphAPIを呼び出す際に使用するアクセス情報を取得
        /// </summary>
        private void GetAccessToken()
        {
            var options = new UsernamePasswordCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
            };

            // ユーザー認証でアクセスする
            var userNamePasswordCredential = new UsernamePasswordCredential(userName, userPass, tenantId, clientId, options);

            graphClient = new GraphServiceClient(userNamePasswordCredential, scopes);
        }

        /// <summary>
        /// 対象サイトの全リストを取得
        /// </summary>
        /// <param name="_siteId">対象サイトのサイトID</param>
        public async Task GetAllLists(string _siteId)
        {
            try
            {
                GetAccessToken();
                // 対象のサイト情報を一旦取得し、その中に含まれるSiteIdを使用しないといけないため
                var site = await graphClient.Sites[_siteId].GetAsync();
                var lists = await graphClient.Sites[site.Id].Lists.GetAsync();
                foreach (var list in lists.Value)
                {
                    // リスト名、リスト作成日時、リスト作成者名
                    Console.WriteLine($"ListTitle : {list.DisplayName}, CreatedDateTime : {list.CreatedDateTime}, CreatedByUser : {list.CreatedBy.User.DisplayName}");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// 対象リストの全アイテムを取得
        /// </summary>
        /// <param name="_siteId">対象サイトのサイトID</param>
        /// <param name="_listDisplayName">対象のリスト表示名</param>
        public async Task GetListAllItems(string _siteId, string _listDisplayName)
        {
            try
            {
                GetAccessToken();
                // 対象のサイト情報を一旦取得し、その中に含まれるSiteIdを使用しないといけないため
                var site = await graphClient.Sites[_siteId].GetAsync();
                var listItems = await graphClient.Sites[site.Id].Lists[_listDisplayName].Items.GetAsync((requestConfiguration) =>
                {
                    // 指定したフィールドの値を取得
                    requestConfiguration.QueryParameters.Expand = new string[] { "fields($select=Title,ShortName)" };
                });

                foreach (var item in listItems.Value)
                {
                    // アイテム名、アイテム最終更新日、アイテム最終更新者
                    Console.WriteLine($"アイテム名 : {item.Fields.AdditionalData["Title"]}, LastModifiedDateTime : {item.LastModifiedDateTime}, LastModifiedByUser : {item.LastModifiedBy.User.DisplayName}");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// タイトル列と一致するものだけを取得する
        /// </summary>
        /// <param name="_siteId">対象サイトのサイトID</param>
        /// <param name="_listDisplayName">対象のリスト表示名</param>
        /// <param name="_itemTitle">取得したいリストアイテム名</param>
        /// <returns>取得したリストアイテム</returns>
        public async Task<ListItemCollectionResponse> GetListItem(string _siteId, string _listDisplayName, string _itemTitle)
        {
            ListItemCollectionResponse listItems = null;
            try
            {
                GetAccessToken();
                // 対象のサイト情報を一旦取得し、その中に含まれるSiteIdを使用しないといけないため
                var site = await graphClient.Sites[_siteId].GetAsync();
                listItems = await graphClient.Sites[site.Id].Lists[_listDisplayName].Items.GetAsync((requestConfiguration) =>
                {
                    // リストアイテムの全列情報を取得する
                    requestConfiguration.QueryParameters.Expand = new string[] { "fields" };
                    // 指定したフィールドの値が一致するものだけを取得
                    // フィルター対象の列がインデックスを作成していないとエラーとなる
                    requestConfiguration.QueryParameters.Filter = $"fields/Title eq '{_itemTitle}'";
                });

                if(listItems.Value.Count == 0)
                {
                    listItems = null;
                }
            }
            catch( Exception ex)
            {
                Console.WriteLine(ex.ToString());
                listItems = null;
            }

            return listItems;
        }

        /// <summary>
        /// リストアイテムを作成する
        /// </summary>
        /// <param name="_siteId">対象サイトのサイトID</param>
        /// <param name="_listDisplayName">対象のリスト表示名</param>
        /// <param name="_itemData">作成するリストアイテムデータ</param>
        public async Task CreateListItem(string _siteId, string _listDisplayName, Dictionary<string, object> _itemData)
        {
            try
            {
                GetAccessToken();
                // 対象のサイト情報を一旦取得し、その中に含まれるSiteIdを使用しないといけないため
                var site = await graphClient.Sites[_siteId].GetAsync();
                var requestBody = new ListItem
                {
                    Fields = new FieldValueSet
                    {
                        AdditionalData = _itemData,
                    },
                };

                var result = await graphClient.Sites[site.Id].Lists[_listDisplayName].Items.PostAsync(requestBody);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// 対象のリストアイテムを更新
        /// </summary>
        /// <param name="_siteId">対象サイトのサイトID</param>
        /// <param name="_listDisplayName">対象のリスト表示名</param>
        /// <param name="_itemId">更新対象リストアイテムID</param>
        /// <param name="_itemData">リストアイテム更新データ</param>
        public async Task UpdateListItem(string _siteId, string _listDisplayName, string _itemId, Dictionary<string, object> _itemData)
        {
            try
            {
                GetAccessToken();
                // 対象のサイト情報を一旦取得し、その中に含まれるSiteIdを使用しないといけないため
                var site = await graphClient.Sites[_siteId].GetAsync();
                var requestBody = new FieldValueSet
                {
                    AdditionalData = _itemData,
                };
                var result = await graphClient.Sites[site.Id].Lists[_listDisplayName].Items[_itemId].Fields.PatchAsync(requestBody);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// 対象のリストアイテムを削除
        /// </summary>
        /// <param name="_siteId">対象サイトのサイトID</param>
        /// <param name="_listDisplayName">対象のリスト表示名</param>
        /// <param name="_itemId">削除対象リストアイテムID</param>
        public async Task DeleteListItem(string _siteId, string _listDisplayName, string _itemId)
        {
            try
            {
                GetAccessToken();
                // 対象のサイト情報を一旦取得し、その中に含まれるSiteIdを使用しないといけないため
                var site = await graphClient.Sites[_siteId].GetAsync();
                await graphClient.Sites[site.Id].Lists[_listDisplayName].Items[_itemId].DeleteAsync();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }

    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var sharePointGraphApi = new SharePointGraphAPI();
            string listTitle = "対象リストの表示名";
            // siteId のフォーマット「サイトドメイン:/sites/サイトURL」
            // 以下、例
            var siteId = "test.sharepoint.com:/sites/testSite";
            // 取得対象リストアイテム名
            string itemTitle = "Japan";
            sharePointGraphApi.GetAllLists(siteId).Wait();
            sharePointGraphApi.GetListAllItems(siteId, listTitle).Wait();
            // 登録データ
            var createData = new Dictionary<string, object>
            {
                {
                    "Title" , "Italy"
                },
                {
                    "ShortName" , "IT"
                },
            };
            sharePointGraphApi.CreateListItem(siteId, listTitle, data).Wait();
            var item = sharePointGraphApi.GetListItem(siteId, listTitle, itemTitle).Result;
            if(item != null)
            {
                // 更新データ
                var updateData = new Dictionary<string, object>
                {
                    {
                        "Title" , "ItalyUpdate!!"
                    },
                };
                sharePointGraphApi.UpdateListItem(siteId, listTitle, item.Value[0].Id, updateData).Wait();

                sharePointGraphApi.DeleteListItem(siteId, listTitle, item.Value[0].Id).Wait();
            }
        }
    }
}
