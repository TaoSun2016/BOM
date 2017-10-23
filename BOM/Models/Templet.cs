using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace BOM.Models
{
    public partial class Templet
    {
        log4net.ILog log = log4net.LogManager.GetLogger("Templet");

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
                sql = $"INSERT INTO TmpInfo (TmpId, TmpNm, Root, LockCount, CrtDate, Crter, EditLock, DCM) VALUES ('{tmpId}', '{templetName}', '0', 0, {DateTime.Now}, '{creater}', 0, 0) ";
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
                sql = $"INSERT INTO Relation (TmpId, CTmpId, LockFlag, CrtDate, Crter, rlSeqNo) VALUES ('99', '{tmpId}', 0,  {DateTime.Now}, '{creater}', 0) ";
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
            DBConnection.CloseConnection(sqlConnection);
        }

        //新建物料信息(非根物料节点,无参考模板,上送父模板ID和新模板名称)
        public void CreateTemplet(string parentTempletId, string templetName, string creater)
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

                //登记新模板信息
                SeqNo seqNo = new SeqNo();
                string tmpId = seqNo.GetBaseSeqNo();
                sql = $"INSERT INTO TmpInfo (TmpId, TmpNm, Root, LockCount, CrtDate, Crter, EditLock, DCM) VALUES ('{tmpId}', '{templetName}', '0', 0, {DateTime.Now}, '{creater}', 0, 0) ";
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
                sql = $"INSERT INTO RELATION (TmpId, CTmpId, CTmpNum, LockFlag, CrtDate, Crter, rlSeqNo) VALUES ('{parentTempletId}', '{tmpId}', 0, 0, {DateTime.Now}, creater,{rlSeqNo})";
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
                rlSeqNo = -1;
                sql = $"SELECT ISNULL(MAX(rlSeqNo),-1) FROM AttrPass WHERE TmpId = '{parentTempletId}' and CTmpId = '{tmpId}'";
                command.CommandText = sql;
                try
                {
                    rlSeqNo = (int)command.ExecuteScalar();
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Select AttrPass error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
                    throw;
                }
                rlSeqNo++;

                sql = $"INSERT INTO AttrPass (TmpId, CTmpId, CAttrId, CAttrValue, CrtDate, Crter, rlSeqNo) VALUES ('{parentTempletId}','{templetName}','_danyl',0,{DateTime.Now},creater,{rlSeqNo})";
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

        //新建物料信息(非根物料节点,有参考模板,父模板,新模板)
        public void CreateCopiedTemplet(string parentTempletId, string referenceTempletId, String NewTmpletId, string NewTmpletName, string creater)
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

                //1 赋值模板信息
                //1.1 insert into AttrPass
                sql = $"INSERT INTO AttrPass (TmpId, CTmpId, CAttrId, CAttrValue, ValueTp, PAttrId, Gt, Lt, Eq, Excld, Gteq, Lteq, CrtDate, Crter, rlSeqNo) SELECT '{NewTmpletId}', CTmpId, CAttrId, CAttrValue, ValueTp, PAttrId, Gt, Lt, Eq, Excld, Gteq, Lteq, {DateTime.Now}, '{creater}', rlSeqNo FROM attrpass WHERE TmpId='{referenceTempletId}'";
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

                //1.2 insert into AttrDefine
                sql = $"INSERT INTO AttrDefine (TmpId, CTmpId, CAttrId, CAttrValue, ValueTp, CrtDate, Crter) SELECT '{NewTmpletId}', CTmpId, CAttrId, CAttrValue, ValueTp, {DateTime.Now}, '{creater}', FROM AttrDefine WHERE TmpId='{referenceTempletId}'";
                command.CommandText = sql;
                try
                {
                    result = command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Insert table AttrDefine error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
                    throw;
                }
                if (result == 0)
                {
                    log.Error(string.Format($"Insert table AttrDefine 0 record\nsql[{sql}]\n"));
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
                    throw new Exception("Insert table AttrDefine 0 record!");
                }

                //1.3 insert into Relation
                sql = $"INSERT INTO Relation (TmpId, CTmpId, CTmpNum, LockFlag, rlSeqNo CrtDate, Crter) SELECT '{NewTmpletId}', CTmpId, CTmpNum, 0, rlSeqNo, {DateTime.Now}, '{creater}', FROM Relation WHERE TmpId='{referenceTempletId}'";
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
                    log.Error(string.Format($"Insert table Relation 0 record\nsql[{sql}]\n"));
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
                    throw new Exception("Insert table Relation 0 record!");
                }


                //登记数据库表Relation

                //获取Relation.rlSeqNo的值
                sql = $"SELECT ISNULL(MAX(rlSeqNo),-1) FROM RELATION WHERE TmpId = '{parentTempletId}' and CTmpId = '{NewTmpletId}'";
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
                sql = $"INSERT INTO RELATION (TmpId, CTmpId, CTmpNum, LockFlag, CrtDate, Crter, rlSeqNo) VALUES ('{parentTempletId}', '{NewTmpletId}', 1, 0, {DateTime.Now}, creater, {rlSeqNo})";
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
                rlSeqNo = -1;
                sql = $"SELECT ISNULL(MAX(rlSeqNo),-1) FROM AttrPass WHERE TmpId = '{parentTempletId}' and CTmpId = '{NewTmpletId}'";
                command.CommandText = sql;
                try
                {
                    rlSeqNo = (int)command.ExecuteScalar();
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Select AttrPass error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
                    throw;
                }
                rlSeqNo++;

                sql = $"INSERT INTO AttrPass (TmpId, CTmpId, CAttrId, CAttrValue, CrtDate, Crter, rlSeqNo) VALUES ('{parentTempletId}', '{NewTmpletId}', '_danyl', 0, {DateTime.Now}, creater,{rlSeqNo})";
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
    }
}
