using Microsoft.SharePoint.Client;
using System.Security;

namespace Csharp_SampleProjects.CSOM
{
    public class CSOMHelper
    {
        private string siteURL {  get; set; }

        private string userName {  get; set; }

        private SecureString password { get; set; }

        private int retryCount { get; set; }

        private int delay {  get; set; }

        private ClientContext context { get; set; }

        /// <summary>
        /// CSOMを使用する設定を含む
        /// </summary>
        /// <param name="_userName">CSOMでアクセスする際に使用するアカウント</param>
        /// <param name="_passWord">アカウントのパスワード</param>
        /// <param name="_retryCount">試行回数</param>
        /// <param name="_delay">試行する際の待機時間(単位：ミリ秒)</param>
        /// <param name="_targetSiteURL">対象となるサイトURL</param>
        public CSOMHelper(string _userName, string _passWord, int _retryCount, int _delay, string _targetSiteURL)
        {
            userName = _userName;
            password = new SecureString();
            _passWord.ToList().ForEach(password.AppendChar);
            retryCount = _retryCount;
            delay = _delay;
            siteURL = _targetSiteURL;

            using(var authenticationManager = new AuthenticationManager())
            {
                context = authenticationManager.GetContext(new Uri(siteURL), userName, password);
            }
            
        }

        /// <summary>
        /// ExecuteQueryが失敗した際に試行する
        /// </summary>
        private void ExecuteQueryWithIncrementalRetry()
        {
            int retryAttempts = 0;
            int backoffInterval = delay;

            if (retryCount <= 0)
            {
                throw new ArgumentException("Please specify a retry count greater than 0");
            }

            if (delay <= 0)
            {
                throw new ArgumentException("Please specify a delay value greater than 0");
            }

            while (retryAttempts < retryCount)
            {
                try
                {
                    context.ExecuteQuery();
                    return;
                }
                catch (Exception ex)
                {
                    string erroMessage = ex.ToString();
                    int intervalTime = (backoffInterval / 1000);
                    string message = $"{erroMessage} \n Retry after {intervalTime} seconds";
                    Console.WriteLine($"{message}");
                    Thread.Sleep(backoffInterval);
                    retryAttempts++;
                    backoffInterval = backoffInterval * 2;
                }
            }
            string throwMessage = $"Executed the maximum retry count, which is {retryCount} times"; 
            throw new Exception(throwMessage);
        }

        /// <summary>
        /// 対象となるリストの全アイテムを取得
        /// </summary>
        /// <param name="_targetListTitle">対象となるリスト名</param>
        /// <returns>取得した全リストアイテム</returns>
        public List<ListItem> GetAllListItem(string _targetListTitle)
        {
            // 結果格納用
            var result = new List<ListItem>();

            var list = context.Web.Lists.GetByTitle(_targetListTitle);
            context.Load(list);
            ExecuteQueryWithIncrementalRetry();

            int rowLimit = 500;
            ListItemCollectionPosition position = null;
            var camlQuery = new CamlQuery();
            camlQuery.ViewXml = @"<View Scope='RecursiveAll'><RowLimit Paged='TRUE'>" + rowLimit + "</RowLimit></View>";

            do
            {
                ListItemCollection listItems = null;
                camlQuery.ListItemCollectionPosition = position;
                listItems = list.GetItems(camlQuery);
                context.Load(listItems);
                ExecuteQueryWithIncrementalRetry();
                position = listItems.ListItemCollectionPosition;
                result.AddRange(listItems.ToList());
            }
            while (position != null);

            return result;
        }

        /// <summary>
        /// 現在アクセスしているサイトの全リストを取得。ただし、システムが作成したものは除く
        /// </summary>
        /// <returns>取得した全リスト</returns>
        public List<List> GetTargetSiteAllList()
        {
            ListCollection lists = context.Web.Lists;
            context.Load(lists);
            ExecuteQueryWithIncrementalRetry();

            var allList = new List<List>();

            foreach (List list in lists)
            {
                context.Load(list);
                ExecuteQueryWithIncrementalRetry();
                // システムが作成したリストは除く
                if ((list.IsCatalog == false) && (list.IsPrivate == false))
                {
                    allList.Add(list);
                }
            }

            return allList;
        }

        public static void Main()
        {
            var userName = "認証時に使用するユーザーアドレス";
            var password = "userNameのパスワード";
            var retryCount = 3;
            var delay = 1000;
            var targetSiteURL = "対象サイトURL";
            var targetListName = "対象リスト名";
            var csomHelper = new CSOMHelper(userName, password, retryCount, delay, targetSiteURL);
            var listItems = csomHelper.GetAllListItem(targetListName);
            foreach(var item in listItems)
            {
                Console.WriteLine($"リストアイテム名：{item["Title"]}");
            }

            var lists = csomHelper.GetTargetSiteAllList();
            foreach(var list in lists)
            {
                Console.WriteLine($"リスト名：{list.Title}");
            }
        }

    }
}
