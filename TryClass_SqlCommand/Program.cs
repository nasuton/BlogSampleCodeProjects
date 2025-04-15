using Microsoft.Data.SqlClient;

try
{
    string sqlServerName = "your_sql_server_name";
    string dbName = "your_database_name";
    string userID = "your_user_id";
    string password = "your_password";

    using (var sqlDataAccess = new SqlDataAccess(sqlServerName, dbName, false, userID, password))
    {
        // 対象テーブルの存在確認
        string tableName = "TestTable002";
        if(!sqlDataAccess.ExistTableInDB(tableName))
        {
            // テーブルが存在しない場合は作成
            string createTableQuery = $"CREATE TABLE {tableName} (Id INT PRIMARY KEY IDENTITY(1,1), Name NVARCHAR(50), Age INT, ModifiedDate DATETIME)";
            sqlDataAccess.ExecuteNonQuery(createTableQuery);
        }

        // データの挿入
        string insertQuery = $"INSERT INTO {tableName} (Name, Age, ModifiedDate) VALUES (@Value1, @Value2, SYSDATETIME())";
        var insertParameters = new List<SqlParameter>
                {
                    new SqlParameter("@Value1", "Test001"),
                    new SqlParameter("@Value2", "12")
                };
        int rowsAffected = sqlDataAccess.ExecuteNonQuery(insertQuery, insertParameters);
        Console.WriteLine($"Rows affected by insert: {rowsAffected}");

        // データの取得
        string selectQuery = $"SELECT * FROM {tableName}";
        sqlDataAccess.ExecuteReader(selectQuery, null, reader =>
        {
            while (reader.Read())
            {
                Console.WriteLine($"Name: {reader["Name"]}, Age: {reader["Age"]}");
            }
        });
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}
