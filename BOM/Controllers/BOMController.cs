using BOM.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Data.Common;
using BOM.DbAccess;

namespace BOM.Controllers
{
    [RoutePrefix("BOM")]
    public class BOMController : ApiController
    {
        log4net.ILog log = log4net.LogManager.GetLogger("Templet");

        [HttpGet]
        [Route("Spread")]
        public List<NodeInfo> SpreadBOM(long pTmpId, int prlSeqNo, long tmpId, int rlSeqNo)
        {
            DbConnection connection = DbUtilities.GetConnection();
            List<NodeInfo> list = new List<NodeInfo>();

            BOMTree bomTree = new BOMTree();
            try
            {
                bomTree.FindChildrenTree(connection, ref list, pTmpId, prlSeqNo, tmpId, rlSeqNo, 1);
            }
            catch (Exception e)
            {
                string logMessage = string.Format($"Spread BOM tree error!\n {e.StackTrace}");
                log.Error(logMessage);
                connection.Close();
                var responseMessge = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(logMessage),
                    ReasonPhrase = "Create BOM Tree ERROR"
                };
                throw new HttpResponseException(responseMessge);
            }
            connection.Close();
            return list;
        }

        [HttpPost]
        [Route("ApplyCode")]
        public string ApplyCode(NodeInfo nodeInfo)
        {

            BOMTree bomTree = new BOMTree();
            try
            {
                return bomTree.ApplyCode(nodeInfo);
            }
            catch (Exception e)
            {
                string logMessage = string.Format($"Applay code error!\n {e.StackTrace}[{e}][{e.Message}]");
                log.Error(logMessage);
                var responseMessge = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(logMessage),
                    ReasonPhrase = "Applay code error"
                };
                throw new HttpResponseException(responseMessge);
            }

        }


        [HttpPost]
        [Route("DeleteNode")]
        public void DeleteNode(long pMaterielId, long materielId, int rlSeqNo)
        {

            BOMTree bomTree = new BOMTree();
            try
            {
                bomTree.DeleteNode(pMaterielId, materielId, rlSeqNo);
            }
            catch (Exception e)
            {
                string logMessage = string.Format($"Delete code error!\n {e.StackTrace}");
                log.Error(logMessage);
                var responseMessge = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(logMessage),
                    ReasonPhrase = "Delete code error"
                };
                throw new HttpResponseException(responseMessge);
            }

        }

        [HttpPost]
        [Route("CreateBOM")]
        public List<NodeInfo> CreateBOM(NodeInfo node)
        {
            DbConnection connection = DbUtilities.GetConnection();
            DbCommand command = connection.CreateCommand();
            DbTransaction transaction = connection.BeginTransaction();

            List<NodeInfo> list = new List<NodeInfo>();

            command.Transaction = transaction;

            BOMTree bomTree = new BOMTree(connection, command, transaction);


            try
            {
                bomTree.CreateBOMTree(ref list, node);
                return list;
            }
            catch (Exception e)
            {
                string logMessage = string.Format($"Create BOM Tree error!\nErrorMsg[{e.Message}]\n {e.StackTrace}");
                log.Error(logMessage);
                var responseMessge = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(logMessage),
                    ReasonPhrase = "Create BOM Tree error!"
                };
                throw new HttpResponseException(responseMessge);
            }
            finally
            {
                command.Dispose();
                connection.Close();
            }

        }

        [HttpPost]
        [Route("SaveBOM")]
        public void SaveBOM(List<NodeInfo> list)
        {
            bool newMaterielId = false;
            DbConnection connection = DbUtilities.GetConnection();
            DbCommand command = connection.CreateCommand();
            DbTransaction transaction = connection.BeginTransaction();
            command.Transaction = transaction;


            BOMTree bomTree = new BOMTree(connection, command, transaction);


            try
            {
                //不同的物料树会包含相同的物料编码吗？？？如果是的话，BOM表主键就不能是materialId,而因该加上PeiTNO,同一BOM数不同节点可能有相同的物料编码吗？
                if (!bomTree.CheckRootNode(list))
                {
                    throw new Exception("根物料编码重复");
                }

                foreach (var node in list)
                {
                    newMaterielId = (node.MaterielId==0L) ? true : false;
                    
                    bomTree.SaveNode(node);

                    //如果保存的节点新生成了物料编码,则该物料编码要保存到其子节点的父物料编码字段中.
                    if (newMaterielId)
                    {
                       foreach(var cnode in  list.Where(m => m.NodeLevel == (node.NodeLevel + 1) && 
                                                             m.PTmpId == node.TmpId && 
                                                             m.PrlSeqNo == node.rlSeqNo))
                        {
                            cnode.pMaterielId = node.MaterielId;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                string logMessage = string.Format($"Save BOM Tree error!\n {e.StackTrace}");
                log.Error(logMessage);
                transaction.Rollback();
                command.Dispose();
                connection.Close();
                var responseMessge = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(logMessage),
                    ReasonPhrase = "Save BOM Tree error!"
                };
                throw new HttpResponseException(responseMessge);
            }
            transaction.Commit();
            command.Dispose();
            connection.Close();
        }
    }
}
