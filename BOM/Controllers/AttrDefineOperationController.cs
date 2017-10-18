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
        AttrDefineOperation operation = new AttrDefineOperation();


        [HttpGet]
        [Route("GetAll")]
        public List<AttrDefine> Get()
        {
            log.Debug(string.Format("time is {0}",DateTime.Now));
            return operation.Query();
        }


        [HttpGet]
        [Route("GetOne")]
        public AttrDefine GetOne(string tmpId, string attrId, string attrNm, string attrTp)
        {
            return operation.QueryOne( tmpId,  attrId,  attrNm,  attrTp);
        }

        [HttpPost]
        [Route("Create")]
        public void Post(AttrDefine attrDefine)
        {
            if (ModelState.IsValid)
            {
                operation.Insert(attrDefine);
            }
            
        }

        [HttpPut]
        [Route("Update")]
        public void Put(string oldTmpId, string oldAttrId, string oldAttrNm, string oldAttrTp, AttrDefine attrDefine)
        {
            operation.Update(oldTmpId,  oldAttrId, oldAttrNm, oldAttrTp, attrDefine.TmpId,attrDefine.AttrId,attrDefine.AttrNm,attrDefine.AttrTp,attrDefine.LstUpdter);
        }

        [HttpDelete]
        [Route("Delete")]
        public void Delete(string tmpId, string attrId, string attrNm, string attrTp)
        {
            operation.Delete(tmpId, attrId, attrNm, attrTp);
        }

        [HttpPost]
        [Route("Batch")]
        public void Batch(AttrDefineList list)
        {
            try
            {
                list.ExecuteBatch();
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
