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
            // 测试数据库数据返回为空的问题
            int i = -1;
            SqlConnection sqlConnection = DBConnection.OpenConnection();
            try
            {
                using (SqlCommand cmd = new SqlCommand("select isnull(max(rlseqno),0) from relation where tmpid='kk'", sqlConnection))
                {
                    i = (int)(cmd.ExecuteScalar());

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                Console.WriteLine("error");
            }
            finally
            {
                DBConnection.CloseConnection(sqlConnection);
            }
            Console.WriteLine(i);
        }
    }
}
