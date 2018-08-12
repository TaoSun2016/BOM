using BOM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Web.Http;

namespace BOM.Controllers
{
    [RoutePrefix("Attribute")]
    public class AttrDefineOperationController : ApiController
    {
        log4net.ILog log = log4net.LogManager.GetLogger("Attribute");

        [HttpPost]
        [Route("Batch")]
        public void Batch(List<AttrDefine> list)
        {
            AttrDefineList attrDefineList = new AttrDefineList();
            try
            {
                attrDefineList.ExecuteBatch(list);
            }
            catch (Exception e)
            {
                string logMessage = string.Format($"批量更新AttrDefine出错!\n {e.StackTrace}");
                log.Error(logMessage);
                var responseMessge = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(logMessage),
                    ReasonPhrase = "Batch update AttrDefine ERROR"
                };
                throw new HttpResponseException(responseMessge);
            }

        }
    }
}
