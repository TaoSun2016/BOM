using BOM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace BOM.Controllers
{
    public class AttrDefineOperationController : ApiController
    {
        AttrDefineOperation operation = new AttrDefineOperation();
        // GET: api/AttrDefineOperation
        [HttpGet]
        public List<AttrDefine> Get()
        {
            return operation.Query();
        }

        // GET: api/AttrDefineOperation/5
        [HttpGet]
        public AttrDefine GetOne(string tmpId, string attrId, string attrNm, string attrTp)
        {
            return operation.QueryOne( tmpId,  attrId,  attrNm,  attrTp);
        }

        // POST: api/AttrDefineOperation
        [HttpPost]
        public void Post(AttrDefine attrDefine)
        {
            if (ModelState.IsValid)
            {
                operation.Insert(attrDefine);
            }
            
        }

        // PUT: api/AttrDefineOperation/5
        [HttpPut]
        public void Put(string oldTmpId, string oldAttrId, string oldAttrNm, string oldAttrTp, AttrDefine attrDefine)
        {
            operation.Update(oldTmpId,  oldAttrId, oldAttrNm, oldAttrTp, attrDefine.TmpId,attrDefine.AttrId,attrDefine.AttrNm,attrDefine.AttrTp,attrDefine.LstUpdter);
        }

        // DELETE: api/AttrDefineOperation/5
        [HttpDelete]
        public void Delete(string tmpId, string attrId, string attrNm, string attrTp)
        {
            operation.Delete(tmpId, attrId, attrNm, attrTp);
        }
    }
}
