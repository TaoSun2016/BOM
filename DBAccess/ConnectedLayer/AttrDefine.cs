using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBAccess.ConnectedLayer
{
    public class AttrDefine
    {
        public string TmpId { get; set; }
        public string AttrId { get; set; }
        public string AttrNm { get; set; }
        public string AttrTp { get; set; }
        public int LockFlag { get; set; }
        public Nullable<System.DateTime> CrtDate { get; set; }
        public string Crter { get; set; }
        public Nullable<System.DateTime> LstUpdtDate { get; set; }
        public string LstUpdter { get; set; }
    }
}
