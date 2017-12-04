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
            log4net.Config.XmlConfigurator.Configure();
            log4net.ILog log = log4net.LogManager.GetLogger("TEST");
            log.Info("hello");
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
            //SqlConnection sqlConnection = DBConnection.OpenConnection();
            //try
            //{
            //    string sql = "(9*2+3*2)/7";
            //    using (SqlCommand cmd = new SqlCommand())
            //    {
            //        cmd.Connection = sqlConnection;
            //        cmd.CommandText = $"select {sql} as result";
            //        string i = cmd.ExecuteScalar().ToString();
            //        Console.WriteLine( "["+i+"]");

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

            /*测试根据父节点属性值计算子节点属性值
            SqlConnection sqlConnection = DBConnection.OpenConnection();
            SqlCommand command = new SqlCommand();

            SqlTransaction transaction = sqlConnection.BeginTransaction();
            List<NodeInfo> list = new List<NodeInfo>();
            command.Connection = sqlConnection;
            command.Transaction = transaction;

            BOMTree bom = new BOMTree(sqlConnection, command, transaction);
            NodeInfo p = new NodeInfo();
                           
            NodeInfo c = new NodeInfo();

            p.TmpId = "T538";
            p.rlSeqNo = 0;
            p.NodeLevel = 1;
            p.Attributes.Add(new TempletAttribute { Flag = "0", Id = "ABCcode",Name="ABC",Type="C", Values = { "0"} });
            p.Attributes.Add(new TempletAttribute { Flag = "0", Id = "GONGSS", Name = "工时", Type = "N", Values = { "0.00" } });
            p.Attributes.Add(new TempletAttribute { Flag = "0", Id = "guige", Name = "规格尺寸", Type = "C", Values = { "TU14-0.65-5.0" } });
            p.Attributes.Add(new TempletAttribute { Flag = "0", Id = "JILDW", Name = "计量单位", Type = "C", Values = { "台" } });
            p.Attributes.Add(new TempletAttribute { Flag = "0", Id = "KUW", Name = "库位", Type = "C", Values = { "CP" } });
            p.Attributes.Add(new TempletAttribute { Flag = "0", Id = "LS", Name = "批量规则", Type = "N", Values = { "0.00" } });
            p.Attributes.Add(new TempletAttribute { Flag = "0", Id = "SHENGCLX", Name = "生产类型", Type = "C", Values = { "4" } });
            p.Attributes.Add(new TempletAttribute { Flag = "0", Id = "SS", Name = "安全库存", Type = "N", Values = { "0.00" } });
            p.Attributes.Add(new TempletAttribute { Flag = "0", Id = "tuhao", Name = "图号", Type = "C", Values = { "WU" } });
            p.Attributes.Add(new TempletAttribute { Flag = "0", Id = "workgroup", Name = "生产组", Type = "C", Values = { "ZP" } });
            p.Attributes.Add(new TempletAttribute { Flag = "1", Id = "ANZFS", Name = "安装方式", Type = "C", Values = { "/" } });
            p.Attributes.Add(new TempletAttribute { Flag = "1", Id = "CHANPDH", Name = "产品代号", Type = "C", Values = { "TU" } });
            p.Attributes.Add(new TempletAttribute { Flag = "1", Id = "DIANJXH", Name = "电机型号", Type = "C", Values = { "WU" } });
            p.Attributes.Add(new TempletAttribute { Flag = "1", Id = "FUJJ", Name = "附加级", Type = "C", Values = { "VU8" } });
            p.Attributes.Add(new TempletAttribute { Flag = "1", Id = "JIZH", Name = "机座号", Type = "C", Values = { "8" } });
            p.Attributes.Add(new TempletAttribute { Flag = "1", Id = "N", Name = "转速", Type = "N", Values = { "5.234" } });
            p.Attributes.Add(new TempletAttribute { Flag = "1", Id = "P", Name = "额定功率", Type = "N", Values = { "0.75" } });
            p.Attributes.Add(new TempletAttribute { Flag = "1", Id = "TEB", Name = "特标", Type = "C", Values = { "/" } });
            p.Attributes.Add(new TempletAttribute { Flag = "1", Id = "XIANGTJG", Name = "箱体结构", Type = "C", Values = { "V" } });


            c.TmpId = "T267";
            c.PTmpId = "T538";
            c.NodeLevel = 2;
            c.rlSeqNo = 0;
            c.Attributes.Add(new TempletAttribute { Flag = "0", Id = "ABCcode", Name = "ABC", Type = "C", Values = { "" } });
            c.Attributes.Add(new TempletAttribute { Flag = "0", Id = "GONGSS", Name = "工时", Type = "N", Values = { "" } });
            c.Attributes.Add(new TempletAttribute { Flag = "0", Id = "guige", Name = "规格尺寸", Type = "C", Values = { "" } });
            c.Attributes.Add(new TempletAttribute { Flag = "0", Id = "JILDW", Name = "计量单位", Type = "C", Values = { "" } });
            c.Attributes.Add(new TempletAttribute { Flag = "0", Id = "KUW", Name = "库位", Type = "C", Values = { "" } });
            c.Attributes.Add(new TempletAttribute { Flag = "0", Id = "LS", Name = "批量规则", Type = "N", Values = { "" } });
            c.Attributes.Add(new TempletAttribute { Flag = "0", Id = "SHENGCLX", Name = "生产类型", Type = "C", Values = { "" } });
            c.Attributes.Add(new TempletAttribute { Flag = "0", Id = "SS", Name = "安全库存", Type = "N", Values = { "" } });
            c.Attributes.Add(new TempletAttribute { Flag = "0", Id = "tuhao", Name = "图号", Type = "C", Values = { "" } });
            c.Attributes.Add(new TempletAttribute { Flag = "0", Id = "workgroup", Name = "生产组", Type = "C", Values = { "" } });
            c.Attributes.Add(new TempletAttribute { Flag = "1", Id = "BIANMTH", Name = "图号编码", Type = "C", Values = { "" } });

            printnode(p);

            bom.GetChildAttributeValues(p,c);

            printnode(c);

            DBConnection.CloseConnection(sqlConnection);
            */

            Console.WriteLine(Convert.ToDecimal(".25"));

        }

        public static void printnode(NodeInfo node)
        {
            log4net.ILog log = log4net.LogManager.GetLogger("TEST");
            log.Info("=====================");
            log.Info("NodeLevel=" + node.NodeLevel);
            log.Info("ptmpid=" + node.PTmpId + "  pmaterielid=" + node.pMaterielId);
            log.Info("TmpId=" + node.TmpId + "  materelid=" + node.MaterielId);
            foreach (var attr in node.Attributes)
            {
                log.Info("---------------------------");

                log.Info(attr.Id + "----" + attr.Name + ":");
                foreach (var value in attr.Values)
                {
                    log.Info("=>" + value);
                }
            }
            log.Info("=====================");
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
