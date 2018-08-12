using System.Configuration;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;
using System.Data.Common;

namespace BOM.DbAccess
{
    public static class DbUtilities
    {
       public static DbConnection GetConnection() {
            DbConnection connection = null;

            if ("MySQL" == ConfigurationManager.AppSettings["DbType"])
            {
                connection =  new MySqlConnection(ConfigurationManager.ConnectionStrings["MySQLDB"].ToString());
                
               
            }
            else//SQL Server
            {
                connection = new SqlConnection(ConfigurationManager.ConnectionStrings["SQLServerDB"].ToString());
            }

            connection.Open();
            return connection;
        }
    }
}
