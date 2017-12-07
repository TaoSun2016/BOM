using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace BOM.Models
{
    public partial class Templet
    {
        log4net.ILog log = log4net.LogManager.GetLogger("Templet");

        //public string GetTmpId(SqlConnection connection,SqlCommand command)
        //{
        //    int result = 0;
        //    long tempSeqNo = 0;
        //    string seqNo = null;
        //    string sql = @"SELECT * FROM SEQ_NO WHERE Ind_Key = '0'";
        //    SqlDataReader dataReader = command.ExecuteReader();

        //    if (dataReader.HasRows)
        //    {
        //        dataReader.Read();

        //        tempSeqNo = (long)dataReader["NEXT_NO"];

        //        dataReader.Close();

        //        seqNo = Convert.ToString(tempSeqNo, 8) + "9";
        //        tempSeqNo += 1;
        //        sql = $"UPDATE SEQ_NO SET NEXT_NO = {tempSeqNo} WHERE IND_KEY = '0'";
        //    }
        //    else
        //    {
        //        log.Error(string.Format($"Select Seq_No error\nsql[{sql}]\n"));
        //        dataReader.Close();
        //        throw new Exception("There is no base parameter!");
        //    }

        //    try
        //    {
        //        command.CommandText = sql;
        //        result = command.ExecuteNonQuery();
        //        if (result == 0)
        //        {
        //            log.Error(string.Format($"Update Seq_No 0 record\nsql[{sql}]\n"));
        //            throw new Exception("update table seq_no error!");
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        log.Error(string.Format($"Update Seq_No error\nsql[{sql}][{e.StackTrace}]\n"));
        //        throw;
        //    }
        //    return seqNo;
        //}
        //新建根物料模板
        public void CreateTemplet(string templetName, string creater)
        {
            int result = 0;
            string sql = null;

            SqlConnection sqlConnection = DBConnection.OpenConnection();
            SqlTransaction sqlTransaction = sqlConnection.BeginTransaction();
            using (SqlCommand command = new SqlCommand())
            {
                command.Connection = sqlConnection;
                command.Transaction = sqlTransaction;

                //检查模板名称是否重复
                sql = $"SELECT COUNT(*) FROM TmpInfo WHERE TmpNm = '{templetName}' ";
                command.CommandText = sql;
                try
                {
                    result = (int)command.ExecuteScalar();
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Select from table TmpInfo error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    DBConnection.CloseConnection(sqlConnection);
                    throw;
                }

                if (result > 0)
                {
                    log.Error(string.Format($"模板名称重复!Name=[{templetName}]\nsql[{sql}]\n"));
                    DBConnection.CloseConnection(sqlConnection);
                    throw new Exception("模板名称重复!");
                }

                //登记模板信息
                SeqNo seqNo = new SeqNo();
                string tmpId = seqNo.GetBaseSeqNo();
                
                sql = $"INSERT INTO TmpInfo (TmpId, TmpNm, Root, LockCount, CrtDate, Crter, EditLock, DCM) VALUES ({tmpId}, '{templetName}', '0', 0, '{DateTime.Now}', '{creater}', 0, 0) ";
                command.CommandText = sql;
                try
                {
                    result = command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Insert table TmpInfo error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
                    throw;
                }

                if (result == 0)
                {
                    log.Error(string.Format($"插入TmpInfo记录数为0\nsql[{sql}]\n"));
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
                    throw new Exception("插入TmpInfo记录数为0!");
                }

                //登记根物料模板表Relation,TmpId='99',旧系统为'root'
                sql = $"INSERT INTO Relation (TmpId, CTmpId, CTmpNum, LockFlag, CrtDate, Crter, rlSeqNo) VALUES (99, {tmpId}, 0, 0, '{DateTime.Now}', '{creater}', 0) ";
                command.CommandText = sql;
                try
                {
                    result = command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Insert table Relation error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
                    throw;
                }

                if (result == 0)
                {
                    log.Error(string.Format($"Insert table Relation 0 record!\nsql[{sql}]\n"));
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
                    throw new Exception("Insert table Relation 0 record!");
                }
            }
            sqlTransaction.Commit();
            DBConnection.CloseConnection(sqlConnection);
        }

        //新建物料信息(非根物料节点,无参考模板,上送父模板ID和新模板名称)
        public void CreateTemplet(long parentTempletId, string templetName, string creater)
        {
            int result = 0;
            int rlSeqNo = -1;
            string sql = null;

            SqlConnection sqlConnection = DBConnection.OpenConnection();
            SqlTransaction sqlTransaction = sqlConnection.BeginTransaction();
            using (SqlCommand command = new SqlCommand())
            {
                command.Connection = sqlConnection;
                command.Transaction = sqlTransaction;

                //检查有无重复模板名称
                sql = $"SELECT COUNT(*) FROM TmpInfo WHERE TmpNm = '{templetName}' ";
                command.CommandText = sql;
                try
                {
                    result = (int)command.ExecuteScalar();
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Select from table TmpInfo error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    DBConnection.CloseConnection(sqlConnection);
                    throw;
                }

                if (result > 0)
                {
                    log.Error(string.Format($"模板名称重复!Name=[{templetName}]\nsql[{sql}]\n"));
                    DBConnection.CloseConnection(sqlConnection);
                    throw new Exception("模板名称重复!");
                }

                //检查父模板是否存在
                sql = $"SELECT COUNT(*) FROM TmpInfo WHERE TmpId = '{parentTempletId}' ";
                command.CommandText = sql;
                try
                {
                    result = (int)command.ExecuteScalar();
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Select from table TmpInfo error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    DBConnection.CloseConnection(sqlConnection);
                    throw;
                }

                if (result != 1)
                {
                    log.Error(string.Format($"父模板不存在!TmpId=[{parentTempletId}]\nsql[{sql}]\n"));
                    DBConnection.CloseConnection(sqlConnection);
                    throw new Exception("父模板不存在!");
                }

                //登记新模板信息
                SeqNo seqNo = new SeqNo();
                string tmpId = seqNo.GetBaseSeqNo();
                sql = $"INSERT INTO TmpInfo (TmpId, TmpNm, Root, LockCount, CrtDate, Crter, EditLock, DCM) VALUES ({tmpId}, '{templetName}', '0', 0, '{DateTime.Now}', '{creater}', 0, 0) ";
                command.CommandText = sql;
                try
                {
                    result = command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Insert table TmpInfo error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
                    throw;
                }

                if (result == 0)
                {
                    log.Error(string.Format($"插入TmpInfo记录数为0\nsql[{sql}]\n"));
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
                    throw new Exception("插入TmpInfo记录数为0!");
                }


                //登记父子模板关系表Relation
                //获取Relation.rlSeqNo的值
                sql = $"SELECT ISNULL(MAX(rlSeqNo),-1) FROM RELATION WHERE TmpId = '{parentTempletId}' and CTmpId = '{tmpId}'";
                command.CommandText = sql;
                try
                {
                    rlSeqNo = (int)command.ExecuteScalar();
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Select Table Relation error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    DBConnection.CloseConnection(sqlConnection);
                    throw;
                }
                rlSeqNo++;

                //Insert Relation
                sql = $"INSERT INTO RELATION (TmpId, CTmpId, CTmpNum, LockFlag, CrtDate, Crter, rlSeqNo) VALUES ({parentTempletId}, {tmpId}, 0, 0, '{DateTime.Now}', '{creater}',{rlSeqNo})";
                command.CommandText = sql;
                try
                {
                    result = command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Insert table Relation error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
                    throw;
                }
                if (result == 0)
                {
                    log.Error(string.Format($"插入Relation表记录数为0\nsql[{sql}]\n"));
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
                    throw new Exception("插入Relation表记录数为0!");
                }


                //登记数据库表AttrPass
               
                sql = $"INSERT INTO AttrPass (TmpId, CTmpId, CAttrId, CAttrValue, ValueTp, PAttrId, Gt, Lt, Eq, Excld, Gteq, Lteq, CrtDate, Crter, rlSeqNo) VALUES ({parentTempletId},{tmpId},'_danyl', 0, '0', '0', '', '', '', '', '', '', '{DateTime.Now}','{creater}',{rlSeqNo})";
                command.CommandText = sql;
                try
                {
                    result = command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Insert table AttrPass error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
                    throw;
                }
                if (result == 0)
                {
                    log.Error(string.Format($"Insert table Attrpass 0 record\nsql[{sql}]\n"));
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
                    throw new Exception("Insert table Attrpass 0 record!");
                }
                sqlTransaction.Commit();

            }
            DBConnection.CloseConnection(sqlConnection);
        }

        //新建物料信息(非根物料节点,有参考模板,父模板)
        public void CreateCopiedTemplet(long parentTempletId, long referenceTempletId, string creater)
        {
            int result = 0;
            int rlSeqNo = -1;
            string sql = null;

            SqlConnection sqlConnection = DBConnection.OpenConnection();
            SqlTransaction sqlTransaction = sqlConnection.BeginTransaction();
            using (SqlCommand command = new SqlCommand())
            {
                command.Connection = sqlConnection;
                command.Transaction = sqlTransaction;

               
                //登记数据库表Relation

                //获取Relation.rlSeqNo的值
                sql = $"SELECT ISNULL(MAX(rlSeqNo),-1) FROM RELATION WHERE TmpId = '{parentTempletId}' and CTmpId = '{referenceTempletId}'";
                command.CommandText = sql;
                try
                {
                    rlSeqNo = (int)command.ExecuteScalar();
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Select Table Relation error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
                    throw;
                }
                rlSeqNo++;

                //Insert Relation
                sql = $"INSERT INTO RELATION (TmpId, CTmpId, CTmpNum, LockFlag, CrtDate, Crter, rlSeqNo) VALUES ({parentTempletId}, {referenceTempletId}, 0, 0, '{DateTime.Now}', '{creater}', {rlSeqNo})";
                command.CommandText = sql;
                try
                {
                    result = command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Insert table Relation error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
                    throw;
                }
                if (result == 0)
                {
                    log.Error(string.Format($"插入Relation表记录数为0\nsql[{sql}]\n"));
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
                    throw new Exception("插入Relation表记录数为0!");
                }


                //登记数据库表AttrPass
                sql = $"INSERT INTO AttrPass (TmpId, CTmpId, CAttrId, CAttrValue, ValueTp, PAttrId, Gt, Lt, Eq, Excld, Gteq, Lteq, CrtDate, Crter, rlSeqNo) VALUES ({parentTempletId}, {referenceTempletId}, '_danyl', 0, '0', '0', '', '', '', '', '', '', '{DateTime.Now}', '{creater}',{rlSeqNo})";
                command.CommandText = sql;
                try
                {
                    result = command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Insert table AttrPass error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
                    throw;
                }
                if (result == 0)
                {
                    log.Error(string.Format($"Insert table Attrpass 0 record\nsql[{sql}]\n"));
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
                    throw new Exception("Insert table Attrpass 0 record!");
                }
                sqlTransaction.Commit();

            }
            DBConnection.CloseConnection(sqlConnection);
        }

        //删除模板节点
        public void DeleteTemplet(long parentTempletId, long TempletId, int rlSeqNo)
        {
            int result = 0;
            string sql = null;

            SqlConnection sqlConnection = DBConnection.OpenConnection();
            SqlTransaction sqlTransaction = sqlConnection.BeginTransaction();
            using (SqlCommand command = new SqlCommand())
            {
                command.Connection = sqlConnection;
                command.Transaction = sqlTransaction;

                //Delete AttrPass
                sql = $"DELETE FROM  AttrPass  where TmpId = '{parentTempletId}' and CTmpId = '{TempletId}' and rlseqno = {rlSeqNo}";
                command.CommandText = sql;
                try
                {
                    result = command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Delete AttrPass error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
                    throw;
                }
                if (result == 0)
                {
                    log.Error(string.Format($"Delete Attrpass 0 record\nsql[{sql}]\n"));
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
                    throw new Exception("Delete Attrpass 0 record!");
                }

                //Delete Relation
                sql = $"DELETE FROM  Relation  where TmpId = '{parentTempletId}' and CTmpId = '{TempletId}' and rlseqno = {rlSeqNo}";
                command.CommandText = sql;
                try
                {
                    result = command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Delete Relation error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
                    throw;
                }
                if (result == 0)
                {
                    log.Error(string.Format($"Delete Relation 0 record\nsql[{sql}]\n"));
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
                    throw new Exception("Delete Relation 0 record!");
                }
                sqlTransaction.Commit();
            }
            DBConnection.CloseConnection(sqlConnection);
        }

        //锁定模板
        public void LockTemplet(long templetId)
        {
            int result1 = 0;
            int result2 = 0;
            int result3 = 0;
            int result4 = 0;

            string sql = null;
            StringBuilder sqlCreate = new StringBuilder();

            SqlConnection sqlConnection = DBConnection.OpenConnection();
            SqlTransaction sqlTransaction = sqlConnection.BeginTransaction();
            using (SqlCommand command = new SqlCommand())
            {
                command.Connection = sqlConnection;
                command.Transaction = sqlTransaction;

                try
                {
                    sql = $"UPDATE TmpInfo SET LockCount = LockCount+1 WHERE TmpId = '{templetId}'";
                    command.CommandText = sql;
                    result1 = command.ExecuteNonQuery();

                    sql = $"UPDATE Relation SET LockFlag = 1 WHERE LockFlag = 0 AND CTmpId = '{templetId}'";
                    command.CommandText = sql;
                    result2 = command.ExecuteNonQuery();

                    sql = $"UPDATE AttrDefine SET LockFlag = 1 WHERE LockFlag = 0 AND TmpId = '{templetId}'";
                    command.CommandText = sql;
                    result3 = command.ExecuteNonQuery();

                    sql = $"INSERT INTO SEQ_NO (IND_KEY, NEXT_NO) VALUES ({templetId},1)";
                    command.CommandText = sql;
                    result4 = command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Lock templet error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
                    throw;
                }
                if (result4 == 0)//其它result可能存在等于0的情况,不作例外处理
                {
                    log.Error(string.Format($"Lock templet error!\nsql[{sql}]\n"));
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
                    throw new Exception("Lock templet error!!");
                }

                sql = $"SELECT AttrId,AttrTp FROM AttrDefine WHERE TmpId = '{templetId}'";
                sqlCreate.Append($"CREATE TABLE [{templetId}] (materielIdentfication bigint PRIMARY KEY CLUSTERED");
                command.CommandText = sql;

                using (SqlDataReader sqlDataReader = command.ExecuteReader())
                {
                    if (sqlDataReader.HasRows)
                    {
                        while (sqlDataReader.Read())
                        {
                            sqlCreate.Append(",[").Append(sqlDataReader["AttrId"].ToString().Trim())
                          .Append((sqlDataReader["AttrTp"].ToString().Trim() == "C") ? "] varchar (50) COLLATE Chinese_PRC_CI_AS" : "] decimal(18,4)");
                        }
                        sqlDataReader.Close();
                        sqlCreate.Append(") ON [PRIMARY]");
                        sql = sqlCreate.ToString();
                        command.CommandText = sql;
                        try
                        {
                            result1 = command.ExecuteNonQuery();
                        }
                        catch (Exception e)
                        {
                            log.Error(string.Format($"Create table error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                            sqlTransaction.Rollback();
                            DBConnection.CloseConnection(sqlConnection);
                            throw;
                        }
                        if (result1 == 0)
                        {
                            log.Error(string.Format($"Create table error!!\nsql[{sql}]\n"));
                            sqlTransaction.Rollback();
                            DBConnection.CloseConnection(sqlConnection);
                            throw new Exception("Lock templet error!!");
                        }
                    }
                }
                sqlTransaction.Commit();
            }
            DBConnection.CloseConnection(sqlConnection);
        }

        public void BatchProcess()
        {

        }
    }
}
