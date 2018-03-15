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
        List<Stock> stocks = new List<Stock>();
        public Plan()
        {
            connection = DBConnection.OpenConnection();
        }



        /// <summary>
        /// 排产
        /// </summary>
        public void ProductionPlan(int option, List<PlanItem> requestItems)
        {
            string sql = null;

            List<ShengChJH> listJH = new List<ShengChJH>();
            List<PlanItem> items = requestItems.OrderBy(m => m.qiH).ThenBy(m => m.xuH).ThenBy(m => m.gongZLH).ToList();

            //初始化库存数据
            InitData(option);

            //遍历生产计划记录
            foreach (PlanItem item in items)
            {
                Calculate(option, item);
            }

            //更新上期库存量
            UpdatePAB();
        }

        private int InitData(int option)
        {
            string sql = null;
            SqlDataReader dataReader = null;

            if ( option == 1 || option == 3)//重排，预重排
            {

            }
            else if(option == 2 || option == 4)//续拍，预续排
            {

            }
            else
            {
                return -1;
            }
            return 0;
        }

        /// <summary>
        /// 计算单个物料的缺件信息和相关表单数据
        /// </summary>
        private void Calculate(int option, PlanItem item)
        {

            decimal GR = shuL;      //毛需求量 GR 由前台上宋
            decimal POH = 0.0000m;       //预计在库量
            decimal SOR = 0.0000m;       //在途数量
            decimal OH = 0.0000m;        //在库数量
            decimal PAB = 0.0000m;       //上期库存数量
            decimal SS = 0.0000m;        //安全库存
            decimal NR = 0.0000m;        //净需求数量NR
            decimal LS = 0.0000m;        //批量规则
            decimal PORC = 0.0000m;      //计算计划订单收料量PORC
            string SHENGCLX = null;     //生产类型 0原材料,1自制件,2外协,3半成品，4成品

            string sql = null;
            SqlDataReader dataReader = null;

            //获取DeafaultAttr表中当前物料的参数
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = connection;
                sql = $"select * from DeafaultAttr where materielIdentfication = {wuLBM}";
                cmd.CommandText = sql;
                try
                {
                    dataReader = cmd.ExecuteReader();
                    if (dataReader.HasRows)
                    {
                        dataReader.Read();
                        SOR = Convert.ToDecimal(dataReader["SR"].ToString());       //在途数量
                        PAB = Convert.ToDecimal(dataReader["_STORE_"].ToString());  //上期库存数量
                        SS = Convert.ToDecimal(dataReader["SS"].ToString());        //安全库存
                        LS = Convert.ToDecimal(dataReader["LS"].ToString());        //批量规则
                        SHENGCLX = dataReader["SHENGCLX"].ToString();        //批量规则

                    }
                    else
                    {
                        log.Error(string.Format($"Select DeafaultAttr error!\nsql[{sql}]\nError"));
                        throw new Exception("No data found!");
                    }
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Select DeafaultAttr error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    throw;
                }
                finally
                {
                    dataReader.Close();
                }


                //预计在库量 POH
                if (Option == 1 || Option == 3)//重拍，预重拍
                {

                    //获取在库数量OH
                    sql = $"select sum(kuCSh) from kuCShJB001 where wuLBM = {wuLBM}";
                    cmd.CommandText = sql;
                    try
                    {
                        OH = Convert.ToDecimal(cmd.ExecuteScalar());
                    }
                    catch (Exception e)
                    {
                        log.Error(string.Format($"Get OH error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                        throw;
                    }


                    //预计在库数量POH = 在途数量SOR + 在库数量OH - 毛需求量GR
                    POH = SOR + OH - GR;
                }
                else//续拍
                {
                    //预计在库数量POH=上期期末可用库存数量PAB-毛需求量GR
                    POH = PAB - GR;
                }

                //计算净需求数量NR
                if (POH - SS >= 0.00005m)
                {
                    NR = 0.0000m;
                }
                else
                {
                    NR = SS - POH;
                }

                //计算计划订单收料量PORC,即为缺件数量
                PORC = 0.0000m;
                if (NR > 0.00005m)
                {
                    do
                    {
                        PORC += LS;
                    } while (PORC - NR < -0.00005m);
                }

                //登记缺件表
                sql = $"insert into ullageTempSingle values ";

                //计算本期库存数量PAB = 计划订单收料量PORC + 预计在库量POH
                PAB = PORC + POH;
                //考虑更新PAB??   中间结果存内存，最终的值存在DeafaultAttr的store字段
                //todo

                //根据生产类型登记数据库表
                switch (SHENGCLX)
                {
                    case "0"://0原材料
                        break;
                    case "1"://1自制件
                        break;
                    case "2"://2外协
                        break;
                    case "3"://3半成品
                        break;
                    case "4"://4成品
                        break;
                    default:
                        break;
                }





            }

        }

        //处理原材料
        private bool Process_0(SqlCommand cmd)
        {
            // update DeafaultAttr set _store_ = PAB
            // update caiGShJB001 采购数据表
            return true;
        }

        //处理自制件
        private bool Process_1(SqlCommand cmd)
        {
            // update DeafaultAttr set _store_ = PAB
            // delete gongXZhYZhBDZhB    工序转移准备单主表 应该只删除本令号本期号的吧？？？
            // insert gongXZhYZhBDZhB   根据期号和物料编码的姓编码，编成一个单据主表
            //delete gongXZhYZhBDCB     工序转移准备单从表 应该只删除本令号本期号的吧？？？
            // insert gongXZhYZhBDCB 令号+顺序号+物料编码+名称+规格+图号+计划数量  其它不用处理
            // 更新投料采购下达， update touLCGXD
            return true;
        }

        //外协
        private bool Process_2(SqlCommand cmd)
        {
            // update DeafaultAttr set _store_ = PAB
            //清空更新[dbo].[外协出库准备单主表]，[dbo].[外协出库准备单从表]
            return true;
        }

        //半成品
        private bool Process_3(SqlCommand cmd)
        {
            // update DeafaultAttr set _store_ = PAB

            // delete gongXZhYZhBDZhB    工序转移准备单主表 应该只删除本令号本期号的吧？？？
            // insert gongXZhYZhBDZhB   根据期号和物料编码的姓编码，编成一个单据主表
            //delete gongXZhYZhBDCB     工序转移准备单从表 应该只删除本令号本期号的吧？？？
            // insert gongXZhYZhBDCB 令号+顺序号+物料编码+名称+规格+图号+计划数量  其它不用处理
            
            return true;
        }

        //成品
        private bool Process_4(SqlCommand cmd)
        {
            //清空更新[dbo].[成品入库准备单主表]，[dbo].[成品入库准备单从表]
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

    public class ShengChJH
    {
        public int biaoSh { get; set; }//标识
        public int zhuBBSh { get; set; }//主表标识
        public int xuH { get; set; }//序号
        public string qiH { get; set; }//期号
        public string gongZLH { get; set; }//工作令号
        public int wuLBSh { get; set; }//物料标识
        public long wuLBM { get; set; }//物料编码
        public string wuLMCh { get; set; }//物料名称
        public string tuH { get; set; }//图号
        public string guiG { get; set; }//规格
        public string chanPXH { get; set; }//产品型号
        public decimal jiHShl { get; set; }//计划数量
        public decimal boaXL { get; set; }//保险量
        public decimal feiPL { get; set; }//废品率
        public decimal shiJShL { get; set; }//实际数量
        public DateTime jiaoHQ { get; set; }//交货期
        public int shenChZhQ { get; set; }//生产周期
        public int tiHLH { get; set; }//替换令号
        public decimal wanChShL { get; set; }//完成数量
        public decimal weiWShL { get; set; }//未完数量
        public string jiHXDBSh { get; set; }//计划下达标识
        public int jiHZhTBSh { get; set; }//计划状态标识
        public string jiHZhTMCh { get; set; }//计划状态名称

        public string xiaoShLH { get; set; }//销售令号
        public int youXJ { get; set; }//优先级
        public string beiZh { get; set; }//备注
        public int heTTZhDCBBSh { get; set; }//合同通知单从表标识
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
        public decimal HO;         //在库数量
        public decimal POH;        //预计在库量
        public decimal PAB;        //上期可用库存量
    }

}