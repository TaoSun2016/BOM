using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;

namespace BOM.Models
{
    public class BOMTree
    {
        log4net.ILog log = log4net.LogManager.GetLogger("BOMTree");

        public void FindChildrenTree(ref List<NodeInfo> list, long tmpId, int level)
        {
            int result = 0;
            string sql = null;
            NodeInfo nodeInfo = new NodeInfo();



            SqlConnection sqlConnection = DBConnection.OpenConnection();
            SqlDataReader dataReader = null;
            SqlTransaction sqlTransaction = sqlConnection.BeginTransaction();
            using (SqlCommand command = new SqlCommand())
            {
                command.Connection = sqlConnection;
                command.Transaction = sqlTransaction;

                sql = $"SELECT TmpNm FROM TmpInfo WHERE TmpId = {tmpId}";
                command.CommandText = sql;

                try
                {
                    dataReader = command.ExecuteReader();
                    if (dataReader.HasRows)
                    {
                        nodeInfo.NodeLevel = level;
                        nodeInfo.TmpId = tmpId;
                        nodeInfo.TmpNm = dataReader[0].ToString();
                    }
                    else
                    {
                        log.Error(string.Format($"Select TmpInfo error!\nsql[{sql}]\nError"));
                        dataReader.Close();
                        DBConnection.CloseConnection(sqlConnection);
                        throw new Exception("No data found!");
                    }
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Select TmpInfo error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    dataReader.Close();
                    DBConnection.CloseConnection(sqlConnection);
                    throw;
                }

                sql = $"SELECT AttrId, AttrNm, AttrTp FROM AttrDefine WHERE TmpId = {tmpId} or TmpId = 0";
                command.CommandText = sql;

                try
                {
                    dataReader = command.ExecuteReader();
                    while (dataReader.Read())
                    {
                        nodeInfo.Attributes.Add(new TempletAttribute { Id=dataReader["AttrId"].ToString(),Name= dataReader["AttrNm"].ToString(), Type = dataReader["AttrTp"].ToString() });
                    }
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Select AttrDefine error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    dataReader.Close();
                    DBConnection.CloseConnection(sqlConnection);
                    throw;
                }
                dataReader.Close();
                list.Add(nodeInfo);


            }

            DBConnection.CloseConnection(sqlConnection);
        }
    }

    public class NodeInfo
    {
        public int NodeLevel { get; set; }
        public long TmpId { get; set; }
        public string TmpNm { get; set; }
        public List<TempletAttribute> Attributes { get; set; }
    }

    public class TempletAttribute
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
    }

}