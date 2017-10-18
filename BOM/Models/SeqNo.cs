using System;
using System.Data.SqlClient;

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

            SqlConnection sqlConnection = DBConnection.OpenConnection();
            SqlTransaction sqlTransaction = sqlConnection.BeginTransaction();
            string sql = @"SELECT * FROM SEQ_NO WHERE Ind_Key = '0'";


            using (SqlCommand command = new SqlCommand(sql, sqlConnection))
            {

                command.Transaction = sqlTransaction;
                SqlDataReader dataReader = command.ExecuteReader();

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
                    DBConnection.CloseConnection(sqlConnection);
                    throw new Exception("There is no base parameter!");
                }

                try
                {
                    command.CommandText = sql;
                    result = command.ExecuteNonQuery();
                    if (result == 0)
                    {
                        sqlTransaction.Rollback();
                        DBConnection.CloseConnection(sqlConnection);
                        throw new Exception("update table seq_no error!");
                    }

                    sql = $"INSERT INTO SEQ_NO VALUES ('{seqNo}',0)";
                    command.CommandText = sql;
                    result = command.ExecuteNonQuery();
                    if (result == 0)
                    {
                        sqlTransaction.Rollback();
                        DBConnection.CloseConnection(sqlConnection);
                        throw new Exception("insert table seq_no error!");
                    }
                }
                catch
                {
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
                    throw;
                }
                sqlTransaction.Commit();
            }

            DBConnection.CloseConnection(sqlConnection);
            return seqNo;
        }

        public long GetSubSeqNo(string tmpId)
        {
            int result = 0;
            long seqNo = 0;

            SqlConnection sqlConnection = DBConnection.OpenConnection();
            string sql = $"SELECT * FROM SEQ_NO WHERE Ind_Key = '{tmpId}'";

            using (SqlCommand command = new SqlCommand(sql, sqlConnection))
            {
                SqlDataReader dataReader = command.ExecuteReader();

                if (dataReader.HasRows)
                {
                    dataReader.Read();

                    this.Ind_Key = dataReader["IND_KEY"].ToString();
                    this.Next_No = (long)dataReader["NEXT_NO"];

                    dataReader.Close();

                    seqNo = this.Next_No;
                    this.Next_No += 1;
                    sql = $"UPDATE SEQ_NO SET NEXT_NO = {this.Next_No} WHERE IND_KEY = '{tmpId}'";
                }
                else
                {
                    log.Error(string.Format($"找不到物料参数记录,TmpID=[{tmpId}]"));
                    dataReader.Close();
                    DBConnection.CloseConnection(sqlConnection);
                    throw new Exception(string.Format($"找不到物料参数记录,TmpID=[{tmpId}]"));
                }

                command.CommandText = sql;
                result = command.ExecuteNonQuery();
                if (result == 0)
                {
                    log.Error($"更新序号表出错,TmpID=[{tmpId}]");
                    DBConnection.CloseConnection(sqlConnection);
                    throw new Exception($"更新序号表出错,TmpID=[{tmpId}]");
                }

            }

            DBConnection.CloseConnection(sqlConnection);
            return seqNo;
        }
    }
}
