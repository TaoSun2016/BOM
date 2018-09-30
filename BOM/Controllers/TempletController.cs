using BOM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace BOM.Controllers
{
    [RoutePrefix("Templet")]
    public class TempletController : ApiController
    {
        log4net.ILog log = log4net.LogManager.GetLogger("Templet");
        [HttpPost]
        [Route("CreateRoot")]
        public string CreateRoot(string templetName, string creater)
        {
            
            Templet templet = new Templet();
            try
            {
                return templet.CreateTemplet(templetName, creater);
            }
            catch (Exception e)
            {
                string logMessage = string.Format($"Create root templet error!\n {e.StackTrace}");
                log.Error(logMessage);
                var responseMessge = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(logMessage),
                    ReasonPhrase = "Create root templet ERROR"
                };
                throw new HttpResponseException(responseMessge);
            }      
        }

        [HttpPost]
        [Route("CreateChild")]
        public string CreateChildWithoutTemplet(long parentTempletId,string templetName, string creater)
        {
            Templet templet = new Templet();
            try
            {
                return templet.CreateTemplet(parentTempletId,templetName, creater);
            }
            catch (Exception e)
            {
                string logMessage = string.Format($"Create child templet error!\n {e.StackTrace}");
                log.Error(logMessage);
                var responseMessge = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(logMessage),
                    ReasonPhrase = "Create child templet ERROR"
                };
                throw new HttpResponseException(responseMessge);
            }
        }

        [HttpPost]
        [Route("CreateWithTemplet")]
        public void CreateCopiedTemplet(long parentTempletId, long referenceTempletId, string creater)
        {
            Templet templet = new Templet();
            try
            {
                templet.CreateCopiedTemplet(parentTempletId, referenceTempletId, creater);
            }
            catch (Exception e)
            {
                string logMessage = string.Format($"Create child templet error!\n {e.StackTrace}");
                log.Error(logMessage);
                var responseMessge = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(logMessage),
                    ReasonPhrase = "Create child templet ERROR"
                };
                throw new HttpResponseException(responseMessge);
            }
        }

        [HttpPost]
        [Route("Delete")]
        public void DeleteTemplet(long parentTempletId, long TempletId, int rlSeqNo)
        {
            Templet templet = new Templet();
            try
            {
                templet.DeleteTemplet( parentTempletId, TempletId, rlSeqNo);
            }
            catch (Exception e)
            {
                string logMessage = string.Format($"Delete templet error!\n {e.StackTrace}");
                log.Error(logMessage);
                var responseMessge = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(logMessage),
                    ReasonPhrase = "Delete templet ERROR"
                };
                throw new HttpResponseException(responseMessge);
            }
        }


        [HttpPost]
        [Route("Lock")]
        public void LockTemplet(long templetId)
        {
            Templet templet = new Templet();
            try
            {
                templet.LockTemplet(templetId);
            }
            catch (Exception e)
            {
                string logMessage = string.Format($"Lock templet error!\n {e.StackTrace}");
                log.Error(logMessage);
                var responseMessge = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(logMessage),
                    ReasonPhrase = "Lock templet error"
                };
                throw new HttpResponseException(responseMessge);
            }
        }
    }
}
