using BOM.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace BOM.Controllers
{
    [RoutePrefix("BOM")]
    public class BOMController : ApiController
    {
        log4net.ILog log = log4net.LogManager.GetLogger("Templet");

        [HttpGet]
        [Route("Spread")]
        public List<NodeInfo> SpreadBOM(string pTmpId, string tmpId, int rlSeqNo)
        {
            SqlConnection sqlConnection = DBConnection.OpenConnection();
            List<NodeInfo> list = new List<NodeInfo>();
            
            BOMTree bomTree = new BOMTree();
            try
            {
                bomTree.FindChildrenTree(sqlConnection, ref list, pTmpId, tmpId, rlSeqNo, 1);
            }
            catch (Exception e)
            {
                string logMessage = string.Format($"Spread BOM tree error!\n {e.StackTrace}");
                log.Error(logMessage);
                DBConnection.CloseConnection(sqlConnection);
                var responseMessge = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(logMessage),
                    ReasonPhrase = "Create BOM Tree ERROR"
                };
                throw new HttpResponseException(responseMessge);
            }
            DBConnection.CloseConnection(sqlConnection);
            return list;
        }

        [HttpPost]
        [Route("ApplyCode")]
        public string ApplyCode(NodeInfo nodeInfo)
        {
            
            BOMTree bomTree = new BOMTree();
            try
            {
              return  bomTree.ApplyCode(nodeInfo);
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
                bomTree.DeleteNode( pMaterielId, materielId, rlSeqNo);
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
            SqlConnection sqlConnection = DBConnection.OpenConnection();
            SqlCommand command = new SqlCommand();
            SqlTransaction transaction = sqlConnection.BeginTransaction();
            List<NodeInfo> list = new List<NodeInfo>;

            BOMTree bomTree = new BOMTree(sqlConnection,command,transaction);


            try
            {
                bomTree.CreateBOMTree(ref list, node, node.NodeLevel);
                return list;
            }
            catch (Exception e)
            {
                string logMessage = string.Format($"Create BOM Tree error!\n {e.StackTrace}");
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
                DBConnection.CloseConnection(sqlConnection);
            }

        }

        [HttpPost]
        [Route("SaveBOM")]
        public void SaveBOM(decimal count, List<NodeInfo> list)
        {
            SqlConnection sqlConnection = DBConnection.OpenConnection();
            SqlCommand command = new SqlCommand();
            SqlTransaction transaction = sqlConnection.BeginTransaction();
            NodeInfo node = list.Find(m => m.NodeLevel == 1);

            BOMTree bomTree = new BOMTree(sqlConnection, command, transaction);


            try
            {
                bomTree.SaveBOMTree(list, node, count);
            }
            catch (Exception e)
            {
                string logMessage = string.Format($"Save BOM Tree error!\n {e.StackTrace}");
                log.Error(logMessage);
                var responseMessge = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(logMessage),
                    ReasonPhrase = "Save BOM Tree error!"
                };
                throw new HttpResponseException(responseMessge);
            }
            finally
            {
                command.Dispose();
                DBConnection.CloseConnection(sqlConnection);
            }

        }
    }
}
