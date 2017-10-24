using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

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

            SqlConnection sqlConnection = DBConnection.OpenConnection();
            SqlTransaction sqlTransaction = sqlConnection.BeginTransaction();
            using (SqlCommand command = new SqlCommand())
            {
                command.Connection = sqlConnection;
                command.Transaction = sqlTransaction;

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
                    DBConnection.CloseConnection(sqlConnection);
                    throw;
                }

                if (result > 0)
                {
                    log.Error(string.Format($"ģ�������ظ�!Name=[{templetName}]\nsql[{sql}]\n"));
                    DBConnection.CloseConnection(sqlConnection);
                    throw new Exception("ģ�������ظ�!");
                }

                //�Ǽ�ģ����Ϣ
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
                    log.Error(string.Format($"����TmpInfo��¼��Ϊ0\nsql[{sql}]\n"));
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
                    throw new Exception("����TmpInfo��¼��Ϊ0!");
                }

                //�ǼǸ�����ģ���Relation,TmpId='99',��ϵͳΪ'root'
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

        //�½�������Ϣ(�Ǹ����Ͻڵ�,�޲ο�ģ��,���͸�ģ��ID����ģ������)
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
                    DBConnection.CloseConnection(sqlConnection);
                    throw;
                }

                if (result > 0)
                {
                    log.Error(string.Format($"ģ�������ظ�!Name=[{templetName}]\nsql[{sql}]\n"));
                    DBConnection.CloseConnection(sqlConnection);
                    throw new Exception("ģ�������ظ�!");
                }

                //�Ǽ���ģ����Ϣ
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
                    log.Error(string.Format($"����TmpInfo��¼��Ϊ0\nsql[{sql}]\n"));
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
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
                    log.Error(string.Format($"����Relation���¼��Ϊ0\nsql[{sql}]\n"));
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
                    throw new Exception("����Relation���¼��Ϊ0!");
                }


                //�Ǽ����ݿ��AttrPass
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

        //�½�������Ϣ(�Ǹ����Ͻڵ�,�вο�ģ��,��ģ��,��ģ��)
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

                //1 ��ֵģ����Ϣ
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


                //�Ǽ����ݿ��Relation

                //��ȡRelation.rlSeqNo��ֵ
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
                    log.Error(string.Format($"����Relation���¼��Ϊ0\nsql[{sql}]\n"));
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
                    throw new Exception("����Relation���¼��Ϊ0!");
                }


                //�Ǽ����ݿ��AttrPass
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

        //ɾ��ģ��ڵ�
        public void DeleteTemplet(string parentTempletId, string TempletId, int sequenceNo)
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
                sql = $"DELETE FROM  AttrPass  where TmpId = '{parentTempletId}' and CTmpId = '{TempletId}' and rlseqno = {sequenceNo}";
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
                sql = $"DELETE FROM  Relation  where TmpId = '{parentTempletId}' and CTmpId = '{TempletId}' and rlseqno = {sequenceNo}";
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

        //����ģ��
        public void LockTemplet(string templetId)
        {
            int result1 = 0;
            int result2 = 0;
            int result3 = 0;

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

                    sql = $"UPDATE Relation SET LockFlag = 1 WHERE LockFlag = 0 AND TmpId = '{templetId}'";
                    command.CommandText = sql;
                    result2 = command.ExecuteNonQuery();

                    sql = $"INSERT INTO SEQ_NO (IND_KEY, NEXT_NO) VALUES('{templetId}',1)";
                    command.CommandText = sql;
                    result3 = command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Lock templet error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
                    throw;
                }
                if (result1 == 0 || result2 == 0 || result3 == 0)
                {
                    log.Error(string.Format($"Lock templet error!\nsql[{sql}]\n"));
                    sqlTransaction.Rollback();
                    DBConnection.CloseConnection(sqlConnection);
                    throw new Exception("Lock templet error!!");
                }

                sql = $"SELECT AttrId,AttrTp FROM AttrDefine WHERE TmpId = '{templetId}'";
                sqlCreate.Append($"CREATE TABLE [{templetId}] (materielIdentfication varchar (50) COLLATE Chinese_PRC_CI_AS PRIMARY KEY CLUSTERED");
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
                        sqlCreate.Append(") ON [PRIMARY]");

                        command.CommandText = sqlCreate.ToString();
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
