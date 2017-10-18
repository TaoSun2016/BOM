using System;
using System.Data.SqlClient;

namespace BOM.Models
{
    public partial class SeqNo
    {
        public string Ind_Key { get; set; }
        public long Next_To { get; set; }

        public string GetSeqNo(string tmpId)
        {
            int result = 0;
            string seqNo = null;
            log4net.ILog log = log4net.LogManager.GetLogger("SeqNo");
            SqlConnection sqlConnection = DBConnection.OpenConnection();
            string sql = $"SELECT * FROM SEQ_NO WHERE Ind_Key = '{tmpId}'";
            using (SqlCommand command = new SqlCommand(sql, sqlConnection))
            {
                SqlDataReader dataReader = command.ExecuteReader();


                if (dataReader.HasRows)
                {
                    dataReader.Read();
                    this.Ind_Key = dataReader["IND_KEY"].ToString();

                    this.Next_To = (long)dataReader["NEXT_NO"];
                    dataReader.Close();
                    this.Next_To += 1;
                    sql = $"UPDATE SEQ_NO SET NEXT_NO = {this.Next_To} WHERE IND_KEY = '{this.Ind_Key}'";
                }
                else
                {
                    this.Ind_Key = tmpId;
                    this.Next_To = 1;
                    dataReader.Close();
                    this.Next_To += 1;
                    sql = $"INSERT INTO SEQ_NO VALUES ('{this.Ind_Key}',{this.Next_To}";
                }
                seqNo = Convert.ToString(this.Next_To,8)+"9";
                command.CommandText = sql;

                result = command.ExecuteNonQuery();
                if (result == 0)
                {
                    DBConnection.CloseConnection(sqlConnection);
                    throw new Exception("update/insert table seq_no error!");
                }
            }

            DBConnection.CloseConnection(sqlConnection);
            return seqNo;
        }
    }
}
