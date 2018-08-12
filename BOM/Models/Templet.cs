using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using BOM.DbAccess;
using System.Data.Common;

namespace BOM.Models
{
    public partial class Templet
    {
        log4net.ILog log = log4net.LogManager.GetLogger("Templet");

        //�½�������ģ��
        public void CreateTemplet(string templetName, string creater)
        {
            int result = 0;
            string sql = null;

            DbConnection connection = DbUtilities.GetConnection();
            DbTransaction transaction = connection.BeginTransaction();
            using (DbCommand command =connection.CreateCommand())
            {
                command.Transaction = transaction;

                //���ģ�������Ƿ��ظ�
                sql = $"SELECT COUNT(*) FROM TmpInfo WHERE TmpNm = '{templetName}' ";
                command.CommandText = sql;
                try
                {
                    result = (int)command.ExecuteScalar();
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Select from table TmpInfo error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    connection.Close();
                    throw;
                }

                if (result > 0)
                {
                    log.Error(string.Format($"ģ�������ظ�!Name=[{templetName}]\nsql[{sql}]\n"));
                    connection.Close();
                    throw new Exception("ģ�������ظ�!");
                }

                //�Ǽ�ģ����Ϣ
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
                    transaction.Rollback();
                    connection.Close();
                    throw;
                }

                if (result == 0)
                {
                    log.Error(string.Format($"����TmpInfo��¼��Ϊ0\nsql[{sql}]\n"));
                    transaction.Rollback();
                    connection.Close();
                    throw new Exception("����TmpInfo��¼��Ϊ0!");
                }

                //�ǼǸ�����ģ���Relation,TmpId='99',��ϵͳΪ'root'
                sql = $"INSERT INTO Relation (TmpId, CTmpId, CTmpNum, LockFlag, CrtDate, Crter, rlSeqNo) VALUES (99, {tmpId}, 0, 0, '{DateTime.Now}', '{creater}', 0) ";
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
        }

        //�½�������Ϣ(�Ǹ����Ͻڵ�,�޲ο�ģ��,���͸�ģ��ID����ģ������)
        public void CreateTemplet(long parentTempletId, string templetName, string creater)
        {
            int result = 0;
            int rlSeqNo = -1;
            string sql = null;

            DbConnection connection = DbUtilities.GetConnection();
            DbTransaction transaction = connection.BeginTransaction();
            using (DbCommand command = connection.CreateCommand())
            {
                command.Transaction = transaction;

                //��������ظ�ģ������
                sql = $"SELECT COUNT(*) FROM TmpInfo WHERE TmpNm = '{templetName}' ";
                command.CommandText = sql;
                try
                {
                    result = (int)command.ExecuteScalar();
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Select from table TmpInfo error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    connection.Close();
                    throw;
                }

                if (result > 0)
                {
                    log.Error(string.Format($"ģ�������ظ�!Name=[{templetName}]\nsql[{sql}]\n"));
                    connection.Close();
                    throw new Exception("ģ�������ظ�!");
                }

                //��鸸ģ���Ƿ����
                sql = $"SELECT COUNT(*) FROM TmpInfo WHERE TmpId = '{parentTempletId}' ";
                command.CommandText = sql;
                try
                {
                    result = (int)command.ExecuteScalar();
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Select from table TmpInfo error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    connection.Close();
                    throw;
                }

                if (result != 1)
                {
                    log.Error(string.Format($"��ģ�岻����!TmpId=[{parentTempletId}]\nsql[{sql}]\n"));
                    connection.Close();
                    throw new Exception("��ģ�岻����!");
                }

                //�Ǽ���ģ����Ϣ
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
                    transaction.Rollback();
                    connection.Close();
                    throw;
                }

                if (result == 0)
                {
                    log.Error(string.Format($"����TmpInfo��¼��Ϊ0\nsql[{sql}]\n"));
                    transaction.Rollback();
                    connection.Close();
                    throw new Exception("����TmpInfo��¼��Ϊ0!");
                }


                //�ǼǸ���ģ���ϵ��Relation
                //��ȡRelation.rlSeqNo��ֵ
                sql = $"SELECT ISNULL(MAX(rlSeqNo),-1) FROM RELATION WHERE TmpId = '{parentTempletId}' and CTmpId = '{tmpId}'";
                command.CommandText = sql;
                try
                {
                    rlSeqNo = (int)command.ExecuteScalar();
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Select Table Relation error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    connection.Close();
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
                    transaction.Rollback();
                    connection.Close();
                    throw;
                }
                if (result == 0)
                {
                    log.Error(string.Format($"����Relation���¼��Ϊ0\nsql[{sql}]\n"));
                    transaction.Rollback();
                    connection.Close();
                    throw new Exception("����Relation���¼��Ϊ0!");
                }


                //�Ǽ����ݿ��AttrPass
               
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
        }

        //�½�������Ϣ(�Ǹ����Ͻڵ�,�вο�ģ��,��ģ��)
        public void CreateCopiedTemplet(long parentTempletId, long referenceTempletId, string creater)
        {
            int result = 0;
            int rlSeqNo = -1;
            string sql = null;

            DbConnection connection = DbUtilities.GetConnection();
            DbTransaction transaction = connection.BeginTransaction();
            using (DbCommand command = connection.CreateCommand())
            {
                command.Transaction = transaction;

               
                //�Ǽ����ݿ��Relation

                //��ȡRelation.rlSeqNo��ֵ
                sql = $"SELECT ISNULL(MAX(rlSeqNo),-1) FROM RELATION WHERE TmpId = '{parentTempletId}' and CTmpId = '{referenceTempletId}'";
                command.CommandText = sql;
                try
                {
                    rlSeqNo = (int)command.ExecuteScalar();
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
                sql = $"INSERT INTO RELATION (TmpId, CTmpId, CTmpNum, LockFlag, CrtDate, Crter, rlSeqNo) VALUES ({parentTempletId}, {referenceTempletId}, 0, 0, '{DateTime.Now}', '{creater}', {rlSeqNo})";
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
                    log.Error(string.Format($"����Relation���¼��Ϊ0\nsql[{sql}]\n"));
                    transaction.Rollback();
                    connection.Close();
                    throw new Exception("����Relation���¼��Ϊ0!");
                }


                //�Ǽ����ݿ��AttrPass
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

        //ɾ��ģ��ڵ�
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
                sql = $"DELETE FROM  AttrPass  where TmpId = '{parentTempletId}' and CTmpId = '{TempletId}' and rlseqno = {rlSeqNo}";
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

        //����ģ��
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
                    sql = $"UPDATE TmpInfo SET LockCount = LockCount+1 WHERE TmpId = '{templetId}'";
                    command.CommandText = sql;
                    result1 = command.ExecuteNonQuery();
                    if (result1 == 0)//�޴�ģ��
                    {
                        log.Error(string.Format($"�޴�ģ��!\nsql[{sql}]\n"));
                        throw new Exception("Lock templet error!!�޴�ģ��");
                    }
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
                    transaction.Rollback();
                    connection.Close();
                    throw;
                }
                if ( result4 == 0)//����result���ܴ��ڵ���0�����,�������⴦��
                {
                    log.Error(string.Format($"Lock templet error!\nsql[{sql}]\n"));
                    transaction.Rollback();
                    connection.Close();
                    throw new Exception("Lock templet error!!");
                }

                sql = $"SELECT AttrId,AttrTp FROM AttrDefine WHERE TmpId = '{templetId}'";
                sqlCreate.Append($"CREATE TABLE [{templetId}] (materielIdentfication bigint PRIMARY KEY CLUSTERED");
                command.CommandText = sql;

                using (DbDataReader sqlDataReader = command.ExecuteReader())
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
                            transaction.Rollback();
                            connection.Close();
                            throw;
                        }
                        if (result1 == 0)
                        {
                            log.Error(string.Format($"Create table error!!\nsql[{sql}]\n"));
                            transaction.Rollback();
                            connection.Close();
                            throw new Exception("Lock templet error!!");
                        }
                    }
                }
                transaction.Commit();
            }
            connection.Close();
        }
    }
}
