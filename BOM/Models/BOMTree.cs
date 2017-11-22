﻿using System;
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
        SqlConnection connection = null;
        SqlCommand command = null;
        SqlTransaction transaction = null;

        public BOMTree()
        {

        }
        public BOMTree(SqlConnection sqlConnection, SqlCommand sqlCommand, SqlTransaction sqlTransaction)
        {
            this.connection = sqlConnection;
            this.command = sqlCommand;
            this.transaction = sqlTransaction;
        }



        //Open BOM Tree
        //测试暂时将tmpid设为string型
        public void FindChildrenTree(SqlConnection connection, ref List<NodeInfo> list, string pTmpid, string tmpId, int rlSeqNo, int level)
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
                        nodeInfo.PTmpId = pTmpid;
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

                        nodeInfo.Attributes.Add(new TempletAttribute { Flag = dataReader["Flag"].ToString(), Id = dataReader["AttrId"].ToString(), Name = dataReader["AttrNm"].ToString(), Type = dataReader["AttrTp"].ToString(), Values = new List<string>() });
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
                    FindChildrenTree(connection, ref list, tmpId, listCTmpId[i], listSeqNo[i], level + 1);
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
                            builder.Append($" {attrbute.Id} = '{attrbute.Values[0]}'");
                            insertBuilder.Append($" {attrbute.Id},");
                            insertValues.Append($" '{attrbute.Values[0]}',");
                        }
                        else
                        {
                            builder.Append($" {attrbute.Id} = {attrbute.Values[0]}");
                            insertBuilder.Append($" {attrbute.Id},");
                            insertValues.Append($" {attrbute.Values[0]},");
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
                        insertValues.Append($" '{defaultAttrbute.Values[0]}',");
                    }
                    else
                    {
                        insertValues.Append($" {defaultAttrbute.Values[0]},");
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
            if (string.IsNullOrEmpty(node.MaterielId))
            {
                //对该节点作申请编码
            }

            //递归调用方法:保存当前节点信息,查找子节点列表,根据当前节点属性值生成子节点属性值

            //




            return new List<NodeInfo>();
        }

        public bool CreateBOMTree(ref List<NodeInfo> list, NodeInfo node, int level)
        {
            string sql = null;
            NodeInfo child = new NodeInfo();
            List<string> listCtmpId = new List<string>();
            List<int> listSeqNo = new List<int>();


            list.Add(node);

            sql = $"SELECT CTmpId, rlSeqNo　FROM RELATION WHERE TmpId = '{node.TmpId}' and LockFlag = 1 ORDER BY CTmpId, rlSeqNo";
            command.CommandText = sql;

            SqlDataReader reader = command.ExecuteReader();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    //生成子节点数据
                    listCtmpId.Add(reader["CTmpId"].ToString());
                    listSeqNo.Add((int)reader["rlSeqNo"]);

                }
                reader.Close();


                for (int i = 0; i < listCtmpId.Count; i++)
                {
                    string cTmpId = listCtmpId[i];
                    int cSeqNo = listSeqNo[i];

                    sql = $"SELECT TmpNm FROM TmpInfo WHERE TmpId = '{cTmpId}'";
                    command.CommandText = sql;

                    try
                    {
                        reader = command.ExecuteReader();
                        if (reader.HasRows)
                        {
                            reader.Read();
                            child.NodeLevel = level;
                            child.PTmpId = node.TmpId;
                            child.TmpId = cTmpId;
                            child.TmpNm = reader[0].ToString();
                            child.rlSeqNo = cSeqNo;
                            reader.Close();
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
                        reader.Close();
                        throw;
                    }

                    //Flag :0 default attribute 1 private attribute
                    //sql = $"SELECT CASE TmpId WHEN 0 THEN 0 ELSE 1 END AS Flag, AttrId, AttrNm, AttrTp FROM AttrDefine WHERE TmpId = {tmpId} or TmpId = 0";
                    sql = $"SELECT CASE TmpId WHEN '0' THEN '0' ELSE '1' END AS Flag, AttrId, AttrNm, AttrTp FROM AttrDefine WHERE TmpId = '{cTmpId}' or TmpId = '0' ORDER BY FLAG, AttrId";
                    command.CommandText = sql;

                    try
                    {
                        reader = command.ExecuteReader();
                        while (reader.Read())
                        {

                            child.Attributes.Add(new TempletAttribute { Flag = reader["Flag"].ToString(), Id = reader["AttrId"].ToString(), Name = reader["AttrNm"].ToString(), Type = reader["AttrTp"].ToString(), Values = new List<string>() });
                        }
                    }
                    catch (Exception e)
                    {
                        log.Error(string.Format($"Select AttrDefine error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                        reader.Close();
                        throw;
                    }
                    reader.Close();
                    GetChildAttributeValues(node, child);
                }

            }
            else
            {
                reader.Close();
                return true;
            }




            return true;
        }


        public void GetChildAttributeValues(NodeInfo pNode, NodeInfo cNode)
        {
            bool newValueTp = true;

            string origValueType = "";
            string origAttrValue = "";

            string cAttrId = "";
            string cAttrValue = "";
            string valueType = "";
            string pAttrId = "";
            string gt = "";
            string lt = "";
            string eq = "";
            string excld = "";
            string gteq = "";
            string lteq = "";

            List<string> values = new List<string>();

            string sql = "";
            string defaultValue = "";
            bool verified = true; //同样Valuetype的记录,如果上一条验证通过则为true
            bool found = false; //是否找到匹配记录

            foreach (var attribute in cNode.Attributes)
            {
                sql = $"SELECT CAttrID,CAttrValue,ValueType,PAttrId,Gt,Lt,Eq,Excld,Gteq,Lteq FROM AttrPass WHERE TmpId = '{pNode.TmpId}' and CTmpId = '{cNode.TmpId}' AND rlSeqNo = '{cNode.rlSeqNo} AND CAttrId = '{attribute.Id}' ORDER BY ValueType";
                command.CommandText = sql;

                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    cAttrId = reader["CAttrId"].ToString();
                    cAttrValue = reader["CAttrValue"].ToString();
                    valueType = reader["ValueType"].ToString();
                    pAttrId = reader["PAttrId"].ToString();
                    gt = reader["Gt"].ToString();
                    lt = reader["Lt"].ToString();
                    eq = reader["Eq"].ToString();
                    excld = reader["Excld"].ToString();
                    gteq = reader["Gteq"].ToString();
                    lteq = reader["Lteq"].ToString();

                    //ValueTp变更时即切换规则时,判断上一规则有无计算出值,有则保存到列表再继续下移valueType
                    if (valueType != origValueType && !newValueTp)
                    {
                        if (verified)
                        {
                            values.Add(origAttrValue);                          
                        }
                        //初始化属性取值
                        verified = true;
                        origAttrValue = "";
                        origValueType = valueType;
                    }
                 
                    //属性的缺省值,在此循环中只保留值,不作处理
                    if (valueType == "0")
                    {
                        defaultValue = cAttrValue;
                        continue;
                    }

                    newValueTp = false;

                    //如果同组valuetype中有验证不过的,则该组valuetype后续记录都跳过
                    if (!verified)
                    {
                        continue;
                    }

                    //获取父属性信息
                    var attr = pNode.Attributes.Find(m => m.Id == pAttrId);

                    //父属性是字符型,字符型只判断相等和不等
                    if (attr.Type == "C")
                    {

                        var pAttrValue = attr.Values[0];

                        //检查是否满足Excld的情况
                        if (!string.IsNullOrEmpty(excld) && excld != "NULL")
                        {
                            found = false;

                            var valueArray = excld.Split(',');
                            foreach (var element in valueArray)
                            {
                                if (pAttrValue == element)
                                {
                                    found = true;
                                    break;
                                }
                            }
                            if (found)
                            {
                                verified = false;
                                continue;
                            }
                        }

                        //判断是否满足相等的情况
                        if (!string.IsNullOrEmpty(eq) && eq != "NULL")
                        {
                            found = false;  //true表示找到相等的值

                            var valueArray = eq.Split(',');
                            foreach (var element in valueArray)
                            {
                                if (pAttrValue == element)
                                {
                                    found = true;
                                    break;
                                }
                            }
                            if (found)
                            {
                                origAttrValue = cAttrValue;
                                continue;
                            }
                        }
                    }
                    else//父属性是数字型,数字型需要判断相等,不等和大于小于
                    {
                        var pAttrValue = Convert.ToDecimal(attr.Values[0]);


                        //检查是否满足Excld的情况
                        if (!string.IsNullOrEmpty(excld) && excld != "NULL")
                        {
                            found = false;

                            var valueArray = excld.Split(',');
                            foreach (var element in valueArray)
                            {

                                if (Math.Abs(pAttrValue - Convert.ToDecimal(element)) < 0.005m)
                                {
                                    found = true; ;
                                    break;
                                }
                            }
                            if (found)
                            {
                                verified = false;
                                continue;
                            }
                        }

                        //检查是否满足相等情况,满足即可进入下一轮循环,不满足在判断大于和小于的情况
                        if (!string.IsNullOrEmpty(eq) && eq != "NULL")
                        {
                            found = false;
                            var valueArray = eq.Split(',');
                            foreach (var element in valueArray)
                            {
                                if (Math.Abs(pAttrValue - Convert.ToDecimal(element)) < 0.005m)
                                {
                                    found = true;
                                    break;
                                }
                            }

                            if (found)
                            {
                                origAttrValue = cAttrValue;
                                continue;
                            }
                        }

                        //判断大于,小于的情况

                        //gt,lt都有值
                        if (!string.IsNullOrEmpty(gt) && gt != "NULL" && !string.IsNullOrEmpty(lt) && lt != "NULL")
                        {
                            var gtValue = Convert.ToDecimal(gt);
                            var ltValue = Convert.ToDecimal(lt);
                            found = false;
                            if (ltValue > gtValue)//此时应判断父属性值大于gtValue并且小于ltValue
                            {
                                if (gteq == "1") //pAttrValue >= gtValue
                                {
                                    if (pAttrValue - gtValue > -0.005m)
                                    {
                                        found = true;
                                    }
                                    else
                                    {
                                        found = false;
                                    }
                                }
                                else  //pAttrValue > gtValue
                                {
                                    if (pAttrValue - gtValue > 0)
                                    {
                                        found = true;
                                    }
                                    else
                                    {
                                        found = false;
                                    }
                                }

                                if (lteq == "1") //pAttrValue <= ltValue
                                {
                                    if (pAttrValue - gtValue < 0.005m)
                                    {
                                        found = true;
                                    }
                                    else
                                    {
                                        found = false;
                                    }
                                }
                                else  //pAttrValue < ltValue
                                {
                                    if (pAttrValue - gtValue < 0)
                                    {
                                        found = true;
                                    }
                                    else
                                    {
                                        found = false;
                                    }
                                }

                                if (found)
                                {
                                    origAttrValue = cAttrValue;
                                    continue;
                                }
                            }
                            else//此时应判断父属性值大于gtValue或者小于ltValue
                            {
                                if (gteq == "1") //pAttrValue >= gtValue
                                {
                                    if (pAttrValue - gtValue > -0.005m)
                                    {
                                        found = true;
                                    }
                                    else
                                    {
                                        found = false;
                                    }
                                }
                                else  //pAttrValue > gtValue
                                {
                                    if (pAttrValue - gtValue > 0)
                                    {
                                        found = true;
                                    }
                                    else
                                    {
                                        found = false;
                                    }
                                }
                                if (found)
                                {
                                    origAttrValue = cAttrValue;
                                    continue;
                                }

                                if (lteq == "1") //pAttrValue <= ltValue
                                {
                                    if (pAttrValue - gtValue < 0.005m)
                                    {
                                        found = true;
                                    }
                                    else
                                    {
                                        found = false;
                                    }
                                }
                                else  //pAttrValue < ltValue
                                {
                                    if (pAttrValue - gtValue < 0)
                                    {
                                        found = true;
                                    }
                                    else
                                    {
                                        found = false;
                                    }
                                }

                                if (found)
                                {
                                    origAttrValue = cAttrValue;
                                    continue;
                                } 
                            }
                        }
                        else//gt,lt都没有值或只有一个有值
                        {
                                                       
                            found = false;
                            if (!string.IsNullOrEmpty(gt) && gt != "NULL")
                            {
                                var gtValue = Convert.ToDecimal(gt);

                                if (gteq == "1") //pAttrValue >= gtValue
                                {
                                    if (pAttrValue - gtValue > -0.005m)
                                    {
                                        found = true;
                                    }
                                    else
                                    {
                                        found = false;
                                    }
                                }
                                else  //pAttrValue > gtValue
                                {
                                    if (pAttrValue - gtValue > 0)
                                    {
                                        found = true;
                                    }
                                    else
                                    {
                                        found = false;
                                    }
                                }
                                if (found)
                                {
                                    origAttrValue = cAttrValue;
                                    continue;
                                }

                            }
                            else if (!string.IsNullOrEmpty(lt) && lt != "NULL")
                            {
                                var ltValue = Convert.ToDecimal(lt);

                                if (lteq == "1") //pAttrValue <= ltValue
                                {
                                    if (pAttrValue - ltValue < 0.005m)
                                    {
                                        found = true;
                                    }
                                    else
                                    {
                                        found = false;
                                    }
                                }
                                else  //pAttrValue < ltValue
                                {
                                    if (pAttrValue - ltValue < 0)
                                    {
                                        found = true;
                                    }
                                    else
                                    {
                                        found = false;
                                    }
                                }
                                if (found)
                                {
                                    origAttrValue = cAttrValue;
                                    continue;
                                }
                            }
                        }
                    }
                    verified = false;

                }

                //如果根据父节点属性无法计算子节点属性值,则赋缺省值
                if (values.Count == 0 && !string.IsNullOrEmpty(defaultValue))
                {
                    values.Add(defaultValue);
                }
                if (values.Count > 0)
                {
                    attribute.Values = values;
                }
            }
        }
        public void Save(List<NodeInfo> nodes)
        {

        }

    }
    public class NodeInfo
    {
        public int NodeLevel { get; set; }
        public string PTmpId { get; set; }
        public string TmpId { get; set; }
        public string TmpNm { get; set; }
        public int rlSeqNo { get; set; }

        public string MaterielId { get; set; }
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
        public List<string> Values { get; set; }

        public TempletAttribute()
        {
            Values = new List<string>();
        }
    }


}