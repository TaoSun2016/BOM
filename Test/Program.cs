using BOM.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            //AttrDefineOperation attrDefine = new AttrDefineOperation();
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
            //// 测试数据库数据返回为空的问题
            //int i = -1;
            //SqlConnection sqlConnection = DBConnection.OpenConnection();
            //try
            //{
            //    using (SqlCommand cmd = new SqlCommand("select isnull(max(rlseqno),0) from relation where tmpid='kk'", sqlConnection))
            //    {
            //        i = (int)(cmd.ExecuteScalar());

            //    }
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine(e.StackTrace);
            //    Console.WriteLine("error");
            //}
            //finally
            //{
            //    DBConnection.CloseConnection(sqlConnection);
            //}
            //Console.WriteLine(i);

            c1 list = new c1();
            list.c2s = new List<c2>();

            list.c2s.Add(new c2 { id="1",name="A"});

            Console.WriteLine(list.c2s[0].id);
        }
    }

    class c1
    {
        public int id { get; set; }
        public List<c2> c2s { get; set; }
    }

    class c2
    {
        public string  id { get; set; }
        public string  name { get; set; }
    }
}
