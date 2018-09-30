using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using BOM.DbAccess;
using System.Data.Common;
using System.Configuration;

namespace BOM.Models
{
    public partial class Templet
    {
        log4net.ILog log = log4net.LogManager.GetLogger("Templet");
        string dbType = ConfigurationManager.AppSettings["DbType"]; 

        //新建根物料模板
        public string CreateTemplet(string templetName, string creater)
        {
            string tmpId = "";
            int result = 0;
            string sql = null;

            DbConnection connection = DbUtilities.GetConnection();
            DbTransaction transaction = connection.BeginTransaction();
            using (DbCommand command =connection.CreateCommand())
            {
                command.Transaction = transaction;

                //检查模板名称是否重复
                /* 李爱军要求不检查重复 20180930
                sql = $"SELECT COUNT(*) FROM TmpInfo WHERE TmpNm = '{templetName}' ";
                command.CommandText = sql;
                try
                {
                    result = Convert.ToInt32(command.ExecuteScalar());
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Select from table TmpInfo error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    connection.Close();
                    throw;
                }

                if (result > 0)
                {
                    log.Error(string.Format($"模板名称重复!Name=[{templetName}]\nsql[{sql}]\n"));
                    connection.Close();
                    throw new Exception("模板名称重复!");
                }
                */

                //登记模板信息
                SeqNo seqNo = new SeqNo();
                tmpId = seqNo.GetBaseSeqNo();
                
                sql = $"INSERT INTO TmpInfo (TmpId, TmpNm, Root, LockCount, CrtDate, Crter, EditLock, DCM) VALUES ({tmpId}, '{templetName}', '0', 0, '{DateTime.Now}', '{creater}', 0, 0) ";
                command.CommandText = sql;
                try
                {
                    result = command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Insert table TmpInfo error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    transaction.Rollback();
                    connection.Close();
                    throw;
                }

                if (result == 0)
                {
                    log.Error(string.Format($"插入TmpInfo记录数为0\nsql[{sql}]\n"));
                    transaction.Rollback();
                    connection.Close();
                    throw new Exception("插入TmpInfo记录数为0!");
                }

                //登记根物料模板表Relation,TmpId='99',旧系统为'root'
                sql = $"INSERT INTO Relation (TmpId, CTmpId, CTmpNm, CTmpNum, LockFlag, CrtDate, Crter, rlSeqNo) VALUES (99, {tmpId}, '{templetName}',0, 0, '{DateTime.Now}', '{creater}', 0) ";
                command.CommandText = sql;
                try
                {
                    result = command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Insert table Relation error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    transaction.Rollback();
                    connection.Close();
                    throw;
                }

                if (result == 0)
                {
                    log.Error(string.Format($"Insert table Relation 0 record!\nsql[{sql}]\n"));
                    transaction.Rollback();
                    connection.Close();
                    throw new Exception("Insert table Relation 0 record!");
                }
            }
            transaction.Commit();
            connection.Close();
            return tmpId;
        }

        //新建物料信息(非根物料节点,无参考模板,上送父模板ID和新模板名称)
        public string CreateTemplet(long parentTempletId, string templetName, string creater)
        {
            int result = 0;
            int rlSeqNo = -1;
            string sql = null;
            string tmpId = "";

            DbConnection connection = DbUtilities.GetConnection();
            DbTransaction transaction = connection.BeginTransaction();
            using (DbCommand command = connection.CreateCommand())
            {
                command.Transaction = transaction;

                //检查有无重复模板名称
                /* 李爱军要求不检查重复 20180930
                sql = $"SELECT COUNT(*) FROM TmpInfo WHERE TmpNm = '{templetName}' ";
                command.CommandText = sql;
                try
                {
                    result = Convert.ToInt32( command.ExecuteScalar());
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Select from table TmpInfo error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    connection.Close();
                    throw;
                }

                if (result > 0)
                {
                    log.Error(string.Format($"模板名称重复!Name=[{templetName}]\nsql[{sql}]\n"));
                    connection.Close();
                    throw new Exception("模板名称重复!");
                }
                */

                //检查父模板是否存在
                sql = $"SELECT COUNT(*) FROM TmpInfo WHERE TmpId = '{parentTempletId}' ";
                command.CommandText = sql;
                try
                {
                    result = Convert.ToInt32(command.ExecuteScalar());
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Select from table TmpInfo error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    connection.Close();
                    throw;
                }

                if (result != 1)
                {
                    log.Error(string.Format($"父模板不存在!TmpId=[{parentTempletId}]\nsql[{sql}]\n"));
                    connection.Close();
                    throw new Exception("父模板不存在!");
                }

                //登记新模板信息
                SeqNo seqNo = new SeqNo();
                tmpId = seqNo.GetBaseSeqNo();
                sql = $"INSERT INTO TmpInfo (TmpId, TmpNm, Root, LockCount, CrtDate, Crter, EditLock, DCM) VALUES ({tmpId}, '{templetName}', '0', 0, '{DateTime.Now}', '{creater}', 0, 0) ";
                command.CommandText = sql;
                try
                {
                    result = command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Insert table TmpInfo error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    transaction.Rollback();
                    connection.Close();
                    throw;
                }

                if (result == 0)
                {
                    log.Error(string.Format($"插入TmpInfo记录数为0\nsql[{sql}]\n"));
                    transaction.Rollback();
                    connection.Close();
                    throw new Exception("插入TmpInfo记录数为0!");
                }


                //登记父子模板关系表Relation
                //获取Relation.rlSeqNo的值
                if ("MySQL" == dbType)
                {
                    sql = $"SELECT IFNULL(MAX(rlSeqNo),-1) FROM RELATION WHERE TmpId = '{parentTempletId}' and CTmpId = '{tmpId}'";

                }
                else
                {
                    sql = $"SELECT ISNULL(MAX(rlSeqNo),-1) FROM RELATION WHERE TmpId = '{parentTempletId}' and CTmpId = '{tmpId}'";

                }
                command.CommandText = sql;
                try
                {
                    rlSeqNo = Convert.ToInt32(command.ExecuteScalar());
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Select Table Relation error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    connection.Close();
                    throw;
                }
                rlSeqNo++;

                //Insert Relation
                sql = $"INSERT INTO RELATION (TmpId, CTmpId, CTmpNm, CTmpNum, LockFlag, CrtDate, Crter, rlSeqNo) VALUES ({parentTempletId}, {tmpId}, '{templetName}',0, 0, '{DateTime.Now}', '{creater}',{rlSeqNo})";
                command.CommandText = sql;
                try
                {
                    result = command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Insert table Relation error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    transaction.Rollback();
                    connection.Close();
                    throw;
                }
                if (result == 0)
                {
                    log.Error(string.Format($"插入Relation表记录数为0\nsql[{sql}]\n"));
                    transaction.Rollback();
                    connection.Close();
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
                    transaction.Rollback();
                    connection.Close();
                    throw;
                }
                if (result == 0)
                {
                    log.Error(string.Format($"Insert table Attrpass 0 record\nsql[{sql}]\n"));
                    transaction.Rollback();
                    connection.Close();
                    throw new Exception("Insert table Attrpass 0 record!");
                }
                transaction.Commit();

            }
            connection.Close();
            return tmpId;
        }

        //新建物料信息(非根物料节点,有参考模板,父模板)
        public void CreateCopiedTemplet(long parentTempletId, long referenceTempletId, string creater)
        {
            int result = 0;
            int rlSeqNo = -1;
            string sql = null;
            string tmpNm = null;

            DbConnection connection = DbUtilities.GetConnection();
            DbTransaction transaction = connection.BeginTransaction();
            using (DbCommand command = connection.CreateCommand())
            {
                command.Transaction = transaction;


                //获取子模版名称
                sql = $"select TmpNm from tmpinfo where tmpid = {referenceTempletId}";
                command.CommandText = sql;
                try
                {
                    tmpNm = Convert.ToString(command.ExecuteScalar());
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Select Table TmpInfo error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    transaction.Rollback();
                    connection.Close();
                    throw;
                }

                //获取Relation.rlSeqNo的值
                if ("MySQL" == dbType)
                {
                    sql = $"SELECT IFNULL(MAX(rlSeqNo),-1) FROM RELATION WHERE TmpId = {parentTempletId} and CTmpId = {referenceTempletId}";

                }
                else
                {
                    sql = $"SELECT ISNULL(MAX(rlSeqNo),-1) FROM RELATION WHERE TmpId = {parentTempletId} and CTmpId = {referenceTempletId}";

                }
                command.CommandText = sql;
                try
                {
                    rlSeqNo = Convert.ToInt32(command.ExecuteScalar());
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Select Table Relation error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    transaction.Rollback();
                    connection.Close();
                    throw;
                }
                rlSeqNo++;

                //Insert Relation
                sql = $"INSERT INTO RELATION (TmpId, CTmpId, CTmpNm, CTmpNum, LockFlag, CrtDate, Crter, rlSeqNo) VALUES ({parentTempletId}, {referenceTempletId}, '{tmpNm}',0, 0, '{DateTime.Now}', '{creater}', {rlSeqNo})";
                command.CommandText = sql;
                try
                {
                    result = command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Insert table Relation error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    transaction.Rollback();
                    connection.Close();
                    throw;
                }
                if (result == 0)
                {
                    log.Error(string.Format($"插入Relation表记录数为0\nsql[{sql}]\n"));
                    transaction.Rollback();
                    connection.Close();
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
                    transaction.Rollback();
                    connection.Close();
                    throw;
                }
                if (result == 0)
                {
                    log.Error(string.Format($"Insert table Attrpass 0 record\nsql[{sql}]\n"));
                    transaction.Rollback();
                    connection.Close();
                    throw new Exception("Insert table Attrpass 0 record!");
                }
                transaction.Commit();

            }
            connection.Close();
        }

        //删除模板节点
        public void DeleteTemplet(long parentTempletId, long TempletId, int rlSeqNo)
        {
            int result = 0;
            string sql = null;

            DbConnection connection = DbUtilities.GetConnection();
            DbTransaction transaction = connection.BeginTransaction();
            using (DbCommand command = connection.CreateCommand())
            {
                command.Transaction = transaction;

                //Delete AttrPass
                if (parentTempletId != 99) //根物料在AttrPass表中没有数据
                {
                    sql = $"DELETE FROM  AttrPass  where TmpId = {parentTempletId} and CTmpId = {TempletId} and rlseqno = {rlSeqNo}";
                    command.CommandText = sql;
                    try
                    {
                        result = command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        log.Error(string.Format($"Delete AttrPass error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                        transaction.Rollback();
                        connection.Close();
                        throw;
                    }
                    if (result == 0)
                    {
                        log.Error(string.Format($"Delete Attrpass 0 record\nsql[{sql}]\n"));
                        transaction.Rollback();
                        connection.Close();
                        throw new Exception("Delete Attrpass 0 record!");
                    }
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
                    transaction.Rollback();
                    connection.Close();
                    throw;
                }
                if (result == 0)
                {
                    log.Error(string.Format($"Delete Relation 0 record\nsql[{sql}]\n"));
                    transaction.Rollback();
                    connection.Close();
                    throw new Exception("Delete Relation 0 record!");
                }
                transaction.Commit();
            }
            connection.Close();
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

            DbConnection connection = DbUtilities.GetConnection();
            DbTransaction transaction = connection.BeginTransaction();
            using (DbCommand command = connection.CreateCommand())
            {
                command.Transaction = transaction;

                try
                {
                    sql = $"UPDATE TmpInfo SET LockCount = LockCount+1 WHERE TmpId = {templetId}";
                    command.CommandText = sql;
                    result1 = command.ExecuteNonQuery();
                    if (result1 == 0)//无此模板
                    {
                        log.Error(string.Format($"无此模板!\nsql[{sql}]\n"));
                        throw new Exception("Lock templet error!!无此模板");
                    }
                    sql = $"UPDATE Relation SET LockFlag = 1 WHERE LockFlag = 0 AND CTmpId = {templetId}";
                    command.CommandText = sql;
                    result2 = command.ExecuteNonQuery();

                    sql = $"UPDATE AttrDefine SET LockFlag = 1 WHERE LockFlag = 0 AND TmpId = {templetId}";
                    command.CommandText = sql;
                    result3 = command.ExecuteNonQuery();

                    sql = $"INSERT INTO SEQ_NO (IND_KEY, NEXT_NO) VALUES ({templetId},1)";
                    command.CommandText = sql;
                    result4 = command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Lock templet error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    transaction.Rollback();
                    connection.Close();
                    throw;
                }
                if ( result4 == 0)//其它result可能存在等于0的情况,不作例外处理
                {
                    log.Error(string.Format($"Lock templet error!\nsql[{sql}]\n"));
                    transaction.Rollback();
                    connection.Close();
                    throw new Exception("Lock templet error!!");
                }

                sql = $"SELECT AttrId,AttrTp FROM AttrDefine WHERE TmpId = {templetId}";
                if ("MySQL" == dbType)
                {
                    sqlCreate.Append($"CREATE TABLE `{templetId}` (materielIdentfication bigint PRIMARY KEY");

                }
                else
                {
                    sqlCreate.Append($"CREATE TABLE [{templetId}] (materielIdentfication bigint PRIMARY KEY CLUSTERED");

                }
                command.CommandText = sql;

                using (DbDataReader sqlDataReader = command.ExecuteReader())
                {
                    if (sqlDataReader.HasRows)
                    {
                        while (sqlDataReader.Read())
                        {
                            if ("MySQL" == dbType)
                            {
                                sqlCreate.Append(",`").Append(sqlDataReader["AttrId"].ToString().Trim()).Append((sqlDataReader["AttrTp"].ToString().Trim() == "C") ? "` varchar (50)" : "` decimal(18,4)");
                            }
                            else
                            {
                                sqlCreate.Append(",[").Append(sqlDataReader["AttrId"].ToString().Trim()).Append((sqlDataReader["AttrTp"].ToString().Trim() == "C") ? "] varchar (50) COLLATE Chinese_PRC_CI_AS" : "] decimal(18,4)");
                            }

                        }
                        sqlDataReader.Close();
                        if ("MySQL" == dbType)
                        {
                            sqlCreate.Append(");");

                        }
                        else
                        {
                            sqlCreate.Append(") ON [PRIMARY]");

                        }
                        sql = sqlCreate.ToString();
                        command.CommandText = sql;
                        try
                        {
                            result1 = command.ExecuteNonQuery();
                        }
                        catch (Exception e)
                        {
                            log.Error(string.Format($"Create table error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                            transaction.Rollback();
                            connection.Close();
                            throw;
                        }
                    }
                }
                transaction.Commit();
            }
            connection.Close();
        }
    }
}
