using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace BOM.Models
{
    public class AttrDefineOperation
    {
        log4net.ILog log = log4net.LogManager.GetLogger("AttrDefineOperation");

        public void Insert(AttrDefine attrDefine)
        {
            SqlConnection sqlConnection = DBConnection.OpenConnection();

            string crtDate = DateTime.Now.ToString("yyyyMMdd HH:mm:ss");
            string sql = "INSERT INTO AttrDefine " + $"(TmpId, AttrId, AttrNm, AttrTp,CrtDate, Crter) Values ({attrDefine.TmpId},'{attrDefine.AttrId}', '{attrDefine.AttrNm}', '{attrDefine.AttrTp}', '{crtDate}','{attrDefine.Crter}')";
            using (SqlCommand command = new SqlCommand(sql, sqlConnection))
            {
                try
                {
                    command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    log.Error("INSERT INTO AttrDefine ERROR!", e);
                    DBConnection.CloseConnection(sqlConnection);
                    throw;
                }
            }
            DBConnection.CloseConnection(sqlConnection);
        }
        public void Delete(long tmpId, string attrId, string attrNm, string attrTp)
        {
            SqlConnection sqlConnection = DBConnection.OpenConnection();
            string sql = $"DELETE FROM AttrDefine WHERE TmpId = {tmpId} AND AttrId='{attrId}' AND AttrNm='{attrNm}' AND AttrTp='{attrTp}'";
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

        public void Update(long oldTmpId, string oldAttrId, string oldAttrNm, string oldAttrTp, long tmpId, string attrId, string attrNm, string attrTp, string lstupdter)
        {
            SqlConnection sqlConnection = DBConnection.OpenConnection();
            string updtDate = DateTime.Now.ToString("yyyyMMdd HH:mm:ss");

            string sql = $"UPDATE AttrDefine SET TmpId = {tmpId} , AttrId='{attrId}' , AttrNm='{attrNm}' , AttrTp='{attrTp}' , LstUpdtDate='{updtDate}' , LstUpdter='{lstupdter}'  WHERE TmpId = {oldTmpId} AND AttrId='{oldAttrId}' AND AttrNm='{oldAttrNm}' AND AttrTp='{oldAttrTp}' AND LockFlag='0'";

            using (SqlCommand command = new SqlCommand(sql, sqlConnection))
            {
                command.ExecuteNonQuery();
            }
            DBConnection.CloseConnection(sqlConnection);
        }

        public void Lock( long tmpId, string attrId, string attrNm, string attrTp, int lockFlag, string updter)
        {
            SqlConnection sqlConnection = DBConnection.OpenConnection();
            string updtDate = DateTime.Now.ToString("yyyyMMdd HH:mm:ss");

            string sql = $"UPDATE AttrDefine SET LockFlag = '{lockFlag}' WHERE TmpId = {tmpId} AND AttrId='{attrId}' AND AttrNm='{attrNm}' AND AttrTp='{attrTp}'";

            using (SqlCommand command = new SqlCommand(sql, sqlConnection))
            {
                command.ExecuteNonQuery();
            }
            DBConnection.CloseConnection(sqlConnection);
        }
        public List<AttrDefine> Query(long? tmpId = null, string attrId = null, string attrNm = null, string attrTp = null)
        {
            SqlConnection sqlConnection = DBConnection.OpenConnection();
            
            List<AttrDefine> list = new List<AttrDefine>();
            
            string sql = "SELECT * FROM AttrDefine WHERE 0 = 0";
            if (tmpId != null)
            {
                sql += $" AND TmpId = {tmpId}";
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
                        TmpId = (long)dataReader["TmpId"],
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
            if (list.Count == 0)
            {
                string logMessage = string.Format("未找到记录!TmpId[{0}]AttrId[{1}]AttrNm[{2}]AttrTp[{3}]", tmpId, attrId, attrNm, attrTp);
                log.Error(logMessage);
                var responseMessge = new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent(logMessage),
                    ReasonPhrase = "AttrDefine record not found"
                };
                throw new HttpResponseException(responseMessge);
            }
            else
            {
                return list;
            }
           
            
        }

        public AttrDefine QueryOne(long tmpId, string attrId, string attrNm, string attrTp)
        {
            
            SqlConnection sqlConnection = DBConnection.OpenConnection();

            string sql = "SELECT * FROM AttrDefine WHERE 0 = 0"
                       + $" AND TmpId = {tmpId}"            
                       + $" AND AttrId = '{attrId}'"
                       + $" AND AttrNm = '{attrNm}'"
                       + $" AND AttrTp = '{attrTp}'";
            AttrDefine attrDefine = null;
            using (SqlCommand command = new SqlCommand(sql, sqlConnection))
            {
                SqlDataReader dataReader = command.ExecuteReader();


                if (dataReader.HasRows)
                {
                    dataReader.Read();
                    attrDefine = new AttrDefine
                    {
                        TmpId = (long)dataReader["TmpId"],
                        AttrId = dataReader["AttrId"].ToString(),
                        AttrNm = dataReader["AttrNm"].ToString(),
                        AttrTp = dataReader["AttrTp"].ToString(),
                        LockFlag = (int)dataReader["LockFlag"],
                        CrtDate = dataReader["CrtDate"].ToString(),
                        Crter = dataReader["Crter"].ToString(),
                        LstUpdtDate = dataReader["LstUpdtDate"].ToString(),
                        LstUpdter = dataReader["LstUpdter"].ToString(),
                    };
                    dataReader.Close();
                }
                else {
                    dataReader.Close();
                    DBConnection.CloseConnection(sqlConnection);
                    string logMessage = string.Format("未找到记录!TmpId[{0}]AttrId[{1}]AttrNm[{2}]AttrTp[{3}]", tmpId, attrId, attrNm, attrTp);
                    log.Error(logMessage);
                    var responseMessge = new HttpResponseMessage(HttpStatusCode.NotFound)
                    {
                        Content = new StringContent(logMessage),
                        ReasonPhrase = "AttrDefine record not found"
                    };
                    throw new HttpResponseException(responseMessge);
                }
                
            }
            DBConnection.CloseConnection(sqlConnection);
            return attrDefine;

        }
    }
}

