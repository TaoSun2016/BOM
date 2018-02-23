using System;
using System.Configuration;
using System.Data.SqlClient;

namespace BOM.Models
{
    public static class DBConnection
    {
        public static SqlConnection OpenConnection()
        {
            log4net.ILog log = log4net.LogManager.GetLogger("DBConnection");
            SqlConnection connection = null;
            try
            {
                connection = new SqlConnection { ConnectionString = ConfigurationManager.ConnectionStrings["BOMDB"].ToString() };
                connection.Open();
            }
            catch (Exception e)
            {
                log.Error($"Connect to database error!\nErrorMsg[{e.Message}]\nErrStack[{e.StackTrace}]");
                throw;
            }

            return connection;
        }
        public static void CloseConnection(SqlConnection sqlConnection)
        {
            sqlConnection.Close();
        }
    }
}
