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

            //c1 list = new c1();
            //list.c1s = new List<c1>();

            //list.id = 1;
            //list.c1s.Add(new c1 ());


            //测试databasereader
            //int i = -1;
            //SqlConnection sqlConnection = DBConnection.OpenConnection();
            //try
            //{
            //    using (SqlCommand cmd = new SqlCommand("select * from seq_no where ind_key='T2'", sqlConnection))
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


            //
            //BOMNode node = new BOMNode();
            //node.id = 1;

            //BOMNode node1 = new BOMNode();
            //node1.id = 2;
            //node.nodes.Add(node1);
            //node.nodes.Add(new BOMNode());



            //    Console.WriteLine(node.id  );
            //    Console.WriteLine(node.nodes[0].id);
            //Console.WriteLine(node.nodes[1].id);
            //foreach (var aa in node.nodes)
            //{
            //    aa.id += 1;
            //}
            ////node.nodes.Find(m => m.id == 0).id = 8;
            //Console.WriteLine(node.id);
            //Console.WriteLine(node.nodes[0].id);
            //Console.WriteLine(node.nodes[1].id);

            //test string split method
            //string stringTest = "aaa-a+b*c/d";
            //var result = stringTest.Split('+', '-', '*', '/');
            //foreach (var i in result)
            //{
            //    Console.WriteLine("[" + i + "]");

            //}


            //测试利用数据库计算表达式
            SqlConnection sqlConnection = DBConnection.OpenConnection();
            try
            {
                string sql = "(9*2+3*2)/7";
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = sqlConnection;
                    cmd.CommandText = $"select {sql} as result";
                    string i = cmd.ExecuteScalar().ToString();
                    Console.WriteLine( "["+i+"]");

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

        }
    }

    class c1
    {
        public int id { get; set; }
        public List<c1> c1s { get; set; }
    }

    class c2
    {
        public string  id { get; set; }
        public string  name { get; set; }
    }


    class BOMNode
    {
        public int id { get; set; }
        public List<BOMNode> nodes { get; set; }
        public BOMNode() {
            this.id = 0;
            this.nodes = new List<BOMNode>();
        }
    }
}
