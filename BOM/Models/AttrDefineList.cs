using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.Common;
using BOM.DbAccess;

namespace BOM.Models
{
    public partial class AttrDefineList
    {
        public void ExecuteBatch(List<AttrDefine> list)
        {
            int result = 0;
            string sql = null;
            log4net.ILog log = log4net.LogManager.GetLogger("attributeList");

            DbConnection connection = DbUtilities.GetConnection();
            DbTransaction transaction = connection.BeginTransaction();
            using (DbCommand command = connection.CreateCommand())
            {
                command.Transaction = transaction;


                foreach (AttrDefine attribute in list)
                {
                    sql = $"SELECT COUNT(*) FROM tmpinfo WHERE tmpid = {attribute.TmpId}";
                    command.CommandText = sql;
                    try
                    {
                        result = Convert.ToInt32(command.ExecuteScalar());
                    }
                    catch (Exception e)
                    {
                        log.Error(string.Format($"Update attrdefine error!\nsql[{sql}]\nError[{e.Message}]"));
                        transaction.Rollback();
                        connection.Close();
                        throw;
                    }
                    if (result == 0)
                    {
                        log.Error(string.Format($"�޴�ģ���!TmpId={attribute.TmpId}\n sql[{sql}]\n"));
                        transaction.Rollback();
                        connection.Close();
                        throw new Exception($"�޴�ģ��[{attribute.TmpId}]!");
                    }

                    switch (attribute.Option)
                    {
                        case 'I':
                            DateTime crtDate = DateTime.Now;
                            sql = $"INSERT INTO AttrDefine (TmpId, AttrId, AttrNm, AttrTp,CrtDate, Crter) Values ({attribute.TmpId},'{attribute.AttrId}', '{attribute.AttrNm}', '{attribute.AttrTp}', '{crtDate}','{attribute.Crter}')";
                            break;
                        case 'U':
                            DateTime updtDate = DateTime.Now;
                            sql = $"UPDATE AttrDefine SET TmpId = {attribute.TmpId} , AttrId='{attribute.AttrId}' , AttrNm='{attribute.AttrNm}' , AttrTp='{attribute.AttrTp}' , LstUpdtDate='{updtDate}' , LstUpdter='{attribute.LstUpdter}'  WHERE TmpId = '{attribute.OrigTmpId}' AND AttrId='{attribute.OrigAttrId}' AND AttrNm='{attribute.OrigAttrNm}' AND AttrTp='{attribute.OrigAttrTp}' AND LockFlag='0'";

                            break;
                        case 'D':
                            sql = $"DELETE FROM AttrDefine WHERE TmpId = {attribute.TmpId} AND AttrId='{attribute.AttrId}' AND AttrNm='{attribute.AttrNm}' AND AttrTp='{attribute.AttrTp}'";
                            break;
                        case 'L':
                            DateTime lockDate = DateTime.Now;
                            sql = $"UPDATE AttrDefine SET LockFlag = '1'  WHERE TmpId = '{attribute.TmpId}' AND AttrId='{attribute.AttrId}' AND AttrNm='{attribute.AttrNm}' AND AttrTp='{attribute.AttrTp}'";
                            break;
                        default:
                            log.Error(string.Format($"���ݲ���ѡ�����!\nOption[{attribute.Option}]"));
                            transaction.Rollback();
                            connection.Close();
                            throw new Exception("���ݲ���ѡ�����!");
                    }
                    command.CommandText = sql;
                    try
                    {
                        result = command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        log.Error(string.Format($"Update attrdefine error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                        transaction.Rollback();
                        connection.Close();
                        throw;
                    }


                    if (result == 0)
                    {
                        log.Error(string.Format($"�޼�¼��Ӱ��!\nsql[{sql}]\n"));
                        transaction.Rollback();
                        connection.Close();
                        throw new Exception("�޼�¼��Ӱ��!");
                    }
                }
                transaction.Commit();
            }
            connection.Close();
        }
    }
}
