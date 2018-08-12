using System.Configuration;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;
using System.Data.Common;

namespace Test.DbAccess
{
    public static class DbUtilities
    {
       public static DbConnection GetConnect() {

            if ("MySQL" == ConfigurationManager.AppSettings["DbType"])
            {
                return new MySqlConnection(ConfigurationManager.ConnectionStrings["MySQLDB"].ToString());
            }
            else//SQL Server
            {
                return new SqlConnection(ConfigurationManager.ConnectionStrings["SQLServerDB"].ToString());
            }
        }
    }
}
