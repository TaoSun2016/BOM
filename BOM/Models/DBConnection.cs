using System.Configuration;
using System.Data.SqlClient;

namespace BOM.Models
{
    public static class DBConnection
    {
        public static SqlConnection OpenConnection()
        {
            
            //SqlConnection DBConnection =  new SqlConnection { ConnectionString = ConfigurationManager.ConnectionStrings["BOMDB"].ToString() };
            SqlConnection DBConnection = new SqlConnection { ConnectionString = ConfigurationManager.ConnectionStrings["MAT"].ToString() };
            DBConnection.Open();
            return DBConnection;
        }
        public static void CloseConnection(SqlConnection sqlConnection)
        {
            sqlConnection.Close();
        }
    }
}
