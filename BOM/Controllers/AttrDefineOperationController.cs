using BOM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace BOM.Controllers
{
    [RoutePrefix("Attribute")]
    public class AttrDefineOperationController : ApiController
    {
        AttrDefineOperation operation = new AttrDefineOperation();

        [HttpGet]
        [Route("GetAll")]
        public List<AttrDefine> Get()
        {
            return operation.Query();
        }


        [HttpGet]
        [Route("GetOne")]
        public AttrDefine GetOne(string tmpId, string attrId, string attrNm, string attrTp)
        {
            return operation.QueryOne( tmpId,  attrId,  attrNm,  attrTp);
        }

        // POST: api/AttrDefineOperation
        [HttpPost]
        [Route("Create")]
        public void Post(AttrDefine attrDefine)
        {
            if (ModelState.IsValid)
            {
                operation.Insert(attrDefine);
                //ProcessResult processResult = new ProcessResult() {ResultCode="000000",ResultMessage="插入成功" };
                //HttpResponse
            }
            
        }

        // PUT: api/AttrDefineOperation/5
        [HttpPut]
        [Route("Update")]
        public void Put(string oldTmpId, string oldAttrId, string oldAttrNm, string oldAttrTp, AttrDefine attrDefine)
        {
            operation.Update(oldTmpId,  oldAttrId, oldAttrNm, oldAttrTp, attrDefine.TmpId,attrDefine.AttrId,attrDefine.AttrNm,attrDefine.AttrTp,attrDefine.LstUpdter);
        }

        // DELETE: api/AttrDefineOperation/5
        [HttpDelete]
        [Route("Delete")]
        public void Delete(string tmpId, string attrId, string attrNm, string attrTp)
        {
            operation.Delete(tmpId, attrId, attrNm, attrTp);
        }
    }
}
