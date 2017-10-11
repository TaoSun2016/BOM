using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace BOM.Models
{
    public class AttrDefineOperation
    {       
        public void Insert(string tmpId, string attrId, string attrNm, string attrTp, string crter)
        {
            SqlConnection sqlConnection = DBConnection.OpenConnection();

            string crtDate = DateTime.Now.ToString("yyyyMMdd HH:mm:ss");
            string sql = "INSERT INTO AttrDefine " + $"(TmpId, AttrId, AttrNm, AttrTp,CrtDate, Crter) Values ('{tmpId}','{attrId}', '{attrNm}', '{attrTp}', '{crtDate}','{crter}')";
            using (SqlCommand command = new SqlCommand(sql, sqlConnection))
            {
                command.ExecuteNonQuery();
            }
            DBConnection.CloseConnection(sqlConnection);
        }
        public void Delete(string tmpId, string attrId, string attrNm, string attrTp)
        {
            SqlConnection sqlConnection = DBConnection.OpenConnection();
            string sql = $"DELETE FROM AttrDefine WHERE TmpId = '{tmpId}' AND AttrId='{attrId}' AND AttrNm='{attrNm}' AND AttrTp='{attrTp}'";
            using (SqlCommand command = new SqlCommand(sql, sqlConnection))
            {
                try
                {
                    command.ExecuteNonQuery();
                }
                catch (SqlException ex)
                {
                    Exception error = new Exception("Sorry! That car is on order!", ex);
                    throw error;
                }
            }
            DBConnection.CloseConnection(sqlConnection);

        }

        public void Update(string oldTmpId, string oldAttrId, string oldAttrNm, string oldAttrTp, string tmpId, string attrId, string attrNm, string attrTp, string updter)
        {
            SqlConnection sqlConnection = DBConnection.OpenConnection();
            string updtDate = DateTime.Now.ToString("yyyyMMdd HH:mm:ss");

            string sql = $"UPDATE AttrDefine SET TmpId = '{tmpId}' , AttrId='{attrId}' , AttrNm='{attrNm}' , AttrTp='{attrTp}' , LstUpdtDate='{updtDate}' , LstUpdter='{updter}'  WHERE TmpId = '{oldTmpId}' AND AttrId='{oldAttrId}' AND AttrNm='{oldAttrNm}' AND AttrTp='{oldAttrTp}' AND LockFlag='0'";

            using (SqlCommand command = new SqlCommand(sql, sqlConnection))
            {
                command.ExecuteNonQuery();
            }
            DBConnection.CloseConnection(sqlConnection);
        }

        public void Lock( string tmpId, string attrId, string attrNm, string attrTp, int lockFlag, string updter)
        {
            SqlConnection sqlConnection = DBConnection.OpenConnection();
            string updtDate = DateTime.Now.ToString("yyyyMMdd HH:mm:ss");

            string sql = $"UPDATE AttrDefine SET LockFlag = '{lockFlag}' WHERE TmpId = '{tmpId}' AND AttrId='{attrId}' AND AttrNm='{attrNm}' AND AttrTp='{attrTp}'";

            using (SqlCommand command = new SqlCommand(sql, sqlConnection))
            {
                command.ExecuteNonQuery();
            }
            DBConnection.CloseConnection(sqlConnection);
        }
        public List<AttrDefine> Query(string tmpId, string attrId, string attrNm, string attrTp)
        {
            SqlConnection sqlConnection = DBConnection.OpenConnection();
            
            List<AttrDefine> list = new List<AttrDefine>();
            
            string sql = "SELECT * FROM AttrDefine WHERE 0 = 0";
            if (tmpId != null)
            {
                sql += $" AND TmpId = '{tmpId}'";
            }
            if (attrId != null)
            {
                sql += $" AND AttrId = '{attrId}'";
            }
            if (attrNm != null)
            {
                sql += $" AND AttrNm = '{attrNm}'";
            }
            if (attrTp != null)
            {
                sql += $" AND AttrTp = '{attrTp}'";
            }
            using (SqlCommand command = new SqlCommand(sql, sqlConnection))
            {
                SqlDataReader dataReader= command.ExecuteReader();
                while (dataReader.Read())
                {
                    list.Add(new AttrDefine
                    {
                        TmpId = dataReader["TmpId"].ToString(),
                        AttrId = dataReader["AttrId"].ToString(),
                        AttrNm = dataReader["AttrNm"].ToString(),
                        AttrTp = dataReader["AttrTp"].ToString(),
                        LockFlag = (int)dataReader["LockFlag"],
                        CrtDate = dataReader["CrtDate"].ToString(),
                        Crter = dataReader["Crter"].ToString(),
                        LstUpdtDate =dataReader["LstUpdtDate"].ToString(),
                        LstUpdter = dataReader["LstUpdter"].ToString(),

                    });
                }
                dataReader.Close();
            }
            DBConnection.CloseConnection(sqlConnection);
            return list;
            
        }
    }
}

