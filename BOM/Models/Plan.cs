using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;

namespace BOM.Models
{
    public class Plan : IDisposable
    {
        log4net.ILog log = log4net.LogManager.GetLogger("Plan");
        private SqlConnection connection = null;
        private SqlCommand cmd = new SqlCommand();
        private SqlTransaction transaction = null;


        string sql = null;
        List<Stock> stocks = new List<Stock>();
        List<Bom> bomTree = new List<Bom>();
        public Plan(SqlConnection conn, SqlCommand sqlCommand, SqlTransaction sqlTransaction)
        {
            connection = conn;
            cmd = sqlCommand;
            transaction = sqlTransaction;
        }

        /// <summary>
        /// 排产
        /// </summary>
        public bool CreatePlan(int option, List<PlanItem> requestItems)
        {
            int i = 0;
            List<PlanItem> items = requestItems.OrderBy(m => m.qiH).ThenBy(m => m.xuH).ThenBy(m => m.gongZLH).ToList();

            //初始化库存数据
            if (!InitData(option))
            {
                log.Error(string.Format($"Init Data Failed!\n"));
                return false;
            }

            //遍历生产计划记录
            foreach (PlanItem item in items)
            {
                i++;
                //初始化当前物料的BOM树
                if (InitBomTree(item))
                {
                    log.Error(string.Format($"初始化BOM树失败，BOMID={item.wuLBM}\n"));
                    return false;
                }
                if (!CheckData(i, item))
                {
                    log.Error(string.Format($"数据检查失败，BOMID={item.wuLBM}\n"));
                    return false;
                }
                Calculate(i, item);

                //处理当前记录，令号，期号，顺序号
                if (!ProcessOrder(i, option, item))
                {
                    log.Error(string.Format($"Process Order Failed!\n"));
                    return false;
                }
            }

            //更新上期库存量
            if (!UpdatePAB(option))
            {
                log.Error(string.Format($"Update PAB Failed!\n"));
                return false;
            }
            return true;
        }
        private bool CheckData(int i, PlanItem item)
        {
            return true;
        }
        private bool InitData(int option)
        {
            SqlDataReader dataReader = null;
            stocks.Clear();

            //获取DeafaultAttr表中当前所有物料的参数
            using (SqlCommand cmd1 = new SqlCommand())
            {
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
                            item.wuLBM = Convert.ToInt64(dataReader["materielIdentfication"].ToString());   //物料编码
                            item.SOR = Convert.ToDecimal(dataReader["SR"].ToString());          //在途数量
                            item.PAB = Convert.ToDecimal(dataReader["_STORE_"].ToString());     //上期库存数量
                            item.SS = Convert.ToDecimal(dataReader["SS"].ToString());           //安全库存
                            item.LS = Convert.ToDecimal(dataReader["LS"].ToString());           //批量规则
                            item.shengChXSh = dataReader["SHENGCLX"].ToString();                //生产形式
                            item.touLBSh = Convert.ToInt64(dataReader["touLBSh"].ToString());   //投料标识
                            item.zum = dataReader["zum"].ToString();                            //工序号
                            item.tuhao = dataReader["tuhao"].ToString();                            //图号
                            item.guige = dataReader["guige"].ToString();                            //规格

                            item.OH = 0.00m;                                                //获取在库数量OH

                            sql = $"select sum(kuCSh) from kuCShJB001 where wuLBM = {item.wuLBM}";
                            cmd1.CommandText = sql;
                            cmd1.Connection = connection;
                            try
                            {
                                item.OH = Convert.ToDecimal(cmd.ExecuteScalar());
                            }
                            catch (Exception e)
                            {
                                log.Error(string.Format($"Get OH error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                                return false;
                            }

                            if (option == 1 || option == 3)//重排，预重排
                            {
                                item.PAB = item.SOR + item.OH;
                            }
                            stocks.Add(item);
                        }
                    }
                    else
                    {
                        log.Error(string.Format($"Select DeafaultAttr error!\nsql[{sql}]\nError"));
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

                return true;
            }
        }

        private bool UpdatePAB(int option)
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

                //是续排且本次续排没有使用该物料，则无需更新PAB
                if ((option == 2 || option == 4) && item.flag == 0)
                {
                    continue;
                }

                iterator++;

                sb.Append($"UPDATE DeafaultAttr SET _Store_ = {item.PAB} WHERE materielIdentfication = {item.wuLBM} ");

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

            return true;
        }
        /// <summary>
        /// 计算单个物料的缺件信息和相关表单数据
        /// </summary>
        private void Calculate(int seqno, PlanItem item)
        {
            decimal multiple = 0.00m;       //当前物料不足的数量
            decimal surplus = 0.00m;        //上期剩余减去当前物料需要的数量后，剩余的数量

            //考虑替代物料??
            var stock = stocks.Find(m => m.wuLBM == item.wuLBM);
            stock.flag = seqno;
            surplus = stock.PAB - stock.shuL;
            stock.shuL += item.shuL;

            if (surplus - item.shuL > -0.00005m)
            {
                return;
            }

            if (surplus < 0.00005m)
            {
                multiple = item.shuL;
            }
            else if (surplus - item.shuL < 0.00005m)
            {
                multiple = item.shuL - surplus;
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

        private bool ProcessOrder(int i, int option, PlanItem item)
        {
            decimal POH = 0.00m;    //预计在库量
            decimal NR = 0.00m;    //净需求数量
            decimal PORC = 0.00m;    //缺件数量

            foreach (Stock stock in stocks.Where(m => m.flag == i))
            {
                //预计在库量 POH
                POH = stock.PAB - stock.shuL;

                //计算净需求数量NR
                if (POH - stock.SS > 0.00005m)
                {
                    NR = 0.0000m;
                }
                else
                {
                    NR = stock.SS - POH;
                }

                //计算计划订单收料量PORC,即为缺件数量
                PORC = 0.0000m;
                if (NR > 0.00005m)
                {
                    do
                    {
                        PORC += stock.LS;
                    } while (PORC - NR < -0.00005m);
                }

                //计算本期库存数量PAB = 计划订单收料量PORC + 预计在库量POH
                stock.PAB = PORC + POH;

                //所有的生产形式都要登记缺件表
                if (option == 1 || option == 3)
                {
                    sql = $"delete from ullageTempSingle where gongZLH={item.gongZLH} and qiH={item.qiH} and wuLBM={stock.wuLBM} insert into ullageTempSingle (wuLBM, gongZLBSh, mingCh, guiG, tuH, shengChXSh, gongShSh, queJShL, qiH, jiHBB, chunJHQJ, gongZLH) values ({stock.wuLBM},'','','','',{stock.shengChXSh},0.00,{PORC},{item.qiH},'',0.00,{item.gongZLH})";
                }
                else//续排原来要求登记新的缺件表，旧表中的数据有什么用??没有直接覆盖就行，否则很难控制交替使用不同的表
                {
                    sql = $"delete from ullageTempSingle where gongZLH={item.gongZLH} and qiH={item.qiH} and wuLBM={stock.wuLBM} insert into ullageTempSingle (wuLBM, gongZLBSh, mingCh, guiG, tuH, shengChXSh, gongShSh, queJShL, qiH, jiHBB, chunJHQJ, gongZLH) values ({stock.wuLBM},'','','','',{stock.shengChXSh},0.00,{PORC},{item.qiH},'',0.00,{item.gongZLH})";
                }
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

                switch (stock.shengChXSh)
                {
                    case "0"://原材料
                             // update caiGShJB001 采购数据表
                        sql = $"select count(wuLBM) from caiGShJB001 where wuLBM = {stock.wuLBM}";
                        cmd.CommandText = sql;
                        try
                        {
                            var result = (int)cmd.ExecuteScalar();
                            if (result == 1)//找到记录，更新原记录
                            {
                                if (option == 1 || option == 3)//重排，预重排 覆盖原记录计划数量的值
                                {
                                    sql = $"update caiGShJB001 set jiHShL = {PORC}， wanChShL = 0.00， leiJXDShL = 0.00， jinE = 0.00， heJJE = 0.00 where wuLBM = {stock.wuLBM}";
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
                        sql = $"select count(wuLBM) from gongXZhYZhBDZhB where gongZLH = {item.gongZLH} and qiH = {item.qiH} and wuLBM = {stock.wuLBM}";
                        cmd.CommandText = sql;
                        try
                        {
                            var result = (int)cmd.ExecuteScalar();
                            if (result == 1)//找到记录，更新原记录
                            {
                                if (option == 1 || option == 3)//重排，预重排 覆盖原记录计划数量的值
                                {
                                    //?? 给哪些字段赋值
                                    sql = $"delete from gongXZhYZhBDZhB where gongZLH = {item.gongZLH} and qiH = {item.qiH} and wuLBM = {stock.wuLBM} ";
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

                            sql = sql + $"insert into gongXZhYZhBDZhB (wuLBM,chanPMCh，gongXBSh) values ({stock.wuLBM},{chanPMCh}，{stock.zum})";

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
                        sql = $"select count(wuLBM) from gongXZhYZhBDCB where gongZLH = {item.gongZLH} and qiH = {item.qiH} and xuH = {item.xuH} and wuLBM = {stock.wuLBM}";
                        cmd.CommandText = sql;
                        try
                        {
                            var result = (int)cmd.ExecuteScalar();
                            if (result == 1)//找到记录，更新原记录
                            {
                                if (option == 1 || option == 3)//重排，预重排 覆盖原记录计划数量的值
                                {
                                    //
                                    sql = $"delete from gongXZhYZhBDCB where gongZLH = {item.gongZLH} and qiH = {item.qiH} and xuH = {item.xuH} and wuLBM = {stock.wuLBM} ";
                                }
                                else//续排 报错??
                                {
                                    log.Error(string.Format($"续排，物料为自制件，工序转移准备单从表已有记录r\nsql[{sql}]\n"));
                                    return false;
                                }
                            }

                            sql = sql + $"insert into gongXZhYZhBDCB (gongZLH, qiH, xuH, wuLBM, jiHShL,tuH, guiG ) values ({item.gongZLH},{item.qiH},{item.xuH},{stock.wuLBM},{PORC},{stock.tuhao},{stock.guige})";

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
                        sql = $"select count(wuLBM) from touLCGXD where gongZLH = {item.gongZLH} and qiH = {item.qiH} and wuLBM = {stock.wuLBM}";
                        cmd.CommandText = sql;
                        try
                        {
                            var result = (int)cmd.ExecuteScalar();
                            if (result == 1)//找到记录，更新原记录
                            {
                                if (option == 1 || option == 3)//重排，预重排 覆盖原记录计划数量的值
                                {
                                    sql = $"update touLCGXD set queJShL = {PORC}， chunJHQJ = 0.00， leiJXDShL = 0.00 where gongZLH = {item.gongZLH} and qiH = {item.qiH} and wuLBM = {stock.wuLBM}";
                                }
                                else//续排 在原记录基础上累加??
                                {
                                    sql = $"update touLCGXD set queJShL = queJShL + {PORC} where gongZLH = {item.gongZLH} and qiH = {item.qiH} and wuLBM = {stock.wuLBM}";
                                }

                            }
                            else//找不到则登记新记录:缺件数量=PORC ?? 纯计划缺件取值待定
                            {
                                sql = $"insert into touLCGXD (wuLBM,queJShL, chunJHQJ,qiH,gongZLH,shengChXSh，touLBSh) values ({stock.wuLBM},{PORC},0.00,{item.qiH},{item.gongZLH},{stock.shengChXSh},{stock.touLBSh})";
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
                             //外协出库准备单主表waiXChKZhBDZB  ??
                        sql = $"select count(wuLBM) from waiXChKZhBDZB where gongZLH = {item.gongZLH} and qiH = {item.qiH} and xuH = {item.xuH} and wuLBM = {stock.wuLBM}";
                        cmd.CommandText = sql;
                        try
                        {
                            var result = (int)cmd.ExecuteScalar();
                            if (result == 1)//找到记录，更新原记录
                            {
                                if (option == 1 || option == 3)//重排，预重排 覆盖原记录计划数量的值
                                {
                                    //
                                    sql = $"delete from waiXChKZhBDZB where gongZLH = {item.gongZLH} and qiH = {item.qiH} and xuH = {item.xuH} and wuLBM = {stock.wuLBM} ";
                                }
                                else//续排 报错??
                                {
                                    log.Error(string.Format($"续排，物料为外协件，外协出库准备单主表已有记录r\nsql[{sql}]\n"));
                                    return false;
                                }

                            }

                            sql = sql + $"insert into waiXChKZhBDZB (gongZLH, qiH, xuH, wuLBM, tuH, guiG ) values ({item.gongZLH},{item.qiH},{item.xuH},{stock.wuLBM},{stock.tuhao},{stock.guige})";

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
                        sql = $"select count(wuLBM) from waiXChKZhBDCB where gongZLH = {item.gongZLH} and qiH = {item.qiH} and xuH = {item.xuH} and wuLBM = {stock.wuLBM}";
                        cmd.CommandText = sql;
                        try
                        {
                            var result = (int)cmd.ExecuteScalar();
                            if (result == 1)//找到记录，更新原记录
                            {
                                if (option == 1 || option == 3)//重排，预重排 覆盖原记录计划数量的值
                                {
                                    //
                                    sql = $"delete from waiXChKZhBDCB where gongZLH = {item.gongZLH} and qiH = {item.qiH} and xuH = {item.xuH} and wuLBM = {stock.wuLBM} ";
                                }
                                else//续排 报错??
                                {
                                    log.Error(string.Format($"续排，物料为外协件，外协出库准备单从表已有记录r\nsql[{sql}]\n"));
                                    return false;
                                }

                            }

                            sql = sql + $"insert into waiXChKZhBDCB (gongZLH, qiH, xuH, wuLBM, tuH, guiG, jiHShL ) values ({item.gongZLH},{item.qiH},{item.xuH},{stock.wuLBM},{stock.tuhao},{stock.guige},{PORC})";

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
                        sql = $"select count(wuLBM) from gongXZhYZhBDZhB where gongZLH = {item.gongZLH} and qiH = {item.qiH} and wuLBM = {stock.wuLBM}";
                        cmd.CommandText = sql;
                        try
                        {
                            var result = (int)cmd.ExecuteScalar();
                            if (result == 1)//找到记录，更新原记录
                            {
                                if (option == 1 || option == 3)//重排，预重排 覆盖原记录计划数量的值
                                {
                                    //?? 给哪些字段赋值
                                    sql = $"delete from gongXZhYZhBDZhB where gongZLH = {item.gongZLH} and qiH = {item.qiH} and wuLBM = {stock.wuLBM} ";
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

                            sql = sql + $"insert into gongXZhYZhBDZhB (wuLBM,chanPMCh，gongXBSh) values ({stock.wuLBM},{chanPMCh}，{stock.zum})";

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
                        sql = $"select count(wuLBM) from gongXZhYZhBDCB where gongZLH = {item.gongZLH} and qiH = {item.qiH} and xuH = {item.xuH} and wuLBM = {stock.wuLBM}";
                        cmd.CommandText = sql;
                        try
                        {
                            var result = (int)cmd.ExecuteScalar();
                            if (result == 1)//找到记录，更新原记录
                            {
                                if (option == 1 || option == 3)//重排，预重排 覆盖原记录计划数量的值
                                {
                                    //
                                    sql = $"delete from gongXZhYZhBDCB where gongZLH = {item.gongZLH} and qiH = {item.qiH} and xuH = {item.xuH} and wuLBM = {stock.wuLBM} ";
                                }
                                else//续排 报错??
                                {
                                    log.Error(string.Format($"续排，物料为自制件，工序转移准备单从表已有记录r\nsql[{sql}]\n"));
                                    return false;
                                }

                            }

                            sql = sql + $"insert into gongXZhYZhBDCB (gongZLH, qiH, xuH, wuLBM, jiHShL,tuH, guiG ) values ({item.gongZLH},{item.qiH},{item.xuH},{stock.wuLBM},{PORC},{stock.tuhao},{stock.guige})";

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
            long tmpWuLBM = 0l;
            Bom bom = new Bom();
            SqlDataReader reader = null;

            sql = $"select daiWLBM from daiYJJHZhB z, daiYJJHCB c where c.zhuBBSh = z.biaoSh and c.yuanWLBM = z.wuLBM and c.yuanWLTH = z.tuH and c.yuanWLGG=z.GuiG and z.qiH='{qiH}' and z.gongZLH = '{gongZLH}' and z.wuLBM={wuLBM}";
            cmd.CommandText = sql;
            try
            {
                reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    tmpWuLBM = Convert.ToInt64(reader["daiWLBMh"].ToString());
                }
                else {
                    tmpWuLBM = wuLBM;
                }

            }
            catch (Exception e)
            {
                log.Error(string.Format($"查询代用件计划表失败，SQL={sql}\nError{e.StackTrace}"));
                return false;
            }
            finally
            {
                reader?.Close();
            }

            sql = $"select * from BOM where CmId = {tmpWuLBM}";
            cmd.CommandText = sql;
            try
            {
                reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    bom.materielIdentfication = wuLBM;
                    bom.tmpId = Convert.ToInt64(reader["tmpId"].ToString());
                    bom.CTmpId = Convert.ToInt64(reader["CTmpId"].ToString());
                    bom.CmId = Convert.ToInt64(reader["CmId"].ToString());
                    bom.CNum = Convert.ToDecimal(reader["CNum"].ToString());
                    bom.rlSeqNo = Convert.ToInt32(reader["rlSeqNo"].ToString());
                    bom.peiTNo = Convert.ToInt32(reader["peiTNo"].ToString());
                }
                else
                {
                    log.Error(string.Format($"查询BOM失败，SQL={sql}\n"));
                    return false;
                }
            }
            catch (Exception e)
            {
                log.Error(string.Format($"查询BOM失败，SQL={sql}\nError{e.StackTrace}"));
                return false;
            }
            finally
            {
                reader.Close();
            }

            bomTree.Add(bom);

            sql = $"select * from BOM where materielIdentfication = {wuLBM} ";
            cmd.CommandText = sql;
            try
            {
                reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {

                    while (reader.Read())
                    {
                        var childMId = Convert.ToInt64(reader["CmId"].ToString());
                        if (!AddBomItem(childMId,qiH,gongZLH))
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
                log.Error(string.Format($"查询BOM失败，SQL={sql}\nError{e.StackTrace}"));
                return false;
            }
            finally
            {
                reader.Close();
            }

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

        public int flag { get; set; }  //1.本次排产用到该物料 0.没有用到
    }

    public class Bom
    {
        public long materielIdentfication { get; set; }     //父物料编码
        public long tmpId { get; set; }         //父物料模板ID
        public long CmId { get; set; }          //物料编码
        public long CTmpId { get; set; }        //物料模板ID
        public decimal CNum { get; set; }
        public int rlSeqNo { get; set; }
        public int peiTNo { get; set; }         //配套标识
        public long substitute { get; set; }    //替代物料编码
    }
}