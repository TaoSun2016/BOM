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
        [HttpGet]
        [Route("Get")]
        public string Get(string tmpId)
        {
            SeqNo seqNo = new SeqNo();
            return seqNo.GetSeqNo(tmpId);
        }

    }
}
