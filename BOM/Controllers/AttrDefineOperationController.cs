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
        public List<AttrDefine> Get()
        {
            return operation.Query();
        }

        // GET: api/AttrDefineOperation/5
        public List<AttrDefine> Get(string tmpId, string attrId, string attrNm, string attrTp)
        {
            return operation.Query( tmpId,  attrId,  attrNm,  attrTp);
        }

        // POST: api/AttrDefineOperation
        public void Post([FromBody]string value)
        {
        }

        // PUT: api/AttrDefineOperation/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/AttrDefineOperation/5
        public void Delete(int id)
        {
        }
    }
}
