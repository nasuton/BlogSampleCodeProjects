using Microsoft.Data.SqlClient;
using System.Data;

internal class SQLCommit
{
    private SqlConnection sqlConnection;

    private string tableName = "dbo.TestTable";

    public SQLCommit()
    {
        var sqlBuilder = new SqlConnectionStringBuilder()
        {
            DataSource = "localhost or IPアドレス",
            InitialCatalog = "対象データベース",
            UserID = "ログインユーザー",
            Password = "UserIDのパスワード",
            TrustServerCertificate = true, // 証明書の検証を行わない
            Encrypt = true, // 暗号化を有効にする
        };
        sqlConnection = new SqlConnection(sqlBuilder.ConnectionString);
        sqlConnection.Open();
    }

    public void Dispose()
    {
        sqlConnection.Dispose();
    }

    public void GetDate()
    {
        //クエリ
        string query = $"SELECT * FROM {tableName}";
        SqlCommand comm = new SqlCommand(query, sqlConnection);
        comm.CommandType = CommandType.Text;
        using (SqlDataReader dr = comm.ExecuteReader())
        {
            //値取得
            while (dr.Read())
            {
                Console.WriteLine($"{dr["name"]}, {dr["age"]}");
            }
        }
    }

    public void InsertData(string name, int age)
    {
        using(SqlCommand comm = sqlConnection.CreateCommand())
        {
            //トランザクションの開始
            comm.Transaction = sqlConnection.BeginTransaction();

            //クエリ
            string query = $"INSERT INTO {tableName} (name, age) VALUES (@Name, @Age)";
            comm.CommandText = query;
            comm.CommandType = CommandType.Text;
            comm.Parameters.Add("@Name", SqlDbType.NVarChar).Value = name;
            comm.Parameters.Add("@Age", SqlDbType.Int).Value = age;
            try
            {
                comm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                //ロールバック
                comm.Transaction.Rollback();
                Console.WriteLine(ex.Message);
                return;
            }

            //Parameterのクリア
            comm.Parameters.Clear();

            //変更を確定する
            comm.Transaction.Commit();
        }
    }
}

try
{
    var sqlCommit = new SQLCommit();
    sqlCommit.InsertData("テスト", 30);
    sqlCommit.GetDate();
    sqlCommit.Dispose();
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}