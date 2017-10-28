using System;
namespace BOM.Models
{
    public partial class AttrDefine
    {
        //I:Insert U:Update D:Delete
        public char Option { get; set; }
        public long OrigTmpId { get; set; }
        public string OrigAttrId { get; set; }
        public string OrigAttrNm { get; set; }
        public string OrigAttrTp { get; set; }
        public long TmpId { get; set; }
        public string AttrId { get; set; }
        public string AttrNm { get; set; }
        public string AttrTp { get; set; }
        public int LockFlag { get; set; }
        public string CrtDate { get; set; }
        public string Crter { get; set; }
        public string LstUpdtDate { get; set; }
        public string LstUpdter { get; set; }
    }
}
