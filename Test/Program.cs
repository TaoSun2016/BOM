using BOM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            AttrDefineOperation attrDefine = new AttrDefineOperation();
            //attrDefine.Insert("tmpid","attid","attname","C","tao");
            //attrDefine.Update("tmpid", "attid", "attname", "C", "tmpid1","tmpid2","tmpid3","T","sun");
            //attrDefine.Delete("tmpid", "attid", "attname", "C");
            /*
            string p1 = null;
            string p2 = null;
            string p3 = null;
            string p4 = null;

            foreach (var i in attrDefine.Query("T123",p1,p2,p3))
            {
                Console.WriteLine($"[{i.TmpId}][{i.AttrId}][{i.AttrNm}][{i.AttrTp}][{i.CrtDate}][{i.Crter}][{i.LockFlag}][{i.LstUpdtDate}][{i.LstUpdter}]");
            }
            Console.WriteLine("finished");
            */
            Console.WriteLine(Convert.ToString(9,8));
        }
    }
}
