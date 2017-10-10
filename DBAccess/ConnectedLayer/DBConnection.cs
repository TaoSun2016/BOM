using System.Configuration;
using System.Data.SqlClient;

namespace DBAccess.ConnectedLayer
{
    public class DBConnection
    {
        public SqlConnection OpenConnection()
        {
            
            SqlConnection DBConnection =  new SqlConnection { ConnectionString = ConfigurationManager.ConnectionStrings["BOMDB"].ToString() };
            DBConnection.Open();
            return DBConnection;
        }
        public void CloseConnection(SqlConnection sqlConnection)
        {
            sqlConnection.Close();
        }
    }
}
