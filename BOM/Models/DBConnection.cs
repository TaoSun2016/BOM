using System.Configuration;
using System.Data.SqlClient;

namespace DBAccess.ConnectedLayer
{
    public static class DBConnection
    {
        public static SqlConnection OpenConnection()
        {
            
            SqlConnection DBConnection =  new SqlConnection { ConnectionString = ConfigurationManager.ConnectionStrings["BOMDB"].ToString() };
            DBConnection.Open();
            return DBConnection;
        }
        public static void CloseConnection(SqlConnection sqlConnection)
        {
            sqlConnection.Close();
        }
    }
}
