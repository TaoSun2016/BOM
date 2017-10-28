using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace BOM.Models
{
    public partial class AttrDefineList
    {
        public void ExecuteBatch(List<AttrDefine> list)
        {
            int result = 0;
            string sql = null;
            log4net.ILog log = log4net.LogManager.GetLogger("attributeList");

            SqlConnection sqlConnection = DBConnection.OpenConnection();
            SqlTransaction sqlTransaction = sqlConnection.BeginTransaction();
            using (SqlCommand command = new SqlCommand())
            {
                command.Connection = sqlConnection;
                command.Transaction = sqlTransaction;


                foreach (AttrDefine attribute in list)
                {
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
                        default:
                            log.Error(string.Format($"数据操作选项错误!\nOption[{attribute.Option}]"));
                            sqlTransaction.Rollback();
                            DBConnection.CloseConnection(sqlConnection);
                            throw new Exception("数据操作选项错误!");
                    }
                    command.CommandText = sql;
                    try
                    {
                        result = command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        log.Error(string.Format($"Update attrdefine error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                        sqlTransaction.Rollback();
                        DBConnection.CloseConnection(sqlConnection);
                        throw;
                    }


                    if (result == 0)
                    {
                        log.Error(string.Format($"无记录受影响!\nsql[{sql}]\n"));
                        sqlTransaction.Rollback();
                        DBConnection.CloseConnection(sqlConnection);
                        throw new Exception("无记录受影响!");
                    }
                }
                sqlTransaction.Commit();
            }
            DBConnection.CloseConnection(sqlConnection);
        }
    }
}
