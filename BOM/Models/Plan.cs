using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;

namespace BOM.Models
{
    public class Plan:IDisposable
    {
        log4net.ILog log = log4net.LogManager.GetLogger("Plan");
        private SqlConnection connection = null;

        public Plan()
        {
            connection = DBConnection.OpenConnection();
        }



        /// <summary>
        /// 排产
        /// </summary>
        /// <param name="Option">造作选项：1重拍2续排3预重拍4预续排</param>
        /// <param name="wuLBM">物料编码</param>
        /// <param name="shuL">物料数量</param>
        /// <param name="gongZLH">工作令号</param>
        /// <param name="qiH">期号</param>
        /// <param name="xuH">序号</param>
        public void ProductionPlan(int Option, long wuLBM, int shuL, string gongZLH, string qiH, int xuH)
        {
            string sql = null;
            List<ShengChJH> listJH = new List<ShengChJH>();

            //查询生产计划表
            using (SqlCommand command = new SqlCommand())
            {
                command.Connection = connection;
                SqlDataReader dataReader = null;

                sql = $"SELECT * FROM shengChJH WHERE wuLBM = '{wuLBM}' and gongZLH = '{gongZLH}' and qiH = '{qiH}' and xuH = '{xuH}' order by gongZLH, qiH, xuH, wuLBM";
                command.CommandText = sql;

                try
                {
                    dataReader = command.ExecuteReader();
                    if (dataReader.HasRows)
                    {
                        while (dataReader.Read())
                        {
                            listJH.Add(new ShengChJH() {
                                wuLBM =(long)dataReader["wuLBM"],
                                gongZLH = dataReader["gongZLH"].ToString(),
                                qiH = dataReader["qiH"].ToString(),
                                xuH = (int)dataReader["xuH"]
                            });
                        }

                    }
                    else
                    {
                        log.Error(string.Format($"Select shengChJH error!\nsql[{sql}]\nError"));
                        throw new Exception("No data found!");
                    }
                }
                catch (Exception e)
                {
                    log.Error(string.Format($"Select shengChJH error!\nsql[{sql}]\nError[{e.StackTrace}]"));
                    throw;
                }
                finally
                {
                    dataReader.Close();
                }

            }

            //遍历生产计划记录
            foreach (ShengChJH jh in listJH)
            {
                Calculate(Option, jh.wuLBM, shuL, jh.gongZLH, jh.qiH, jh.xuH);
            }
        }

        /// <summary>
        /// 计算单个物料的缺件信息和相关表单数据
        /// </summary>
        private void Calculate(int Option, long wuLBM, int shuL, string gongZLH, string qiH, int xuH)
        {
            
            int GR = shuL;  //毛需求量 GR
            int POH = 0;    //预计在库量
            int SOR = 0;    //在途数量
            int OH = 0;     //在库数量





            //预计在库量 POH
            if (Option == 1 || Option == 3)//重拍
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = connection;
                    cmd.CommandText = $"select SR from DeafaultAttr where materielIdentfication = {wuLBM}";
                }
            }
            else//续拍
            {

            }
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
        public int jiHShl { get; set; }//计划数量
        public int boaXL { get; set; }//保险量
        public double feiPL { get; set; }//废品率
        public double shiJShL { get; set; }//实际数量
        public DateTime jiaoHQ { get; set; }//交货期
        public int shenChZhQ { get; set; }//生产周期
        public int tiHLH { get; set; }//替换令号
        public double wanChShL { get; set; }//完成数量
        public double weiWShL { get; set; }//未完数量
        public string jiHXDBSh { get; set; }//计划下达标识
        public int jiHZhTBSh { get; set; }//计划状态标识
        public string jiHZhTMCh { get; set; }//计划状态名称

        public string xiaoShLH { get; set; }//销售令号
        public int youXJ { get; set; }//优先级
        public string beiZh { get; set; }//备注
        public int heTTZhDCBBSh { get; set; }//合同通知单从表标识
    }
}