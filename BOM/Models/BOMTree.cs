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

        //Open BOM Tree
        //测试暂时将tmpid设为string型
        public void FindChildrenTree(SqlConnection connection, ref List<NodeInfo> list, string tmpId, int rlSeqNo, int level)
        {
            string sql = null;
            NodeInfo nodeInfo = new NodeInfo();
            nodeInfo.Attributes = new List<TempletAttribute>();

            List<string> listCTmpId = new List<string>();
            List<int> listSeqNo = new List<int>();

            SqlDataReader dataReader = null;

            log.Info("=================================================================");
            log.Info($"tmpid = [{tmpId}] seqno=[{rlSeqNo}] level=[{level}]");
            using (SqlCommand command = new SqlCommand())
            {
                command.Connection = connection;
                //sql = $"SELECT TmpNm FROM TmpInfo WHERE TmpId = {tmpId}"';
                sql = $"SELECT TmpNm FROM TmpInfo WHERE TmpId = '{tmpId}'";
                command.CommandText = sql;

                try
                {
                    dataReader = command.ExecuteReader();
                    if (dataReader.HasRows)
                    {
                        dataReader.Read();
                        nodeInfo.NodeLevel = level;
                        nodeInfo.TmpId = tmpId;
                        nodeInfo.TmpNm = dataReader[0].ToString();
                        nodeInfo.rlSeqNo = rlSeqNo;
                        dataReader.Close();
                    }
                    else
                    {
                        log.Error(string.Format($"Select TmpInfo error!\nsql[{sql}]\nError"));
                        throw new Exception("No data found!");
                    }
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Select TmpInfo error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    dataReader.Close();
                    throw;
                }

                //Flag :0 default attribute 1 private attribute
                //sql = $"SELECT CASE TmpId WHEN 0 THEN 0 ELSE 1 END AS Flag, AttrId, AttrNm, AttrTp FROM AttrDefine WHERE TmpId = {tmpId} or TmpId = 0";
                sql = $"SELECT CASE TmpId WHEN '0' THEN '0' ELSE '1' END AS Flag, AttrId, AttrNm, AttrTp FROM AttrDefine WHERE TmpId = '{tmpId}' or TmpId = '0' ORDER BY FLAG, AttrId";
                command.CommandText = sql;

                try
                {
                    dataReader = command.ExecuteReader();
                    while (dataReader.Read())
                    {
                        //var v1 = dataReader["Flag"].ToString();
                        //var v2 = dataReader["AttrId"].ToString();
                        //var v3 = dataReader["AttrNm"].ToString();
                        //var v4 = dataReader["AttrTp"].ToString();

                        nodeInfo.Attributes.Add(new TempletAttribute { Flag = dataReader["Flag"].ToString(), Id = dataReader["AttrId"].ToString(), Name = dataReader["AttrNm"].ToString(), Type = dataReader["AttrTp"].ToString(), Value = "" });
                    }
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Select AttrDefine error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    dataReader.Close();
                    throw;
                }
                dataReader.Close();
                list.Add(nodeInfo);

                //sql = $"SELECT CTmpId, rlSeqNo FROM Relation WHERE TmpId = {tmpId} ORDER BY CTmpId, rlSeqNo";
                sql = $"SELECT CTmpId, rlSeqNo FROM Relation WHERE TmpId = '{tmpId}' ORDER BY CTmpId, rlSeqNo";
                command.CommandText = sql;

                try
                {
                    dataReader = command.ExecuteReader();
                    if (!dataReader.HasRows)
                    {
                        dataReader.Close();
                        return;
                    }
                    else
                    {

                        while (dataReader.Read())
                        {
                            listCTmpId.Add(dataReader["CTmpId"].ToString());
                            listSeqNo.Add(Convert.ToInt32(dataReader["rlSeqNo"].ToString()));
                                         
                        }
                        dataReader.Close();                       
                    }

                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Select AttrDefine error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    dataReader.Close();
                    throw;
                }

                for (int i = 0; i < listCTmpId.Count(); i++)
                {
                    FindChildrenTree(connection, ref list, listCTmpId[i], listSeqNo[i], level + 1);
                }
            }

        }

        public string ApplyCode(NodeInfo nodeInfo)
        {
            int result = -1;
            bool hasPrivateAttribute = false;
            string materielIdentification = null;
            string sql = null;
            StringBuilder insertBuilder = new StringBuilder($"INSERT INTO {nodeInfo.TmpId} (");
            StringBuilder insertValues = new StringBuilder($" ) VALUES (");
            SqlConnection sqlConnection = DBConnection.OpenConnection();
            SqlTransaction sqlTransaction = sqlConnection.BeginTransaction();
            SqlDataReader dataReader = null;
            using (SqlCommand command = new SqlCommand())
            {
                command.Connection = sqlConnection;
                command.Transaction = sqlTransaction;

                //If nodeInfo has private attribute
                var privateAttributesCount = nodeInfo.Attributes.Where(m => m.Flag == "1").Count();
                hasPrivateAttribute = (privateAttributesCount > 0);
                if (hasPrivateAttribute)
                {
                    //Check if the private attribute table exists
                    sql = $"SELECT COUNT(*) FROM SYSOBJECTS WHERE NAME = '{nodeInfo.TmpId}' ";
                    command.CommandText = sql;
                    try
                    {
                        result = (int)command.ExecuteScalar();
                    }
                    catch (Exception e)
                    {
                        log.Error(string.Format($"Look for talbe {nodeInfo.TmpId} Error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                        DBConnection.CloseConnection(sqlConnection);
                        throw;
                    }
                    if (result == 0)
                    {
                        log.Error(string.Format($"Table {nodeInfo.TmpId} doesn't exsit!\nsql[{sql}]\n"));
                        DBConnection.CloseConnection(sqlConnection);
                        throw new Exception($"Table {nodeInfo.TmpId} doesn't exsit!!");
                    }

                    //Check duplicated records
                    StringBuilder builder = new StringBuilder($"SELECT COUNT(*) FROM {nodeInfo.TmpId} WHERE");

                    int counter = 0;
                    var privateAttributes = nodeInfo.Attributes.Where(m => m.Flag == "1");
                    foreach (var attrbute in privateAttributes)
                    {
                        counter++;
                        if (attrbute.Type == "C")
                        {
                            builder.Append($" {attrbute.Id} = '{attrbute.Value}'");
                            insertBuilder.Append($" {attrbute.Id},");
                            insertValues.Append($" '{attrbute.Value}',");
                        }
                        else
                        {
                            builder.Append($" {attrbute.Id} = {attrbute.Value}");
                            insertBuilder.Append($" {attrbute.Id},");
                            insertValues.Append($" {attrbute.Value},");
                        }
                        
                        if (counter != privateAttributes.Count())
                        {
                            builder.Append($" AND");
                        }
                    }
                    sql = builder.ToString();

                    command.CommandText = sql;
                    try
                    {
                        result = (int)command.ExecuteScalar();
                    }
                    catch (Exception e)
                    {
                        log.Error(string.Format($"Select private attribute Error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                        DBConnection.CloseConnection(sqlConnection);
                        throw;
                    }
                    if (result > 0)
                    {
                        log.Error(string.Format($"The private attrbutes have already exsited!\nsql[{sql}]\n"));
                        DBConnection.CloseConnection(sqlConnection);
                        throw new Exception("The private attrbutes have already exsited!!");
                    }
                }

                //Get Materiel Identification

                sql = $"SELECT NEXT_NO FROM SEQ_NO WHERE Ind_Key = '{nodeInfo.TmpId}'";

                try
                {
                    command.CommandText = sql;
                    dataReader = command.ExecuteReader();

                    if (dataReader.HasRows)
                    {
                        dataReader.Read();

                        var tmp = dataReader[0];


                        long nextNo = Convert.ToInt64(dataReader[0].ToString());

                        dataReader.Close();

                        materielIdentification = $"{nodeInfo.TmpId}" + Convert.ToString(nextNo, 8);
                        nextNo += 1;
                        sql = $"UPDATE SEQ_NO SET NEXT_NO = {nextNo} WHERE IND_KEY = '{nodeInfo.TmpId}'";
                    }
                    else
                    {
                        log.Error(string.Format($"Can't find record in table SEQ_NO.[{sql}]"));
                        throw new Exception(string.Format($"Can't find record in table SEQ_NO.[{sql}]"));
                    }

                    command.CommandText = sql;
                    result = command.ExecuteNonQuery();
                    if (result == 0)
                    {
                        log.Error($"No record is updated,TmpID=[{nodeInfo.TmpId}]");
                        DBConnection.CloseConnection(sqlConnection);
                        throw new Exception($"No record is updated,TmpID=[{nodeInfo.TmpId}]");
                    }
                }
                catch (Exception e)
                {
                    log.Error($"Get MaterielIdentification Error,TmpID=[{nodeInfo.TmpId}]sql=[{sql}]error[{e.StackTrace}][{e}][{e.Message}]");
                    dataReader.Close();
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
                    throw;
                }

                //Register private attribute values
                if (hasPrivateAttribute)
                {
                    sql = insertBuilder.ToString() + $" materielIdentfication" + insertValues.ToString() + $" '{materielIdentification}')";

                    command.CommandText = sql;
                    try
                    {
                        result = command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        log.Error(string.Format($"Register private attribute Error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                        sqlTransaction.Rollback();
                        DBConnection.CloseConnection(sqlConnection);
                        throw;
                    }
                    if (result == 0)
                    {
                        log.Error(string.Format($"No private attrbutes is registered!\nsql[{sql}]\n"));
                        sqlTransaction.Rollback();
                        DBConnection.CloseConnection(sqlConnection);
                        throw new Exception("No private attrbutes is registered!");
                    }
                }

                //Register default attribute values
                //前台必须给所有的缺省属性赋值,没有值赋缺省值
                insertBuilder = new StringBuilder($"INSERT INTO DeafaultAttr (");
                insertValues = new StringBuilder($" ) VALUES (");
                var defaultAttributes = nodeInfo.Attributes.Where(m => m.Flag == "0");
                foreach (var defaultAttrbute in defaultAttributes)
                {
                    insertBuilder.Append($" {defaultAttrbute.Id},");
                    if (defaultAttrbute.Type == "C")
                    {
                        insertValues.Append($" '{defaultAttrbute.Value}',");
                    }
                    else
                    {
                        insertValues.Append($" {defaultAttrbute.Value},");
                    }                 
                }
                sql = insertBuilder.ToString() + $" materielIdentfication" + insertValues.ToString() + $" '{materielIdentification}')";

                command.CommandText = sql;
                try
                {
                    result = command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Register default attribute Error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
                    throw;
                }
                if (result == 0)
                {
                    log.Error(string.Format($"No default attrbutes is registered!\nsql[{sql}]\n"));
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
                    throw new Exception("No default attrbutes is registered!");
                }
                
                sqlTransaction.Commit();

            }
            DBConnection.CloseConnection(sqlConnection);
            return materielIdentification;
        }

        public void DeleteNode(long pMaterielId, long materielId, int rlSeqNo)
        {
            int result = -1;
            string sql = null;

            SqlConnection sqlConnection = DBConnection.OpenConnection();

            using (SqlCommand command = new SqlCommand())
            {
                command.Connection = sqlConnection;
                sql = $"DELETE FROM BOM WHERE materielIdentfication = '{pMaterielId}' AND CmId = '{materielId}' and rlSeqNo = {rlSeqNo}";
                command.CommandText = sql;
                try
                {
                    result = command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Delete from BOM Error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    DBConnection.CloseConnection(sqlConnection);
                    throw;
                }
                if (result == 0)
                {
                    log.Error(string.Format($"No record is deleted!\nsql[{sql}]\n"));
                    DBConnection.CloseConnection(sqlConnection);
                    throw new Exception("No record is deleted!");
                }
            }
            DBConnection.CloseConnection(sqlConnection);

        }
        public List<NodeInfo> Expand(NodeInfo node)
        {
            return new List<NodeInfo>();
        }

        public void Save(List<NodeInfo> nodes)
        {

        }

    }
    public class NodeInfo
    {
        public int NodeLevel { get; set; }
        public string TmpId { get; set; }
        public string TmpNm { get; set; }
        public int rlSeqNo { get; set; }
        public List<TempletAttribute> Attributes { get; set; }

        public NodeInfo()
        {
            Attributes = new List<TempletAttribute>();
        }
    }

    public class TempletAttribute
    {
        //0:缺省属性 1:私有属性
        public string Flag { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
    }


}