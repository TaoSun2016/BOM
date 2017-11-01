using BOM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Web.Http;
using static BOM.Models.RelationMaintainList;

namespace BOM.Controllers
{
    [RoutePrefix("Relation")]
    public class RelationController : ApiController
    {
        log4net.ILog log = log4net.LogManager.GetLogger("Relation");



        
        [HttpPost]
        [Route("Batch")]
        public void Batch(List<RelationMaintain> list)
        {
            RelationMaintainList relations = new RelationMaintainList(list);
            try
            {
                relations.ExecuteBatch();
            }
            catch (Exception e)
            {
                string logMessage = string.Format($"Update AttrPass Error!\n {e.StackTrace}");
                log.Error(logMessage);
                var responseMessge = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(logMessage),
                    ReasonPhrase = "Batch update AttrPass ERROR"
                };
                throw new HttpResponseException(responseMessge);
            }

        }
    }
}
