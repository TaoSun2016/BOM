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

            //log.Info("=================================================================");
            //log.Info($"tmpid = [{tmpId}] seqno=[{rlSeqNo}] level=[{level}]");
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
                sql = $"SELECT CASE TmpId WHEN '0' THEN '0' ELSE '1' END AS Flag, AttrId, AttrNm, AttrTp FROM AttrDefine WHERE TmpId = '{tmpId}' or TmpId = '0' AND LockFlag = '1' ORDER BY FLAG, AttrId";
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
                sql = $"SELECT CTmpId, rlSeqNo FROM Relation WHERE TmpId = '{tmpId}' AND LockFlag = '1' ORDER BY CTmpId, rlSeqNo";
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
                hasPrivateAttribute = nodeInfo.Attributes.Where(m => m.Flag == "1").Count() > 0;
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

                        materielIdentification = $"{nodeInfo.TmpId}" +"9"+ Convert.ToString(nextNo, 8);
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


        public void CreateBOMTree(ref List<NodeInfo> list, NodeInfo node)
        {
            bool uniqFlag = false;
            string sql = null;
            StringBuilder stringBuilder = new StringBuilder();

            NodeInfo child = null;
            List<string> listCtmpId = new List<string>();
            List<int> listSeqNo = new List<int>();

            SqlDataReader reader = null;

            //如果有物料编码则直接登记节点信息,遍历其下子节点
            if (string.IsNullOrEmpty(node.MaterielId.Trim()))
            {
                //判断属性取值是否唯一
                uniqFlag = true;
                foreach (var attribute in node.Attributes)
                {
                    if (attribute.Values.Count > 1)
                    {
                        uniqFlag = false;
                        break;
                    }
                }

                //属性取值唯一则尝试根据属性取值获取物料编码
                if (uniqFlag)
                {
                    List<string> materielIdList = new List<string>();
                    stringBuilder.Clear();
                    stringBuilder.Append($"SELECT ISNULL(materielIdentfication,'NULL')  AS ID FROM {node.TmpId} WHERE 1 = 1");
                    foreach (var attribute in node.Attributes.Where(m => m.Flag == "1"))
                    {
                        var value = (attribute.Type == "C") ? ("'" + attribute.Values[0].Trim() + "'") : attribute.Values[0].Trim();
                        stringBuilder.Append($" AND {attribute.Id} = {value}");
                    }
                    sql = stringBuilder.ToString();
                    command.CommandText = sql;
                    try
                    {
                        reader = command.ExecuteReader();
                        while (reader.Read())
                        {

                            materielIdList.Add(reader["ID"].ToString());
                        }
                        reader.Close();
                        if (materielIdList.Count == 1)
                        {
                            node.MaterielId = materielIdList[0];
                        }
                        else if (materielIdList.Count > 1)
                        {
                            node.MaterielId = "MultipleMateriels";
                            list.Add(node);
                            return;
                        }
                    }
                    catch (Exception e)
                    {
                        log.Error(string.Format($"Select AttrDefine error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                        reader.Close();
                        throw;
                    }

                }
                else
                {
                    //属性取值不唯一则登记节点并停止该节点以后的遍历
                    node.MaterielId = "MultiplePropertyValues";
                    list.Add(node);
                    return;
                }
            }
           
           
            list.Add(node);

            sql = $"SELECT CTmpId, rlSeqNo　FROM RELATION WHERE TmpId = '{node.TmpId}' and LockFlag = 1 ORDER BY CTmpId, rlSeqNo";
            command.CommandText = sql;

            reader = command.ExecuteReader();
            if (reader.HasRows)
            {
                listCtmpId.Clear();
                listSeqNo.Clear();
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
                    child = new NodeInfo();

                    //查询子节点模板信息,部分子节点属性赋值
                    sql = $"SELECT TmpNm FROM TmpInfo WHERE TmpId = '{cTmpId}'";
                    command.CommandText = sql;

                    try
                    {
                        reader = command.ExecuteReader();
                        if (reader.HasRows)
                        {
                            reader.Read();
                            child.NodeLevel = node.NodeLevel + 1;
                            child.PTmpId = node.TmpId;
                            child.pMaterielId = node.MaterielId;
                            child.TmpId = cTmpId;
                            child.TmpNm = reader[0].ToString();
                            child.rlSeqNo = cSeqNo;
                            reader.Close();
                        }
                        else
                        {
                            log.Error(string.Format($"Select TmpInfo error!\nsql[{sql}]\nError"));
                            reader.Close();
                            throw new Exception("No data found!");
                        }
                    }
                    catch (Exception e)
                    {
                        log.Error(string.Format($"Select TmpInfo error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                        reader.Close();
                        throw;
                    }

                    //获取子节点属性定义
                    //Flag :0 default attribute 1 private attribute
                    //sql = $"SELECT CASE TmpId WHEN 0 THEN 0 ELSE 1 END AS Flag, AttrId, AttrNm, AttrTp FROM AttrDefine WHERE TmpId = {tmpId} or TmpId = 0";
                    sql = $"SELECT CASE TmpId WHEN '0' THEN '0' ELSE '1' END AS Flag, AttrId, AttrNm, AttrTp FROM AttrDefine WHERE TmpId = '{cTmpId}' or TmpId = '0'  AND LockFlag = '1' ORDER BY FLAG, AttrId";
                    command.CommandText = sql;

                    try
                    {
                        reader = command.ExecuteReader();
                        while (reader.Read())
                        {

                            child.Attributes.Add(new TempletAttribute { Flag = reader["Flag"].ToString().Trim(), Id = reader["AttrId"].ToString().Trim(), Name = reader["AttrNm"].ToString().Trim(), Type = reader["AttrTp"].ToString().Trim(), Values = new List<string>() });
                        }
                        reader.Close();
                    }
                    catch (Exception e)
                    {
                        log.Error(string.Format($"Select AttrDefine error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                        reader.Close();
                        throw;
                    }

                    //获取子节点属性值
                    GetChildAttributeValues(node, child);
                    CreateBOMTree(ref list, child);
                }
            }
            else
            {
                reader.Close();
                return;
            }
        }


        public void GetChildAttributeValues(NodeInfo pNode, NodeInfo cNode)
        {
            int valueTpCount = 0;
            string origValueType = "";

            string cAttrId = "";
            string CAttrType = "";
            string cAttrValue = "";
            string valueType = "";
            string pAttrId = "";
            string gt = "";
            string lt = "";
            string eq = "";
            string excld = "";
            string gteq = "";
            string lteq = "";

            List<AttrPass> relationList = new List<AttrPass>();
            List<string> values = new List<string>();
            List<string> midValues = new List<string>();

            string sql = "";
            string defaultValue = "";
            bool verified = true; //同样Valuetype的记录,如果上一条验证通过则为true,有一条验证不过就是false.
            bool found = false; //是否找到匹配记录
            int foundNum = 0;

            //逐个计算子节点属性值
            foreach (var attribute in cNode.Attributes)
            {

                sql = $"SELECT CAttrID,CAttrValue,ValueTp,PAttrId,Gt,Lt,Eq,Excld,Gteq,Lteq FROM AttrPass WHERE TmpId = '{pNode.TmpId}' and CTmpId = '{cNode.TmpId}' AND rlSeqNo = {cNode.rlSeqNo} AND CAttrId = '{attribute.Id}' ORDER BY ValueTp";
                command.CommandText = sql;

                values.Clear();
                relationList.Clear();
                SqlDataReader reader = command.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        relationList.Add(new AttrPass
                        {
                            CAttrId = reader["CAttrId"].ToString().Trim(),
                            CAttrType = attribute.Type.Trim(),
                            CAttrValue = reader["CAttrValue"].ToString().Trim(),
                            ValueTp = reader["ValueTp"].ToString().Trim(),
                            PAttrId = reader["PAttrId"].ToString().Trim(),
                            Gt = reader["Gt"].ToString().Trim(),
                            Lt = reader["Lt"].ToString().Trim(),
                            Eq = reader["Eq"].ToString().Trim(),
                            Excld = reader["Excld"].ToString().Trim(),
                            Gteq = reader["Gteq"].ToString().Trim(),
                            Lteq = reader["Lteq"].ToString().Trim()
                        });

                    }
                    reader.Close();
                    verified = true;
                    midValues.Clear();
                    origValueType = "";
                    valueTpCount = 0;
                    foreach (var attrPass in relationList)
                    {
                        cAttrId = attrPass.CAttrId;
                        CAttrType = attrPass.CAttrType;
                        cAttrValue = attrPass.CAttrValue;
                        valueType = attrPass.ValueTp;
                        pAttrId = attrPass.PAttrId;
                        gt = attrPass.Gt;
                        lt = attrPass.Lt;
                        eq = attrPass.Eq;
                        excld = attrPass.Excld;
                        gteq = attrPass.Gteq;
                        lteq = attrPass.Lteq;

                        //ValueTp变更时即切换规则时,判断上一规则有无计算出值,有则保存到列表再继续下移valueType
                        if (valueType != origValueType && valueTpCount != 0)
                        {
                            if (verified)
                            {
                                values.Union(midValues);
                            }
                            //初始化属性取值
                            verified = true;
                            midValues.Clear();
                            origValueType = valueType;
                        }

                        //属性的缺省值,在此循环中只保留值,不作处理
                        if (valueType == "0")
                        {
                            defaultValue = CalculateAttrbuteValue(pNode, CAttrType, cAttrValue);
                            continue;
                        }

                        valueTpCount++;

                        //如果同组valuetype中有验证不过的,则该组valuetype后续记录都跳过
                        if (!verified)
                        {
                            midValues.Clear();
                            continue;
                        }

                        //获取父属性信息
                        var attr = pNode.Attributes.Find(m => m.Id.Trim() == pAttrId);
                        //父属性是字符型,字符型只判断相等和不等
                        if (attr.Type == "C")
                        {
                            foundNum = 0;
                            foreach (var pValue in attr.Values)
                            {
                                //检查是否满足Excld的情况
                                if (!string.IsNullOrEmpty(excld) && excld != "NULL")
                                {
                                    found = false;

                                    var valueArray = excld.Split(',');
                                    foreach (var element in valueArray)
                                    {
                                        if (pValue == element.Trim())
                                        {
                                            found = true;
                                            break;
                                        }
                                    }
                                    if (found)
                                    {
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
                                        if (pValue == element)
                                        {
                                            found = true;
                                            break;
                                        }
                                    }
                                    if (found)
                                    {
                                        midValues.Add(CalculateAttrbuteValue(pNode, CAttrType, cAttrValue));
                                        foundNum++;
                                        continue;
                                    }
                                }
                            }
                            if (foundNum == 0)
                            {
                                verified = false;
                                continue;
                            }
                        }
                        else//父属性是数字型,数字型需要判断相等,不等和大于小于
                        {
                            foundNum = 0;
                            foreach (var pValue in attr.Values)
                            {
                                var pAttrValue = Convert.ToDecimal(pValue);
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
                                        midValues.Add(CalculateAttrbuteValue(pNode, CAttrType, cAttrValue));
                                        foundNum++;
                                        continue;
                                    }
                                }

                                //判断大于,小于的情况

                                //gt,lt都有值
                                if (!string.IsNullOrEmpty(gt) && gt != "NULL" && !string.IsNullOrEmpty(lt) && lt != "NULL")
                                {
                                    var gtValue = Convert.ToDecimal(gt);
                                    var ltValue = Convert.ToDecimal(lt);

                                    found = true;

                                    if (ltValue > gtValue)//此时应判断父属性值大于gtValue并且小于ltValue
                                    {
                                        //gteq == "1' 表示使用>=判断gt的值,否则使用>判断gt的值. lteq同样处理
                                        if (gteq == "1") //pAttrValue >= gtValue
                                        {
                                            if (pAttrValue - gtValue > -0.005m)
                                            {
                                                //found = true;
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
                                                //found = true;
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
                                                //found = true;
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
                                                //found = true;
                                            }
                                            else
                                            {
                                                found = false;
                                            }
                                        }

                                        if (found)
                                        {
                                            midValues.Add(CalculateAttrbuteValue(pNode, CAttrType, cAttrValue));
                                            foundNum++;
                                            continue;
                                        }
                                    }
                                    else//此时应判断父属性值大于gtValue或者小于ltValue
                                    {
                                        if (gteq == "1") //pAttrValue >= gtValue
                                        {
                                            if (pAttrValue - gtValue > -0.005m)
                                            {
                                                //found = true;
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
                                                //found = true;
                                            }
                                            else
                                            {
                                                found = false;
                                            }
                                        }
                                        if (found)
                                        {
                                            midValues.Add(CalculateAttrbuteValue(pNode, CAttrType, cAttrValue));
                                            foundNum++;
                                            continue;
                                        }

                                        if (lteq == "1") //pAttrValue <= ltValue
                                        {
                                            if (pAttrValue - gtValue < 0.005m)
                                            {
                                                //found = true;
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
                                                //found = true;
                                            }
                                            else
                                            {
                                                found = false;
                                            }
                                        }

                                        if (found)
                                        {
                                            midValues.Add(CalculateAttrbuteValue(pNode, CAttrType, cAttrValue));
                                            foundNum++;
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
                                                //found = false;
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
                                                //found = false;
                                            }
                                        }
                                        if (found)
                                        {
                                            midValues.Add(CalculateAttrbuteValue(pNode, CAttrType, cAttrValue));
                                            foundNum++;
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
                                                //found = false;
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
                                                //found = false;
                                            }
                                        }
                                        if (found)
                                        {
                                            midValues.Add(CalculateAttrbuteValue(pNode, CAttrType, cAttrValue));
                                            foundNum++;
                                            continue;
                                        }
                                    }
                                }
                            }
                            if (foundNum == 0)
                            {
                                verified = false;
                                continue;
                            }

                        }
                    }


                    if (verified && valueTpCount > 0)
                    {
                        values.Union(midValues);
                    }
                    //如果根据父节点属性无法计算子节点属性值,并且子节点属性有缺省值,则赋缺省值
                    if (values.Count == 0 && !string.IsNullOrEmpty(defaultValue))
                    {
                        values.Add(defaultValue);
                    }
                    if (values.Count > 0)
                    {
                        attribute.Values.AddRange(values);
                    }
                }
                else {
                    reader.Close();
                }
            }
        }

        //计算属性值
        public string CalculateAttrbuteValue(NodeInfo pNode, string cAttrType, string Expression)
        {
            StringBuilder returnString = new StringBuilder();
            if (cAttrType == "C")//字符型
            {
                var elements = Expression.Split('+');
                foreach (var element in elements)
                {
                    if (element[0] == '@')//取父属性值
                    {
                        var attrValue = pNode.Attributes.Find(m => m.Id.Trim() == element.Substring(1));
                        if (attrValue.Values.Count > 0)
                        {
                            var result = attrValue.Values[0];
                            returnString.Append(result);
                        }
                        else
                        {
                            returnString.Append("");
                        }
                        

                        //如果父属性有多个取值,子属性也只取一个,否则取值就没法算了
                        
                    }
                    else
                    {
                        returnString.Append(element);
                    }
                }
                return returnString.ToString();
            }
            else//数值型
            {
                returnString.Append(Expression);
                var elements = Expression.Split('+', '-', '*', '/');
                foreach (var element in elements)
                {

                    if (element[0] == '@')
                    {
                        var attrValue = pNode.Attributes.Find(m => m.Id.Trim() == element.Substring(1));
                        if (attrValue.Values.Count > 0 && !string.IsNullOrEmpty(attrValue.Values[0]))
                        {
                            returnString.Replace(element, attrValue.Values[0]);
                        }
                        else
                        {
                            return "";
                        }
                    }
                }
                command.CommandText = $"SELECT {returnString} AS RESULT";
                var result = command.ExecuteScalar().ToString();
                return result;
            }
        }

        //保存节点
        public bool SaveNode( NodeInfo node)
        {
            int result = -1;
            bool hasPrivateAttribute = false;
            string materielIdentification = null;
            string sql = null;
            StringBuilder insertBuilder = new StringBuilder($"INSERT INTO {node.TmpId} (");
            StringBuilder insertValues = new StringBuilder($" ) VALUES (");

            SqlDataReader dataReader = null;

            //如果节点没有物料编码则生成
            if (string.IsNullOrEmpty(node.MaterielId))
            {
                //If nodeInfo has private attribute
                hasPrivateAttribute = node.Attributes.Where(m => m.Flag == "1").Count() > 0;
                if (hasPrivateAttribute)
                {
                    //Check if the private attribute table exists
                    sql = $"SELECT COUNT(*) FROM SYSOBJECTS WHERE NAME = '{node.TmpId}' ";
                    command.CommandText = sql;
                    try
                    {
                        result = (int)command.ExecuteScalar();
                    }
                    catch (Exception e)
                    {
                        log.Error(string.Format($"Look for talbe {node.TmpId} Error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                        throw;
                    }
                    if (result == 0)
                    {
                        log.Error(string.Format($"Table {node.TmpId} doesn't exsit!\nsql[{sql}]\n"));
                        throw new Exception($"Table {node.TmpId} doesn't exsit!!");
                    }

                    //Check duplicated records
                    StringBuilder builder = new StringBuilder($"SELECT COUNT(*) FROM {node.TmpId} WHERE");
                    StringBuilder mIdBuilder = new StringBuilder($"SELECT materielIdentfication FROM {node.TmpId} WHERE");

                    int counter = 0;
                    var privateAttributes = node.Attributes.Where(m => m.Flag == "1");
                    foreach (var attrbute in privateAttributes)
                    {
                        counter++;
                        if (attrbute.Type == "C")
                        {
                            builder.Append($" {attrbute.Id} = '{attrbute.Values[0]}'");
                            mIdBuilder.Append($" {attrbute.Id} = '{attrbute.Values[0]}'");
                            insertBuilder.Append($" {attrbute.Id},");
                            insertValues.Append($" '{attrbute.Values[0]}',");
                        }
                        else
                        {
                            builder.Append($" {attrbute.Id} = {attrbute.Values[0]}");
                            mIdBuilder.Append($" {attrbute.Id} = {attrbute.Values[0]}");
                            insertBuilder.Append($" {attrbute.Id},");
                            insertValues.Append($" {attrbute.Values[0]},");
                        }

                        if (counter != privateAttributes.Count())
                        {
                            builder.Append($" AND");
                            mIdBuilder.Append($" AND");
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
                        throw;
                    }
                    //找不到,则新生成物料编码
                    if (result == 0)
                    {
                        //Get Materiel Identification
                        sql = $"SELECT NEXT_NO FROM SEQ_NO WHERE Ind_Key = '{node.TmpId}'";
                        try
                        {
                            command.CommandText = sql;
                            dataReader = command.ExecuteReader();                          
                            if (dataReader.HasRows)
                            {
                                dataReader.Read();
                                long nextNo = Convert.ToInt64(dataReader[0].ToString());
                                dataReader.Close();

                                materielIdentification = $"{node.TmpId}9" + Convert.ToString(nextNo, 8);
                                nextNo += 1;
                                sql = $"UPDATE SEQ_NO SET NEXT_NO = {nextNo} WHERE IND_KEY = '{node.TmpId}'";
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
                                log.Error($"No record is updated,TmpID=[{node.TmpId}]");

                                throw new Exception($"No record is updated,TmpID=[{node.TmpId}]");
                            }
                        }
                        catch (Exception e)
                        {
                            log.Error($"Get MaterielIdentification Error,TmpID=[{node.TmpId}]sql=[{sql}]error[{e.StackTrace}][{e}][{e.Message}]");
                            dataReader.Close();

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

                                throw;
                            }
                            if (result == 0)
                            {
                                log.Error(string.Format($"No private attrbutes is registered!\nsql[{sql}]\n"));

                                throw new Exception("No private attrbutes is registered!");
                            }
                        }

                        //Register default attribute values
                        //前台必须给所有的缺省属性赋值,没有值赋缺省值
                        insertBuilder = new StringBuilder($"INSERT INTO DeafaultAttr (");
                        insertValues = new StringBuilder($" ) VALUES (");
                        var defaultAttributes = node.Attributes.Where(m => m.Flag == "0");
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
                            throw;
                        }
                        if (result == 0)
                        {
                            log.Error(string.Format($"No default attrbutes is registered!\nsql[{sql}]\n"));

                            throw new Exception("No default attrbutes is registered!");
                        }
                        node.MaterielId = materielIdentification;

                    }
                    else if (result == 1)//找到,使用找到的物料编码
                    {
                        sql = mIdBuilder.ToString();
                        command.CommandText = sql;
                        try
                        {
                            dataReader = command.ExecuteReader();
                            dataReader.Read();
                            materielIdentification = dataReader[0].ToString().Trim();
                            node.MaterielId = materielIdentification;

                        }
                        catch (Exception e)
                        {
                            log.Error(string.Format($"Select materiel identification Error!\nsql[{sql}]\nErrorMessage[{e.Message}]\nErrorStack[{e.StackTrace}]"));
                            throw;
                        }
                        finally
                        {
                            dataReader.Close();
                        }
                    }
                    else//找到多条,报错
                    {
                        log.Error(string.Format($"Found more than one records!\nsql[{sql}]\n"));
                        throw new Exception("Found more than one records!!!");
                    }
                }               
            }
            else
            {
                //如果前台上送有物料编码. 当前台有物料编码时是否还允许用户修改物料的属性,如果允许修改后如果和其他物料属性值一样怎么办?
            }

            //生成DCM码
            int dcm = 0;
            sql = $"SELECT DCM FROM TmpInfo WHERE TmpId = '{node.TmpId}'";
            try
            {
                command.CommandText = sql;
                dataReader = command.ExecuteReader();

                if (dataReader.HasRows)
                {
                    dataReader.Read();

                    dcm = (int)dataReader[0];
                    dataReader.Close();

                    if (dcm == 0 || dcm < node.NodeLevel)
                    {
                        sql = $"UPDATE TmpInfo SET DCM = {node.NodeLevel} WHERE TmpId = '{node.TmpId}'";
                        command.CommandText = sql;
                        result = command.ExecuteNonQuery();
                        if (result == 0)
                        {
                            log.Error($"No record is updated in TmpInfo,TmpID=[{node.TmpId}]");

                            throw new Exception($"No record is updated in TmpInfo,TmpID=[{node.TmpId}]");
                        }
                    }
                }
                else
                {
                    log.Error(string.Format($"Can't find record in table TmpInfo.[{sql}]"));
                    throw new Exception(string.Format($"Can't find record in table TmpInfo.[{sql}]"));
                }
            }
            catch (Exception e)
            {
                log.Error($"Update DCM Error,TmpID=[{node.TmpId}]sql=[{sql}]error[{e.StackTrace}][{e}][{e.Message}]");
                dataReader.Close();
                throw;
            }

            //登记表BOM 

            sql = $"INSERT INTO BOM (materielIdentfication, TmpId, CmId, CTmpId, CNum, rlSeqNo) VALUES "+
                $"('{node.pMaterielId}','{node.PTmpId}','{node.MaterielId}','{node.TmpId}','{node.Count}',{node.rlSeqNo})";

            try
            {
                command.CommandText = sql;
                result = command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                log.Error($"Insert BOM Error,sql=[{sql}]error[{e.StackTrace}][{e}][{e.Message}]");
                throw;
            }
            if (result == 0)
            {
                log.Error($"No record is inserted in BOM,sql=[{sql}]");

                throw new Exception($"No record is inserted in BOM,sql=[{sql}]");
            }
            return true;
        }

        private void LogNode(NodeInfo node)
        {
            log.Info("=====================");
            log.Info("NodeLevel=" + node.NodeLevel);
            log.Info("ptmpid="+node.PTmpId+"  pmaterielid="+node.pMaterielId);
            log.Info("TmpId=" + node.TmpId+"  materelid="+node.MaterielId);
            foreach (var attr in node.Attributes)
            {
                log.Info("---------------------------");

                log.Info(attr.Id + "----" + attr.Name+":");
                foreach (var value in attr.Values)
                {
                    log.Info("=>"+value);
                }
            }
            log.Info("=====================");
        }
    }


    public class NodeInfo
    {
        public int NodeLevel { get; set; }
        public string PTmpId { get; set; }
        public string pMaterielId { get; set; }
        public int PrlSeqNo { get; set; }
        public string TmpId { get; set; }
        public string TmpNm { get; set; }
        public string MaterielId { get; set; }
        public int rlSeqNo { get; set; }
        public decimal Count { get; set; }
        public List<TempletAttribute> Attributes { get; set; }
        public NodeInfo()
        {
            NodeLevel = 0;
            PTmpId = "";
            pMaterielId = "";
            TmpId = "";
            TmpNm = "";
            MaterielId = "";
            rlSeqNo = 0;
            Count = 0.00m;
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

    public class AttrPass
    {
        public string CAttrId { get; set; }
        public string CAttrType { get; set; }
        public string CAttrValue { get; set; }
        public string ValueTp { get; set; }
        public string Eq { get; set; }
        public string Excld { get; set; }
        public string Gt { get; set; }
        public string Gteq { get; set; }
        public string Lt { get; set; }
        public string Lteq { get; set; }
        public string PAttrId { get; set; }

    }


}