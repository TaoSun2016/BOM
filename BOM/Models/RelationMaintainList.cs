using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;

namespace BOM.Models
{
    public class RelationMaintainList
    {
        List<RelationMaintain> list = new List<RelationMaintain>();
        public RelationMaintainList(List<RelationMaintain> list)
        {
            this.list = list;
        }

        public void ExecuteBatch()
        {
            long TmpId = -1;
            long CTmpId = -1;
            int rlSeqNo = -1;
            string Crter = null;
            string LstUpdter = null;
            int result = 0;
            string sql = null;
            StringBuilder condition = new StringBuilder();

            log4net.ILog log = log4net.LogManager.GetLogger("RelationMaintainList");
            SqlConnection sqlConnection = DBConnection.OpenConnection();
            SqlTransaction sqlTransaction = sqlConnection.BeginTransaction();
            using (SqlCommand command = new SqlCommand())
            {
                command.Connection = sqlConnection;
                command.Transaction = sqlTransaction;
                foreach (var state in list)
                {
                    TmpId = (long)state.TmpId;
                    CTmpId = (long)state.CTmpId;
                    rlSeqNo = (int)state.rlSeqNo;
                    Crter = state.Crter;
                    LstUpdter = state.LstUpdter;
                    log.Info("begin");
                    //Insert into AttrPass
                    if (state.InsertStatements != null)
                    {
                        foreach (var insertState in state.InsertStatements)
                        {
                            sql = $"INSERT INTO ATTRPASS (TmpId,CTmpId,CAttrId,CAttrValue,ValueTp,PAttrId,Eq,Excld, Gt,Gteq,Lt,Lteq, rlSeqNo,CrtDate,Crter) values ({TmpId},{CTmpId},'{insertState.CAttrId}','{insertState.CAttrValue}','{insertState.ValueTp}','{insertState.PAttrId}','{insertState.Eq}','{insertState.Excld}','{insertState.Gt}','{insertState.Gteq}','{insertState.Lt}','{insertState.Lteq}',{rlSeqNo},'{DateTime.Now}','{Crter}')";
                            command.CommandText = sql;
                            log.Info(sql);
                            try
                            {
                                result = command.ExecuteNonQuery();
                            }
                            catch (Exception e)
                            {
                                log.Error(string.Format($"Insert into attrpass error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                                sqlTransaction.Rollback();
                                DBConnection.CloseConnection(sqlConnection);
                                throw;
                            }
                            if (result == 0)
                            {
                                log.Error(string.Format($"No data is inserted into attrpass!\nsql[{sql}]\n"));
                                sqlTransaction.Rollback();
                                DBConnection.CloseConnection(sqlConnection);
                                throw new Exception("No data is inserted into attrpass!");
                            }
                        }
                    }
                    

                    //Update AttrPass details
                    if (state.UpdateDetails != null)
                    {
                        foreach (var updateDetail in state.UpdateDetails)
                        {
                            condition.Remove(0, condition.Length);

                            //OrigGteq
                            if (updateDetail.OrigGteq != "1")
                            {
                                condition.Append(" Gteq != '1' ");
                            }
                            else
                            {
                                condition.Append(" Gteq = '1' ");
                            }

                            //OrigLteq
                            if (updateDetail.OrigLteq != "1")
                            {
                                condition.Append("AND Lteq != '1' ");
                            }
                            else
                            {
                                condition.Append("AND Lteq = '1' ");
                            }

                            //OrigEq
                            if (updateDetail.OrigEq.Length == 0)
                            {
                                condition.Append("AND (Eq = '' or Eq is null) ");
                            }
                            else
                            {
                                condition.Append($"AND Eq = '{updateDetail.OrigEq}' ");
                            }

                            //OrigGt
                            if (updateDetail.OrigGt.Length == 0)
                            {
                                condition.Append("AND (Gt = '' or Gt is null) ");
                            }
                            else
                            {
                                condition.Append($"AND Gt = '{updateDetail.OrigGt}' ");
                            }

                            //OrigLt
                            if (updateDetail.OrigLt.Length == 0)
                            {
                                condition.Append("AND (Lt = '' or Lt is null) ");
                            }
                            else
                            {
                                condition.Append($"AND Lt = '{updateDetail.OrigLt}' ");
                            }

                            //OrigExcld
                            if (updateDetail.OrigExcld.Length == 0)
                            {
                                condition.Append("AND (Excld = '' or Excld is null) ");
                            }
                            else
                            {
                                condition.Append($"AND Excld = '{updateDetail.OrigExcld}' ");
                            }

                            //OrigPAttrId
                            if (updateDetail.OrigPAttrId.Length == 0)
                            {
                                condition.Append("AND (PAttrId = '' or PAttrId is null) ");
                            }
                            else
                            {
                                condition.Append($"AND PAttrId = '{updateDetail.OrigPAttrId}'");
                            }
                            sql = $"UPDATE ATTRPASS SET Eq = '{updateDetail.Eq}', Excld = '{updateDetail.Excld}', Gt = '{updateDetail.Gt}', Gteq = '{updateDetail.Gteq}', Lt = '{updateDetail.Lt}', Lteq = '{updateDetail.Lteq}', LstUpdtDate = '{DateTime.Now}', LstUpdter = '{LstUpdter}' WHERE TmpId = {TmpId} and CTmpId = {CTmpId} and rlSeqNo = {rlSeqNo} and CAttrId = '{updateDetail.OrigCAttrId}' and CAttrValue = '{updateDetail.OrigCAttrValue}' and ValueTp = '{updateDetail.OrigValueTp}' and " + condition.ToString();
                            log.Info(sql);
                            command.CommandText = sql;
                            try
                            {
                                result = command.ExecuteNonQuery();
                            }
                            catch (Exception e)
                            {
                                log.Error(string.Format($"Update attrpass error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                                sqlTransaction.Rollback();
                                DBConnection.CloseConnection(sqlConnection);
                                throw;
                            }
                            if (result == 0)
                            {
                                log.Error(string.Format($"No data is updated in attrpass!\nsql[{sql}]\n"));
                                sqlTransaction.Rollback();
                                DBConnection.CloseConnection(sqlConnection);
                                throw new Exception("No data is updated in attrpass!");
                            }
                        }
                    }


                    //Delete AttrPass details
                    if (state.DeleteDetails != null)
                    {
                        foreach (var deleteDetail in state.DeleteDetails)
                        {
                            condition.Remove(0, condition.Length);

                            //OrigGteq
                            if (deleteDetail.OrigGteq != "1")
                            {
                                condition.Append(" Gteq != '1' ");
                            }
                            else
                            {
                                condition.Append(" Gteq = '1' ");
                            }

                            //OrigLteq
                            if (deleteDetail.OrigLteq != "1")
                            {
                                condition.Append("AND Lteq != '1' ");
                            }
                            else
                            {
                                condition.Append("AND Lteq = '1' ");
                            }

                            //OrigEq
                            if (deleteDetail.OrigEq.Length == 0)
                            {
                                condition.Append("AND (Eq = '' or Eq is null) ");
                            }
                            else
                            {
                                condition.Append($"AND Eq = '{deleteDetail.OrigEq}' ");
                            }

                            //OrigGt
                            if (deleteDetail.OrigGt.Length == 0)
                            {
                                condition.Append("AND (Gt = '' or Gt is null) ");
                            }
                            else
                            {
                                condition.Append($"AND Gt = '{deleteDetail.OrigGt}' ");
                            }

                            //OrigLt
                            if (deleteDetail.OrigLt.Length == 0)
                            {
                                condition.Append("AND (Lt = '' or Lt is null) ");
                            }
                            else
                            {
                                condition.Append($"AND Lt = '{deleteDetail.OrigLt}' ");
                            }

                            //OrigExcld
                            if (deleteDetail.OrigExcld.Length == 0)
                            {
                                condition.Append("AND (Excld = '' or Excld is null) ");
                            }
                            else
                            {
                                condition.Append($"AND Excld = '{deleteDetail.OrigExcld}' ");
                            }

                            //OrigPAttrId
                            if (deleteDetail.OrigPAttrId.Length == 0)
                            {
                                condition.Append("AND (PAttrId = '' or PAttrId is null) ");
                            }
                            else
                            {
                                condition.Append($"AND PAttrId = '{deleteDetail.OrigPAttrId}'");
                            }

                            sql = $"DELETE FROM ATTRPASS WHERE TmpId = {TmpId} and CTmpId = {CTmpId} and rlSeqNo = {rlSeqNo} and CAttrId = '{deleteDetail.OrigCAttrId}' and CAttrValue = '{deleteDetail.OrigCAttrValue}' and ValueTp = '{deleteDetail.OrigValueTp}' and " + condition.ToString();
                            log.Info(sql);
                            command.CommandText = sql;
                            try
                            {
                                result = command.ExecuteNonQuery();
                            }
                            catch (Exception e)
                            {
                                log.Error(string.Format($"Delete from attrpass error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                                sqlTransaction.Rollback();
                                DBConnection.CloseConnection(sqlConnection);
                                throw;
                            }
                            if (result == 0)
                            {
                                log.Error(string.Format($"No data is deleted in attrpass!\nsql[{sql}]\n"));
                                sqlTransaction.Rollback();
                                DBConnection.CloseConnection(sqlConnection);
                                throw new Exception("No data is deleted in attrpass!");
                            }
                        }
                    }


                    //Update sum
                    if (state.UpdateSums != null)
                    {
                        foreach (var updateSum in state.UpdateSums)
                        {
                            sql = $"UPDATE ATTRPASS SET CAttrValue = '{updateSum.CAttrValue}', ValueTp = '{updateSum.ValueTp}', LstUpdtDate = '{DateTime.Now}', LstUpdter = '{LstUpdter}' WHERE TmpId = {TmpId} and CTmpId = {CTmpId} and rlSeqNo = {rlSeqNo} and CAttrId = '{updateSum.OrigCAttrId}' and CAttrValue = '{updateSum.OrigCAttrValue}' and ValueTp = '{updateSum.OrigValueTp}'";

                            log.Info(sql);
                            command.CommandText = sql;
                            try
                            {
                                result = command.ExecuteNonQuery();
                            }
                            catch (Exception e)
                            {
                                log.Error(string.Format($"Update attrpass sum error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                                sqlTransaction.Rollback();
                                DBConnection.CloseConnection(sqlConnection);
                                throw;
                            }
                            if (result == 0)
                            {
                                log.Error(string.Format($"No data is updated in attrpass!\nsql[{sql}]\n"));
                                sqlTransaction.Rollback();
                                DBConnection.CloseConnection(sqlConnection);
                                throw new Exception("No data is updated in attrpass!");
                            }
                        }
                    }


                    //Delete sum
                    if (state.DeleteSums != null)
                    {
                        foreach (var deleteSum in state.DeleteSums)
                        {
                            sql = $"DELETE FROM ATTRPASS WHERE TmpId = {TmpId} and CTmpId = {CTmpId} and rlSeqNo = {rlSeqNo} and CAttrId = '{deleteSum.OrigCAttrId}' and CAttrValue = '{deleteSum.OrigCAttrValue}' and ValueTp = '{deleteSum.OrigValueTp}'";
                            log.Info(sql);
                            command.CommandText = sql;
                            try
                            {
                                result = command.ExecuteNonQuery();
                            }
                            catch (Exception e)
                            {
                                log.Error(string.Format($"Delete attrpass sum error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                                sqlTransaction.Rollback();
                                DBConnection.CloseConnection(sqlConnection);
                                throw;
                            }
                            if (result == 0)
                            {
                                log.Error(string.Format($"No data is deleted in attrpass!\nsql[{sql}]\n"));
                                sqlTransaction.Rollback();
                                DBConnection.CloseConnection(sqlConnection);
                                throw new Exception("No data is deleted in attrpass!");
                            }
                        }
                    }


                    //Update Default
                    if (state.UpdateDefaults != null)
                    {
                        foreach (var updateDefault in state.UpdateDefaults)
                        {
                            bool newFlag = (updateDefault.NewFlag == "1");
                            if (newFlag)
                            {
                                sql = $"INSERT INTO ATTRPASS (Ctmpid,rlseqno,tmpid,cattrid,cattrvalue,valuetp,pattrid) values ({CTmpId},{rlSeqNo},{TmpId},'{updateDefault.CAttrId}','{updateDefault.CAttrValue}',0,0)";
                            }
                            else
                            {
                                sql = $"UPDATE ATTRPASS SET CAttrValue = '{updateDefault.CAttrValue}',LstUpdtDate = '{DateTime.Now}', LstUpdter = '{LstUpdter}' WHERE CTmpId = {CTmpId} AND rlSeqNo='{rlSeqNo}' AND TmpId = {TmpId} AND CAttrId = '{updateDefault.CAttrId}' AND ValueTp = '0'";
                            }

                            command.CommandText = sql;
                            log.Info(sql);
                            try
                            {
                                result = command.ExecuteNonQuery();
                            }
                            catch (Exception e)
                            {
                                log.Error(string.Format(newFlag ? "Insert" : "Update" + $" attrpass default error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                                sqlTransaction.Rollback();
                                DBConnection.CloseConnection(sqlConnection);
                                throw;
                            }
                            if (result == 0)
                            {
                                log.Error(string.Format("No data is " + (newFlag ? "Inserted" : "Updated") + $" in attrpass!\nsql[{sql}]\n"));
                                sqlTransaction.Rollback();
                                DBConnection.CloseConnection(sqlConnection);
                                throw new Exception("No data is " + (newFlag ? "Inserted" : "Updated") + $" in attrpass!");
                            }
                        }
                    }
                    
                }
            }
            sqlTransaction.Commit();
            DBConnection.CloseConnection(sqlConnection);
        }

        public class RelationMaintain
        {
            public long TmpId { get; set; }
            public long CTmpId { get; set; }
            public int rlSeqNo { get; set; }
            public string Crter { get; set; }
            public string LstUpdter { get; set; }
            public List<InsertStatement> InsertStatements { get; set; }
            public List<UpdateDetail> UpdateDetails { get; set; }
            public List<DeleteDetail> DeleteDetails { get; set; }
            public List<UpdateSum> UpdateSums { get; set; }
            public List<DeleteSum> DeleteSums { get; set; }
            public List<UpdateDefault> UpdateDefaults { get; set; }

        }

        public class InsertStatement
        {
            public string CAttrId { get; set; }
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
        public class UpdateDetail
        {
            public string OrigCAttrId { get; set; }
            public string OrigCAttrValue { get; set; }
            public string OrigValueTp { get; set; }
            public string OrigEq { get; set; }
            public string OrigExcld { get; set; }
            public string OrigGt { get; set; }
            public string OrigGteq { get; set; }
            public string OrigLt { get; set; }
            public string OrigLteq { get; set; }
            public string OrigPAttrId { get; set; }
            public string Eq { get; set; }
            public string Excld { get; set; }
            public string Gt { get; set; }
            public string Gteq { get; set; }
            public string Lt { get; set; }
            public string Lteq { get; set; }
        }
        public class DeleteDetail
        {
            public string OrigCAttrId { get; set; }
            public string OrigCAttrValue { get; set; }
            public string OrigValueTp { get; set; }
            public string OrigEq { get; set; }
            public string OrigExcld { get; set; }
            public string OrigGt { get; set; }
            public string OrigGteq { get; set; }
            public string OrigLt { get; set; }
            public string OrigLteq { get; set; }
            public string OrigPAttrId { get; set; }
        }
        public class UpdateSum
        {
            public string OrigCAttrId { get; set; }
            public string OrigCAttrValue { get; set; }
            public string OrigValueTp { get; set; }
            public string CAttrValue { get; set; }
            public string ValueTp { get; set; }
        }

        public class DeleteSum
        {
            public string OrigCAttrId { get; set; }
            public string OrigCAttrValue { get; set; }
            public string OrigValueTp { get; set; }
        }
        public class UpdateDefault
        {
            public string NewFlag { get; set; }
            public string CAttrId { get; set; }
            public string CAttrValue { get; set; }
        }
    }
}