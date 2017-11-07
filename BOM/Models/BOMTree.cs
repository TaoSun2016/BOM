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
        public void FindChildrenTree(SqlConnection connection, ref List<NodeInfo> list, long tmpId, int rlSeqNo, int level)
        {
            string sql = null;
            NodeInfo nodeInfo = new NodeInfo();

            SqlDataReader dataReader = null;
            using (SqlCommand command = new SqlCommand())
            {
                command.Connection = connection;
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
                        nodeInfo.rlSeqNo = rlSeqNo;
                    }
                    else
                    {
                        log.Error(string.Format($"Select TmpInfo error!\nsql[{sql}]\nError"));
                        dataReader.Close();
                        throw new Exception("No data found!");
                    }
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Select TmpInfo error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    dataReader.Close();
                    throw;
                }

                sql = $"SELECT CASE TmpId WHEN 0 THEN 0 ELSE 1 END AS Flag, AttrId, AttrNm, AttrTp FROM AttrDefine WHERE TmpId = {tmpId} or TmpId = 0";
                command.CommandText = sql;

                try
                {
                    dataReader = command.ExecuteReader();
                    while (dataReader.Read())
                    {
                        nodeInfo.Attributes.Add(new TempletAttribute { Flag = (int)dataReader["Flag"], Id = dataReader["AttrId"].ToString(), Name = dataReader["AttrNm"].ToString(), Type = dataReader["AttrTp"].ToString() });
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

                sql = $"SELECT CTmpId, rlSeqNo FROM Relation WHERE TmpId = {tmpId} ORDER BY CTmpId, rlSeqNo";
                command.CommandText = sql;

                try
                {
                    dataReader = command.ExecuteReader();
                    if (!dataReader.HasRows)
                    {
                        return;
                    }
                    else
                    {
                        while (dataReader.Read())
                        {
                            FindChildrenTree(connection, ref list, (long)dataReader["CTmpId"],  (int)dataReader["rlSeqNo"], level + 1);
                        }
                    }

                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Select AttrDefine error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    dataReader.Close();
                    throw;
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
            using (SqlCommand command = new SqlCommand())
            {
                command.Connection = sqlConnection;
                command.Transaction = sqlTransaction;

                //If nodeInfo has private attribute
                var privateAttributes = nodeInfo.Attributes.Where(m => m.Flag == 1);
                hasPrivateAttribute = (privateAttributes.Count() > 0);
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
                    StringBuilder builder = new StringBuilder($"SELECT COUNT(*) FROM '{nodeInfo.TmpId}' WHERE");

                    int counter = 0;
                    foreach (var attrbute in privateAttributes)
                    {
                        counter++;
                        builder.Append($" {attrbute.Id} = {attrbute.Value}");
                        insertBuilder.Append($" {attrbute.Id},");
                        insertValues.Append($" '{attrbute.Value}',");
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

                sql = $"SELECT * FROM SEQ_NO WHERE Ind_Key = '{nodeInfo.TmpId}'";

                try
                {
                    SqlDataReader dataReader = command.ExecuteReader();

                    if (dataReader.HasRows)
                    {
                        dataReader.Read();


                        long nextNo = (long)dataReader["NEXT_NO"];

                        dataReader.Close();

                        materielIdentification = $"{nodeInfo.TmpId}" + Convert.ToString(nextNo, 8);
                        nextNo += 1;
                        sql = $"UPDATE SEQ_NO SET NEXT_NO = {nextNo} WHERE IND_KEY = '{nodeInfo.TmpId}'";
                    }
                    else
                    {
                        log.Error(string.Format($"Can't find record in table SEQ_NO.[{sql}]"));
                        dataReader.Close();
                        DBConnection.CloseConnection(sqlConnection);
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
                catch
                {
                    log.Error($"Get MaterielIdentification Error,TmpID=[{nodeInfo.TmpId}]sql=[{sql}]");
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
                    throw;
                }


                //Register private attribute values
                if (hasPrivateAttribute)
                {
                    sql = insertBuilder.ToString() + $" materielIdentification)" + insertValues.ToString() + $" {materielIdentification}";

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
                var defaultAttributes = nodeInfo.Attributes.Where(m => m.Flag == 0);
                foreach (var defaultAttrbute in defaultAttributes)
                {
                    insertBuilder.Append($" {defaultAttrbute.Id},");
                    insertValues.Append($" '{defaultAttrbute.Value}',");
                }
                sql = insertBuilder.ToString() + $" materielIdentification)" + insertValues.ToString() + $" {materielIdentification}";

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
                sql = $"DELETE FROM BOM WHERE materielIdentification = {pMaterielId} AND CmId = {materielId} and rlSeqNo = {rlSeqNo}";
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

        }

        public void Save(List<NodeInfo> nodes)
        {

        }

    }
    public class NodeInfo
    {
        public int NodeLevel { get; set; }
        public long TmpId { get; set; }
        public string TmpNm { get; set; }
        public int rlSeqNo { get; set; }
        public List<TempletAttribute> Attributes { get; set; }
    }

    public class TempletAttribute
    {
        //0:缺省属性 1:私有属性
        public int Flag { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
    }


}