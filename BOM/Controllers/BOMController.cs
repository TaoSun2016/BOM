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
        public List<NodeInfo> SpreadBOM(string tmpId, int rlSeqNo)
        {
            SqlConnection sqlConnection = DBConnection.OpenConnection();
            List<NodeInfo> list = new List<NodeInfo>();
            
            BOMTree bomTree = new BOMTree();
            try
            {
                bomTree.FindChildrenTree(sqlConnection, ref list, tmpId, rlSeqNo, 1);
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
    }
}
