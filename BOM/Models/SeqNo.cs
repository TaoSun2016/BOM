using System;
using System.Data.SqlClient;
using BOM.DbAccess;
using System.Data.Common;

namespace BOM.Models
{
    public partial class SeqNo
    {
        public string Ind_Key { get; set; }
        public long Next_No { get; set; }

        log4net.ILog log = log4net.LogManager.GetLogger("SeqNo");
        public string GetBaseSeqNo()
        {
            int result = 0;
            string seqNo = null;

            //SqlConnection sqlConnection = DBConnection.OpenConnection();
            //SqlTransaction sqlTransaction = sqlConnection.BeginTransaction();

            DbConnection connection = DbUtilities.GetConnection();
            DbTransaction transaction = connection.BeginTransaction();



            string sql = @"SELECT * FROM SEQ_NO WHERE Ind_Key = '0' FOR UPDATE";

            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = sql;
                command.Transaction = transaction;
                DbDataReader dataReader = command.ExecuteReader();

                if (dataReader.HasRows)
                {
                    dataReader.Read();

                    this.Ind_Key = dataReader["IND_KEY"].ToString();
                    this.Next_No = (long)dataReader["NEXT_NO"];

                    dataReader.Close();

                    seqNo = Convert.ToString(this.Next_No, 8) + "9";
                    this.Next_No += 1;
                    sql = $"UPDATE SEQ_NO SET NEXT_NO = {this.Next_No} WHERE IND_KEY = '0'";
                }
                else
                {
                    dataReader.Close();
                    transaction.Rollback();
                    connection.Close();
                    throw new Exception("There is no base parameter!");
                }

                try
                {
                    command.CommandText = sql;
                    result = command.ExecuteNonQuery();
                    if (result == 0)
                    {
                        transaction.Rollback();
                        connection.Close();
                        throw new Exception("update table seq_no error!");
                    }                    
                }
                catch
                {
                    transaction.Rollback();
                    connection.Close();
                    throw;
                }
                transaction.Commit();
            }

            connection.Close();
            return seqNo;
        }

        public string GetSubSeqNo(string tmpId)
        {
            int result = 0;
            string seqNo = null;

            DbConnection connection = DbUtilities.GetConnection();
            string sql = $"SELECT * FROM SEQ_NO WHERE Ind_Key = '{tmpId}' FOR UPDATE";
            DbTransaction transaction = connection.BeginTransaction();
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = sql;
                command.Transaction = transaction;
                DbDataReader dataReader = command.ExecuteReader();

                if (dataReader.HasRows)
                {
                    dataReader.Read();

                    this.Ind_Key = dataReader["IND_KEY"].ToString();
                    this.Next_No = (long)dataReader["NEXT_NO"];

                    dataReader.Close();

                    seqNo = Convert.ToString(this.Next_No, 8);
                    this.Next_No += 1;
                    sql = $"UPDATE SEQ_NO SET NEXT_NO = {this.Next_No} WHERE IND_KEY = '{tmpId}'";
                }
                else
                {
                    log.Error(string.Format($"找不到物料参数记录,TmpID=[{tmpId}]"));
                    dataReader.Close();
                    transaction.Rollback();
                    connection.Close();
                    throw new Exception(string.Format($"找不到物料参数记录,TmpID=[{tmpId}]"));
                }

                try
                {
                    command.CommandText = sql;
                    result = command.ExecuteNonQuery();
                    if (result == 0)
                    {
                        log.Error($"更新序号表出错,TmpID=[{tmpId}]");
                        transaction.Rollback();
                        connection.Close();
                        throw new Exception($"更新序号表出错,TmpID=[{tmpId}]");
                    }
                }
                catch
                {
                    transaction.Rollback();
                    connection.Close();
                    throw;
                }
                transaction.Commit();
            }
            connection.Close();
            return seqNo;
        }
    }
}
