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
            using (SqlCommand command = new SqlCommand())
            {
                command.Connection = sqlConnection;
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
                    DBConnection.CloseConnection(sqlConnection);
                    throw;
                }

                if (result == 0)
                {
                    log.Error(string.Format($"插入TmpInfo记录数为0\nsql[{sql}]\n"));
                    DBConnection.CloseConnection(sqlConnection);
                    throw new Exception("插入TmpInfo记录数为0!");
                }

            }
            DBConnection.CloseConnection(sqlConnection);
        }

        //新建物料信息(非根物料节点,无参考模板)
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
                    DBConnection.CloseConnection(sqlConnection);
                    throw;
                }

                if (result == 0)
                {
                    log.Error(string.Format($"插入TmpInfo记录数为0\nsql[{sql}]\n"));
                    DBConnection.CloseConnection(sqlConnection);
                    throw new Exception("插入TmpInfo记录数为0!");
                }


                //登记数据库表Relation

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
                sql = $"INSERT INTO RELATION (TmpId, CTmpId, CTmpNum, LockFlag, CrtDate, Crter, rlSeqNo) VALUES ('{parentTempletId}','{templetName}',0,0,{DateTime.Now},creater,{rlSeqNo})";
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

        //新建物料信息(非根物料节点,有参考模板)
        public void CreateCopiedTemplet(string parentTempletId, string referenceTempletId, string creater)
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
                sql = $"INSERT INTO RELATION (TmpId, CTmpId, CTmpNum, LockFlag, CrtDate, Crter, rlSeqNo) VALUES ('{parentTempletId}','{templetName}',0,0,{DateTime.Now},creater,{rlSeqNo})";
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
    }
}
