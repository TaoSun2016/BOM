using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;

namespace DBAccess.ConnectedLayer
{
    public class AttrDefineOperation
    {
         

        public void Insert(string tmpId, string attrId, string attrNm, char attrTp, string crter)
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
        public void Delete(string tmpId, string attrId, string attrNm, char attrTp)
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

        public void Update(string oldTmpId, string oldAttrId, string oldAttrNm, char oldAttrTp, string tmpId, string attrId, string attrNm, char attrTp, string updter)
        {
            SqlConnection sqlConnection = DBConnection.OpenConnection();
            string updtDate = DateTime.Now.ToString("yyyyMMdd HH:mm:ss");

            string sql = $"UPDATE AttrDefine SET TmpId = '{tmpId}' AND AttrId='{attrId}' AND AttrNm='{attrNm}' AND AttrTp='{attrTp}' AND LstUpdtDate='{updtDate}' AND LstUpdter='{updter}'  WHERE TmpId = '{oldTmpId}' AND AttrId='{oldAttrId}' AND AttrNm='{oldAttrNm}' AND AttrTp='{oldAttrTp}' AND LockFlag='0'";

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
                        TmpId = (string)dataReader["TmpId"],
                        AttrId = (string)dataReader["AttrId"],
                        AttrNm = (string)dataReader["AttrNm"],
                        AttrTp = (string)dataReader["AttrTp"],
                        LockFlag = (int)dataReader["LockFlag"],
                        CrtDate = (DateTime)dataReader["CrtDate"],
                        Crter = (string)dataReader["Crter"],
                        LstUpdtDate = (DateTime)dataReader["LstUpdtDate"],
                        LstUpdter = (string)dataReader["LstUpdter"],

                    });
                }
                dataReader.Close();
            }
            DBConnection.CloseConnection(sqlConnection);
            return list;
            
        }
    }
}

