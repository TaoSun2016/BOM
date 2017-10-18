using BOM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace BOM.Controllers
{
    [RoutePrefix("SeqNo")]
    public class SeqNoController : ApiController
    {
        log4net.ILog log = log4net.LogManager.GetLogger("SeqNo");
        [HttpGet]
        [Route("Get")]
        public string Get(string tmpId)
        {
            string seqNoString = null;
            try
            {
                SeqNo seqNo = new SeqNo();
                seqNoString = seqNo.GetSeqNo(tmpId);
            }
            catch (Exception e)
            {
                string logMessage = string.Format($"获取序号出错!TmpId[{tmpId}]\n {e.StackTrace}");
                log.Error(logMessage);
                var responseMessge = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(logMessage),
                    ReasonPhrase = "Get SEQ_NO ERROR"
                };
                throw new HttpResponseException(responseMessge);
            }
            return seqNoString;
            
        }

    }
}
