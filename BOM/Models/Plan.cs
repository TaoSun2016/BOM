using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;
using System.Data.Common;
using BOM.DbAccess;
using System.Configuration;

namespace BOM.Models
{
    public class Plan : IDisposable
    {
        log4net.ILog log = log4net.LogManager.GetLogger("Plan");
        private DbConnection connection = null;
        private DbCommand cmd = new SqlCommand();
        private DbTransaction transaction = null;

        private int option;

        string dbType = ConfigurationManager.AppSettings["DbType"];

        //tableFlag=0时，续排登记缺件表1，PAB登记daefault._store_1,否则登记缺件表，PAB登记daefault._store_
        private int tableFlag = 0;
        string sql = null;
        List<Stock> stocks = new List<Stock>();
        List<Bom> bomTree = new List<Bom>();
        public Plan(int option, DbConnection conn, DbCommand sqlCommand, DbTransaction sqlTransaction)
        {
            this.option = option;
            connection = conn;
            cmd = sqlCommand;
            transaction = sqlTransaction;
        }

        /// <summary>
        /// 排产
        /// </summary>
        public bool CreatePlan(List<PlanItem> requestItems)
        {

            foreach (PlanItem item in requestItems)
            {
                log.Error("list item - "+item.wuLBM.ToString());
            }

            int i = 0;
            List<PlanItem> items = requestItems.OrderBy(m => m.qiH).ThenBy(m => m.xuH).ThenBy(m => m.gongZLH).ToList();

            //初始化库存数据：将缺省属性读取到stacks
            if (!InitData())
            {
                log.Error(string.Format($"Init Data Failed!\n"));
                return false;
            }

            //遍历生产计划记录
            foreach (PlanItem item in items)
            {
                i++;

                log.Error("iterate 0");
                
                if (option == 2)//续排
                {
                    if (!GetTableFlag(item))
                    {
                        log.Error(string.Format($"获取续排次数失败\n"));
                        return false;
                    }
                }
                //初始化当前物料的BOM树
                if (!InitBomTree(item))
                {
                    log.Error(string.Format($"初始化BOM树失败，BOMID={item.wuLBM}\n"));
                    return false;
                }
                if (!CheckData(i, item))
                {
                    log.Error(string.Format($"数据检查失败，BOMID={item.wuLBM}\n"));
                    return false;
                }
                log.Error("finish init bom tree");

                //遍历BOM树，将各物料所需的实际数量登记到stocks.shuL中
                Calculate(i, item);

                log.Error("finish calculating");

                //处理当前记录，令号，期号，顺序号
                if (!ProcessOrder(i, item))
                {
                    log.Error(string.Format($"Process Order Failed!\n"));
                    return false;
                }
                log.Error("fiish order");
                //重排，续排才处理
                if (option == 1 || option == 2)
                {
                    if (!HandlePlanFlag(item))
                    {
                        log.Error(string.Format($"处理续排标志失败!\n"));
                        return false;
                    }
                }

            }

            //更新上期库存量，重排，续排才处理
            if (option == 1 || option == 2)
            {
                if (!UpdatePAB())
                {
                    log.Error(string.Format($"Update PAB Failed!\n"));
                    return false;
                }
            }
            return true;
        }
        private bool CheckData(int i, PlanItem item)
        {
            return true;
        }



        private bool InitData()
        {
            DbDataReader dataReader = null;
            stocks.Clear();
            string tmp = "";

            //获取DeafaultAttr表中当前所有物料的参数
            sql = $"select * from DeafaultAttr";
            cmd.CommandText = sql;
            try
            {
                dataReader = cmd.ExecuteReader();
                if (dataReader.HasRows)
                {
                    while (dataReader.Read())
                    {
                        var item = new Stock();
                        item.flag = 0;

                        //物料编码
                        tmp = dataReader["materielIdentfication"].ToString();
                        item.wuLBM = Convert.ToInt64(tmp);

                        //在途数量
                        tmp = dataReader["SR"].ToString();
                        item.SOR = Convert.ToDecimal(tmp == string.Empty ? "0.00" : tmp);

                        //上期库存数量
                        if (option == 2 || option == 4)//续排，预续排
                        {
                            if (tableFlag == 0)//本次从_STORE_获取值，计算后的新值登记到_STORE_1中
                            {
                                tmp = dataReader["_STORE_"].ToString();
                                item.PAB = Convert.ToDecimal(tmp == string.Empty ? "0.00" : tmp);
                            }
                            else
                            {//本次从_STORE_1获取值，计算后的新值登记到_STORE_中
                                tmp = dataReader["_STORE_1"].ToString();
                                item.PAB = Convert.ToDecimal(tmp == string.Empty ? "0.00" : tmp);
                            }
                        }

                        //安全库存
                        tmp = dataReader["SS"].ToString();
                        item.SS = Convert.ToDecimal(tmp == string.Empty ? "0.00" : tmp);

                        //批量规则
                        tmp = dataReader["LS"].ToString();
                        item.LS = Convert.ToDecimal(tmp == string.Empty ? "0.00" : tmp);

                        //生产形式
                        item.shengChXSh = dataReader["SHENGCLX"].ToString();

                        //投料标识
                        tmp = dataReader["touLBSh"].ToString();
                        item.touLBSh = Convert.ToInt64(tmp == string.Empty ? "0" : tmp);

                        //工序号
                        item.zum = dataReader["zum"].ToString();

                        //图号
                        item.tuhao = dataReader["tuhao"].ToString();

                        //规格
                        item.guige = dataReader["guige"].ToString();

                        //在库数量OH初始化
                        item.OH = 0.00m;

                        stocks.Add(item);
                    }
                }
                else
                {
                    log.Error(string.Format($"表DeafaultAttr中没有记录r!\nsql[{sql}]\nError"));
                    return false;
                }
            }
            catch (Exception e)
            {
                log.Error(string.Format($"Select DeafaultAttr error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                return false;
            }
            finally
            {
                dataReader.Close();
            }

            //获取每个物料的库存数量
            foreach (var item in stocks)
            {
                sql = ("MySQL" == dbType)? $"select ifnull(sum(kuCSh),0.0000) from kuCShJB001 where wuLBM = {item.wuLBM}" : $"select isnull(sum(kuCSh),0.0000) from kuCShJB001 where wuLBM = {item.wuLBM}";
                cmd.CommandText = sql;
                cmd.Connection = connection;
                try
                {
                    tmp = cmd.ExecuteScalar().ToString();
                    item.OH = Convert.ToDecimal(tmp == string.Empty ? "0.00" : tmp);
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"获取在库数量OH Error!\nsql[{sql}]\nError[{e.StackTrace}]\nMessage[{e.Message}]"));
                    return false;
                }

                if (option == 1 || option == 3)//重排，预重排
                {
                    //重排：上期库存=在途+在库
                    item.PAB = item.SOR + item.OH;
                }
            }

            return true;
        }

        //给全局变量tableFlag赋值：
        //tableFlag=0时，续排登记缺件表1，PAB登记daefault._store_1,否则登记缺件表，PAB登记daefault._store_
        private bool GetTableFlag(PlanItem item)
        {
            int Count = 0;

            sql = $"select count(*) from switch where gongZLH='{item.gongZLH}' and qiH='{item.qiH}' and xuH={item.xuH}";
            cmd.CommandText = sql;
            try
            {
                Count = Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (Exception e)
            {
                log.Error(string.Format($"select switch error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                return false;
            }

            if (Count == 0)
            {
                tableFlag = 0;
                sql = $"insert into switch (gongZLH, qiH, xuH, ciSh) values ('{item.gongZLH}','{item.qiH}' , {item.xuH}, 0)";
                cmd.CommandText = sql;
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"insert table switch error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    return false;
                }
                return true;
            }

            sql = $"select ciSh from switch where gongZLH='{item.gongZLH}' and qiH='{item.qiH}' and xuH={item.xuH}";
            cmd.CommandText = sql;
            try
            {
                Count = Convert.ToInt32(cmd.ExecuteScalar());
                //tableFlag=0时，续排登记缺件表1，PAB登记daefault._store_1,否则登记缺件表，PAB登记daefault._store_
                tableFlag = Count % 2;
            }
            catch (Exception e)
            {
                log.Error(string.Format($"select switch error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                return false;
            }
            return true;
        }

        //初始化BOM树列表bomTree
        private bool InitBomTree(PlanItem item)
        {
            bomTree.Clear();
            return AddBomItem(item.wuLBM, item.qiH, item.gongZLH);
        }

        //将BOM表中的一个BOM树递归登记到列表bomTree中
        private bool AddBomItem(long wuLBM, string qiH, string gongZLH)
        {
            long tmpWuLBM = 0L;
            Bom bom = new Bom();
            DbDataReader reader = null;


            //查找代用件
            sql = $"select daiWLBM from daiYJJHZhB z, daiYJJHCB c where c.zhuBBSh = z.biaoSh and c.yuanWLBM = z.wuLBM and c.yuanWLTH = z.tuH and c.yuanWLGG=z.GuiG and z.qiH='{qiH}' and z.gongZLH = '{gongZLH}' and z.wuLBM={wuLBM}";
            cmd.CommandText = sql;
            try
            {
                reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    reader.Read();
                    tmpWuLBM = Convert.ToInt64(reader["daiWLBM"].ToString());
                }
                else
                {
                    tmpWuLBM = wuLBM;
                }

            }
            catch (Exception e)
            {
                log.Error(string.Format($"查询代用件计划表失败，SQL=[{sql}]\nError{e.StackTrace}"));
                return false;
            }
            finally
            {
                reader?.Close();
            }

            //查找当前物料信息 Cmid = tmpWuLBM
            sql = $"select * from BOM where CmId = {tmpWuLBM}";
            cmd.CommandText = sql;
            try
            {
                reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {

                    reader.Read();
                    bom.materielIdentfication = Convert.ToInt64(reader["materielIdentfication"].ToString());
                    bom.tmpId = Convert.ToInt64(reader["tmpId"].ToString());
                    bom.CTmpId = Convert.ToInt64(reader["CTmpId"].ToString());
                    bom.CmId = Convert.ToInt64(reader["CmId"].ToString());
                    bom.CNum = Convert.ToDecimal(reader["CNum"].ToString());
                    bom.rlSeqNo = Convert.ToInt32(reader["rlSeqNo"].ToString());
                    bom.peiTNo = Convert.ToInt64(reader["peiTNo"].ToString());
                }
                else
                {
                    log.Error(string.Format($"未在BOM表中查到相关记录，SQL=[{sql}]\n"));
                    return false;
                }
            }
            catch (Exception e)
            {
                log.Error(string.Format($"查询BOM失败，SQL=[{sql}]\nError{e.StackTrace}"));
                return false;
            }
            finally
            {
                reader.Close();
            }

            //登记物料树
            bomTree.Add(bom);

            //查找当前物料的子物料
            List<long> mid_list = new List<long>();
            sql = $"select Cmid from BOM where materielIdentfication = {wuLBM} ";
            cmd.CommandText = sql;
            try
            {
                reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {

                    while (reader.Read())
                    {
                        mid_list.Add(Convert.ToInt64(reader["CmId"].ToString()));
                    }
                    reader.Close();

                    foreach (var mid in mid_list)
                    {
                        if (!AddBomItem(mid, qiH, gongZLH))
                        {
                            return false;
                        }
                    }

                }
                else
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                log.Error(string.Format($"查询BOM失败，SQL=[{sql}]\nError{e.StackTrace}"));
                return false;
            }
            finally
            {
                reader?.Close();
            }

            return true;

        }
        private bool HandlePlanFlag(PlanItem item)
        {
            if (option == 1 || option == 3)//重排，预重排
            {
                sql = $"delete from switch where gongZLH='{item.gongZLH}' and qiH='{item.qiH}' and xuH={item.xuH}; " +
                      $"insert into switch (gongZLH, qiH, xuH, ciSh) values ('{item.gongZLH}', '{item.qiH}', {item.xuH}, 0);";

            }
            else
            {
                sql = $"update switch set ciSh = ciSh + 1 where gongZLH='{item.gongZLH}' and qiH='{item.qiH}' and xuH={item.xuH};";
            }
            cmd.CommandText = sql;
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch
            {
                log.Error(string.Format($"update switch Error!\nsql[{sql}]\n"));
                return false;
            }
            return true;
        }
        private bool UpdatePAB()
        {
            int iterator = 0;
            StringBuilder sb = new StringBuilder();
            sb.Clear();

            foreach (Stock item in stocks)
            {
                //成品不更新PAB
                if (item.shengChXSh == "4")
                {
                    continue;
                }

                //是续排且本次续排没有使用该物料（item.flag == 0），则无需更新PAB
                if ((option == 2 || option == 4) && item.flag == 0)
                {
                    continue;
                }

                iterator++;

                var fieldName = "_STORE_";
                if ((option == 2 || option == 4) && tableFlag == 0)//续排且tableFlag为0，则登记—_STORE_1字段
                {
                    fieldName = "_STORE_1";
                }

                sb.Append(("MySQL"==dbType)? $"UPDATE DeafaultAttr SET `{fieldName}` = {item.PAB} WHERE materielIdentfication = {item.wuLBM}; "
                                            :$"UPDATE DeafaultAttr SET [{fieldName}] = {item.PAB} WHERE materielIdentfication = {item.wuLBM}; ");

                if (iterator % 1000 == 0)
                {
                    sql = sb.ToString();
                    cmd.CommandText = sql;
                    try
                    {
                        var result = cmd.ExecuteNonQuery();
                        if (result != 1)
                        {
                            log.Error(string.Format($"Update DeafaultAttr Error!\nsql[{sql}]\n"));
                            return false;
                        }
                    }
                    catch (Exception e)
                    {
                        log.Error(string.Format($"Update DeafaultAttr Error\nsql[{sql}]\nError[{e.StackTrace}]"));
                        return false;
                    }
                    finally
                    {
                        sb.Clear();
                    }
                }
            }
            if (sb.Length != 0)
            {
                sql = sb.ToString();
                cmd.CommandText = sql;
                try
                {
                    var result = cmd.ExecuteNonQuery();
                    if (result == 0)
                    {
                        log.Error(string.Format($"Update DeafaultAttr Error!\nsql=[{sql}]\n"));
                        return false;
                    }
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Update DeafaultAttr Error\nsql=[{sql}]\nError[{e.StackTrace}]"));
                    return false;
                }
                finally
                {
                    sb.Clear();
                }
            }

            return true;
        }
        /// <summary>
        /// 计算整个物料树各物料节点的实际需求数量，登记到stocks.shuL
        /// </summary>
        private void Calculate(int seqno, PlanItem item)
        {
            decimal multiple = 0.00m;       //当前物料不足的数量，也是其子物料需要乘以的倍数
            decimal surplus = 0.00m;        //当前剩余物料的数量
            decimal shuL = item.shuL;


            var stock = stocks.Find(m => m.wuLBM == item.wuLBM);
            stock.flag = seqno;
            surplus = stock.PAB - stock.shuL;//当前剩余的数量
            stock.shuL += shuL;//累计实际需要的数量

            //当前剩余大于本次所需，不需进一步处理
            if (surplus - shuL > -0.00005m)
            {
                return;
            }

            //当前没有剩余
            if (surplus < 0.00005m)
            {
                multiple = shuL;
            }//当前有剩余，但不足本次所需
            else if (surplus - shuL < 0.00005m)
            {
                multiple = shuL - surplus;
            }

            //从BOM树种找到子物料，再逐个遍历，累计更新stacks相应物料的需求数量shuL
            foreach (Bom bomnode in bomTree.Where(m => m.materielIdentfication == item.wuLBM))
            {
                PlanItem planItem = new PlanItem
                {
                    wuLBM = bomnode.CmId,               //物料编码
                    shuL = bomnode.CNum * multiple,     //数量
                    gongZLH = item.gongZLH,             //工作令号
                    qiH = item.qiH,                     //期号
                    xuH = item.xuH,                      //序号
                    jiaoHQ = item.jiaoHQ                //交货期
                };

                Calculate(seqno, planItem);
            }
        }

        private bool ProcessOrder(int i, PlanItem item)
        {
            decimal POH = 0.00m;    //预计在库量
            decimal NR = 0.00m;    //净需求数量
            decimal PORC = 0.00m;    //缺件数量

            foreach (Stock stock in stocks.Where(m => m.flag == i))
            {
                log.Error("process order 0");
                //预计在库量 POH
                POH = stock.PAB - stock.shuL;

                //计算净需求数量NR
                if (POH - stock.SS > -0.00005m)
                {
                    NR = 0.0000m;
                }
                else
                {
                    NR = stock.SS - POH;
                }
                log.Error("NR ="+ NR + "LS = "+stock.LS);
                //计算计划订单收料量PORC,即为缺件数量
                PORC = 0.0000m;
                if (NR > 0.00005m)
                {
                    do
                    {
                        PORC += stock.LS;
                    } while (PORC - NR < -0.00005m);
                }
                log.Error("PORC =" +PORC);
                //计算本期库存数量PAB = 计划订单收料量PORC + 预计在库量POH
                stock.PAB = PORC + POH;
                log.Error("process order 1");
                //所有的生产形式都要登记缺件表
                var tableName = "";
                switch (option)
                {
                    case 1://重排
                        tableName = "ullageTempSingle";
                        break;
                    case 2://续排
                        tableName = (tableFlag == 0) ? "ullageTempSingle2" : "ullageTempSingle";
                        break;
                    case 3://预重排
                        tableName = "ullageTempSingleTemp";
                        break;
                    case 4://预续排 
                        tableName = "ullageTempSingle2Temp";
                        break;
                    default:
                        break;
                }


                sql = $"delete from {tableName} where gongZLH='{item.gongZLH}' and qiH='{item.qiH}' and wuLBM={stock.wuLBM}; " +
                      $"insert into {tableName} (gongZLH, qiH, xuH, wuLBM, gongZLBSh, mingCh, guiG, tuH, shengChXSh, gongShSh, queJShL, jiHBB, chunJHQJ ) values ('{item.gongZLH}', '{item.qiH}', 0, {stock.wuLBM},'','','','',{stock.shengChXSh},0.00,{PORC},'',0.00);";


                cmd.CommandText = sql;
                try
                {
                    var result = cmd.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"update ullageTempSingle Error\nsql[{sql}]\nError[{e.StackTrace}]"));
                    return false;
                }
                log.Error("process order 2");
                //预排不再进行后续处理
                if (option == 3 || option == 4)
                {
                    return true;
                }

                switch (stock.shengChXSh)
                {
                    case "0"://原材料
                             // update caiGShJB001 采购数据表
                        sql = $"select count(wuLBM) from caiGShJB001 where wuLBM = {stock.wuLBM}";
                        cmd.CommandText = sql;
                        try
                        {
                            var result = Convert.ToInt32(cmd.ExecuteScalar());
                            if (result == 1)//找到记录，更新原记录
                            {
                                if (option == 1 || option == 3)//重排，预重排 覆盖原记录计划数量的值
                                {
                                    sql = $"update caiGShJB001 set jiHShL = {PORC}, wanChShL = 0.00,  jinE = 0.00, heJJE = 0.00 where wuLBM = {stock.wuLBM}";
                                }
                                else//续排 在原记录基础上累加 完成数量wanChShL和累计下达数量leiJXDShL如何赋值??
                                {
                                    sql = $"update caiGShJB001 set jiHShL = jiHShL + {PORC} where wuLBM = {stock.wuLBM}";
                                }

                            }
                            else//找不到则登记新记录,计划数量= 缺件数量，其它为0
                            {
                                sql = $"insert into caiGShJB001 (wuLBM,jiHShL, wanChShL,leiJXDShL,jinE,heJJE,caiGY) values ({stock.wuLBM},{PORC},0.00,0.00,0.00,0.00,'')";
                            }
                            cmd.CommandText = sql;
                            result = cmd.ExecuteNonQuery();
                            if (result != 1)
                            {
                                log.Error(string.Format($"update/insert caiGShJB001 Error\nsql[{sql}]\n"));
                                return false;
                            }
                        }
                        catch (Exception e)
                        {
                            log.Error(string.Format($"Select caiGShJB001 Error\nsql[{sql}]\nError[{e.StackTrace}]"));
                            return false;
                        }
                        break;
                    case "1"://自制件
                             //工序转移准备单主表 gongXZhYZhBDZhB
                        sql = $"select count(wuLBM) from gongXZhYZhBDZhB where gongZLH = '{item.gongZLH}' and qiH = '{item.qiH}' and wuLBM = {stock.wuLBM}";
                        cmd.CommandText = sql;
                        try
                        {
                            var result = Convert.ToInt32(cmd.ExecuteScalar());
                            sql = "";  
                            if (result == 1)//找到记录，更新原记录   ???不等于1如何处理
                            {
                                if (option == 1 || option == 3)//重排，预重排 覆盖原记录计划数量的值
                                {
                                    //根据令号，期号和物料编码删除原记录
                                    sql = $"delete from gongXZhYZhBDZhB where gongZLH = '{item.gongZLH}' and qiH = '{item.qiH}' and wuLBM = {stock.wuLBM}; ";
                                }
                                else//续排 报错
                                {
                                    log.Error(string.Format($"续排，物料为自制件，工序转移准备单主表已有记录\nsql[{sql}]\n"));
                                    return false;
                                }
                            }

                            //产品名称：物料的模板编码
                            var chanPMCh = "";
                            var pos = stock.wuLBM.ToString().IndexOf("9");
                            if (pos >= 0)
                            {
                                chanPMCh = stock.wuLBM.ToString().Substring(0, pos);
                            }

                            sql = sql + $"insert into gongXZhYZhBDZhB (wuLBM,chanPMCh,gongXBSh) values ({stock.wuLBM},'{chanPMCh}',{stock.zum});";

                            cmd.CommandText = sql;
                            result = cmd.ExecuteNonQuery();
                            if (result != 1)
                            {
                                log.Error(string.Format($"update/insert gongXZhYZhBDZhB Error\nsql[{sql}]\n"));
                                return false;
                            }
                        }
                        catch (Exception e)
                        {
                            log.Error(string.Format($"process gongXZhYZhBDZhB Error\nsql[{sql}]\nError[{e.StackTrace}]"));
                            return false;
                        }

                        //工序转移准备单从表 gongXZhYZhBDCB
                        sql = $"select count(wuLBM) from gongXZhYZhBDCB where gongZLH = '{item.gongZLH}' and qiH = '{item.qiH}' and xuH = {item.xuH} and wuLBM = {stock.wuLBM}";
                        cmd.CommandText = sql;
                        try
                        {
                            var result = Convert.ToInt32(cmd.ExecuteScalar());
                            sql = "";
                            if (result == 1)//找到记录，更新原记录
                            {
                                if (option == 1 || option == 3)//重排，预重排 覆盖原记录计划数量的值
                                {
                                    //
                                    sql = $"delete from gongXZhYZhBDCB where gongZLH = '{item.gongZLH}' and qiH = '{item.qiH}' and xuH = {item.xuH} and wuLBM = {stock.wuLBM}; ";
                                }
                                else//续排 报错
                                {
                                    log.Error(string.Format($"续排，物料为自制件，工序转移准备单从表已有记录\nsql[{sql}]\n"));
                                    return false;
                                }
                            }

                            sql = sql + $"insert into gongXZhYZhBDCB (gongZLH, qiH, xuH, wuLBM, jiHShL,tuH, guiG ) values ('{item.gongZLH}','{item.qiH}',{item.xuH},{stock.wuLBM},{PORC},'{stock.tuhao}','{stock.guige}');";

                            cmd.CommandText = sql;
                            result = cmd.ExecuteNonQuery();
                            if (result != 1)
                            {
                                log.Error(string.Format($"update/insert gongXZhYZhBDCB Error\nsql[{sql}]\n"));
                                return false;
                            }
                        }
                        catch (Exception e)
                        {
                            log.Error(string.Format($"process gongXZhYZhBDCB Error\nsql[{sql}]\nError[{e.StackTrace}]"));
                            return false;
                        }

                        // 更新投料采购下达  touLCGXD
                        sql = $"select count(wuLBM) from touLCGXD where gongZLH = '{item.gongZLH}' and qiH = '{item.qiH}' and wuLBM = {stock.wuLBM}";
                        cmd.CommandText = sql;
                        try
                        {
                            var result = Convert.ToInt32(cmd.ExecuteScalar());
                            if (result == 1)//找到记录，更新原记录
                            {
                                if (option == 1 || option == 3)//重排，预重排 覆盖原记录计划数量的值
                                {
                                    sql = $"update touLCGXD set queJShL = {PORC}, chunJHQJ = 0.00, leiJXDShL = 0.00 where gongZLH = '{item.gongZLH}' and qiH = '{item.qiH}' and wuLBM = {stock.wuLBM}";
                                }
                                else//续排 在原记录基础上累加??
                                {
                                    sql = $"update touLCGXD set queJShL = queJShL + {PORC} where gongZLH = '{item.gongZLH}' and qiH = '{item.qiH}' and wuLBM = {stock.wuLBM}";
                                }

                            }
                            else//找不到则登记新记录:缺件数量=PORC
                            {
                                sql = $"insert into touLCGXD (wuLBM,queJShL, chunJHQJ,qiH,gongZLH,shengChXSh,touLBSh) values ({stock.wuLBM},{PORC},0.00,'{item.qiH}','{item.gongZLH}','{stock.shengChXSh}',{stock.touLBSh})";
                            }
                            cmd.CommandText = sql;
                            result = cmd.ExecuteNonQuery();
                            if (result != 1)
                            {
                                log.Error(string.Format($"update/insert touLCGXD Error\nsql[{sql}]\n"));
                                return false;
                            }
                        }
                        catch (Exception e)
                        {
                            log.Error(string.Format($"process touLCGXD Error\nsql[{sql}]\nError[{e.StackTrace}]"));
                            return false;
                        }
                        break;

                    case "2"://外协
                             //外协出库准备单主表waiXChKZhBDZB
                        sql = $"select count(wuLBM) from waiXChKZhBDZB where gongZLH = '{item.gongZLH}' and qiH = '{item.qiH}' and xuH = {item.xuH} and wuLBM = {stock.wuLBM}";
                        cmd.CommandText = sql;
                        try
                        {
                            var result = Convert.ToInt32(cmd.ExecuteScalar());
                            sql = "";
                            if (result == 1)//找到记录，更新原记录
                            {
                                if (option == 1 || option == 3)//重排，预重排 覆盖原记录计划数量的值
                                {
                                    //
                                    sql = $"delete from waiXChKZhBDZB where gongZLH = '{item.gongZLH}' and qiH = '{item.qiH}' and xuH = {item.xuH} and wuLBM = {stock.wuLBM}; ";
                                }
                                else//续排 报错
                                {
                                    log.Error(string.Format($"续排，物料为外协件，外协出库准备单主表已有记录r\nsql[{sql}]\n"));
                                    return false;
                                }

                            }

                            sql = sql + $"insert into waiXChKZhBDZB (gongZLH, qiH, xuH, wuLBM, tuH, guiG ) values ('{item.gongZLH}','{item.qiH}',{item.xuH},{stock.wuLBM},'{stock.tuhao}','{stock.guige}');";

                            cmd.CommandText = sql;
                            result = cmd.ExecuteNonQuery();
                            if (result != 1)
                            {
                                log.Error(string.Format($"update/insert waiXChKZhBDZB Error\nsql[{sql}]\n"));
                                return false;
                            }
                        }
                        catch (Exception e)
                        {
                            log.Error(string.Format($"process waiXChKZhBDZB Error\nsql[{sql}]\nError[{e.StackTrace}]"));
                            return false;
                        }

                        //外协出库准备单从表 waiXChKZhBDCB
                        sql = $"select count(wuLBM) from waiXChKZhBDCB where gongZLH = '{item.gongZLH}' and qiH = '{item.qiH}' and xuH = {item.xuH} and wuLBM = {stock.wuLBM}";
                        cmd.CommandText = sql;
                        try
                        {
                            var result = Convert.ToInt32(cmd.ExecuteScalar());
                            sql = "";
                            if (result == 1)//找到记录，更新原记录
                            {
                                if (option == 1 || option == 3)//重排，预重排 覆盖原记录计划数量的值
                                {
                                    //
                                    sql = $"delete from waiXChKZhBDCB where gongZLH = '{item.gongZLH}' and qiH = '{item.qiH}' and xuH = {item.xuH} and wuLBM = {stock.wuLBM}; ";
                                }
                                else//续排 报错
                                {
                                    log.Error(string.Format($"续排，物料为外协件，外协出库准备单从表已有记录r\nsql[{sql}]\n"));
                                    return false;
                                }

                            }

                            sql = sql + $"insert into waiXChKZhBDCB (gongZLH, qiH, xuH, wuLBM, tuH, guiG, jiHShL ) values ('{item.gongZLH}','{item.qiH}',{item.xuH},{stock.wuLBM},'{stock.tuhao}','{stock.guige}',{PORC})";

                            cmd.CommandText = sql;
                            result = cmd.ExecuteNonQuery();
                            if (result != 1)
                            {
                                log.Error(string.Format($"update/insert waiXChKZhBDCB Error\nsql[{sql}]\n"));
                                return false;
                            }
                        }
                        catch (Exception e)
                        {
                            log.Error(string.Format($"process waiXChKZhBDCB Error\nsql[{sql}]\nError[{e.StackTrace}]"));
                            return false;
                        }
                        break;
                    case "3"://半成品
                             //工序转移准备单主表 gongXZhYZhBDZhB
                        sql = $"select count(wuLBM) from gongXZhYZhBDZhB where gongZLH = '{item.gongZLH}' and qiH = '{item.qiH}' and wuLBM = {stock.wuLBM}";
                        cmd.CommandText = sql;
                        try
                        {
                            var result = Convert.ToInt32(cmd.ExecuteScalar());
                            sql = "";
                            if (result == 1)//找到记录，更新原记录
                            {
                                if (option == 1 || option == 3)//重排，预重排 覆盖原记录计划数量的值
                                {
                                    //?? 给哪些字段赋值
                                    sql = $"delete from gongXZhYZhBDZhB where gongZLH = '{item.gongZLH}' and qiH = '{item.qiH}' and wuLBM = {stock.wuLBM}; ";
                                }
                                else//续排 报错??应该是查到续排前有重复的领号，期号，序号的记录就报错
                                {
                                    log.Error(string.Format($"续排，物料为自制件，工序转移准备单主表已有记录r\nsql[{sql}]\n"));
                                    return false;
                                }

                            }

                            //产品名称：物料的模板编码
                            var chanPMCh = "";
                            var pos = stock.wuLBM.ToString().IndexOf("9");
                            if (pos >= 0)
                            {
                                chanPMCh = stock.wuLBM.ToString().Substring(0, pos);
                            }

                            sql = sql + $"insert into gongXZhYZhBDZhB (wuLBM,chanPMCh,gongXBSh) values ({stock.wuLBM},'{chanPMCh}',{stock.zum});";

                            cmd.CommandText = sql;
                            result = cmd.ExecuteNonQuery();
                            if (result != 1)
                            {
                                log.Error(string.Format($"update/insert gongXZhYZhBDZhB Error\nsql[{sql}]\n"));
                                return false;
                            }
                        }
                        catch (Exception e)
                        {
                            log.Error(string.Format($"process gongXZhYZhBDZhB Error\nsql[{sql}]\nError[{e.StackTrace}]"));
                            return false;
                        }

                        //工序转移准备单从表 gongXZhYZhBDCB
                        sql = $"select count(wuLBM) from gongXZhYZhBDCB where gongZLH = '{item.gongZLH}' and qiH = '{item.qiH}' and xuH = {item.xuH} and wuLBM = {stock.wuLBM}";
                        cmd.CommandText = sql;
                        try
                        {
                            var result = Convert.ToInt32(cmd.ExecuteScalar());
                            sql = "";
                            if (result == 1)//找到记录，更新原记录
                            {
                                if (option == 1 || option == 3)//重排，预重排 覆盖原记录计划数量的值
                                {
                                    //
                                    sql = $"delete from gongXZhYZhBDCB where gongZLH = '{item.gongZLH}' and qiH = '{item.qiH}' and xuH = {item.xuH} and wuLBM = {stock.wuLBM}; ";
                                }
                                else//续排 报错
                                {
                                    log.Error(string.Format($"续排，物料为自制件，工序转移准备单从表已有记录\nsql[{sql}]\n"));
                                    return false;
                                }

                            }

                            sql = sql + $"insert into gongXZhYZhBDCB (gongZLH, qiH, xuH, wuLBM, jiHShL,tuH, guiG ) values ('{item.gongZLH}','{item.qiH}',{item.xuH},{stock.wuLBM},{PORC},'{stock.tuhao}','{stock.guige}')";

                            cmd.CommandText = sql;
                            result = cmd.ExecuteNonQuery();
                            if (result != 1)
                            {
                                log.Error(string.Format($"update/insert gongXZhYZhBDCB Error\nsql[{sql}]\n"));
                                return false;
                            }
                        }
                        catch (Exception e)
                        {
                            log.Error(string.Format($"process gongXZhYZhBDCB Error\nsql[{sql}]\nError[{e.StackTrace}]"));
                            return false;
                        }
                        break;
                    case "4"://成品
                        break;
                    default:
                        break;
                }
                log.Error("process order 3");
            }
            log.Error("process order end");
            return true;
        }




        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                //if (disposing)
                //{
                //    // TODO: dispose managed state (managed objects).
                //}

                connection = null;

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~Plan()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }
        #endregion
    }




    public class PlanItem
    {
        public int option;       
        public long wuLBM;         //物料编码
        public decimal shuL;       //数量
        public string gongZLH;     //工作令号
        public string qiH;         //期号
        public int xuH;            //序号
        public DateTime jiaoHQ;    //交货期
    }

    public class Stock
    {
        public long wuLBM;         //物料编码

        public decimal SOR;        //在途数量

        public decimal OH;         //在库数量

        public decimal shuL;        //订单数量

        public decimal PAB;        //上期可用库存量

        public decimal SS { get; set; } //安全库存

        public decimal LS { get; set; } //批量规则

        public string shengChXSh { get; set; }    //生产形式  0原材料 1自制件 2外协 3半成品 4成品

        public long touLBSh { get; set; }   //投料标识

        public string zum { get; set; }     //工序号

        public string tuhao { get; set; }   //图号

        public string guige { get; set; }   //规格

        public int flag { get; set; }  // 0.本次请求没有用到该物料
    }

    public class Bom
    {
        public long materielIdentfication { get; set; }     //父物料编码
        public long tmpId { get; set; }         //父物料模板ID
        public long CmId { get; set; }          //物料编码
        public long CTmpId { get; set; }        //物料模板ID
        public decimal CNum { get; set; }
        public int rlSeqNo { get; set; }
        public long peiTNo { get; set; }         //配套标识
        public long substitute { get; set; }    //替代物料编码
    }
}