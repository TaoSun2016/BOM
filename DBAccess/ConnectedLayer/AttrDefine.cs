using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;

namespace DBAccess.ConnectedLayer
{
    public class AttrDefine
    {
        private SqlConnection sqlConnection = DBConnection.OpenConnection();

        public void Insert(string tmpId, string attrId, string attrNm, char attrTp, string crter)
        {
            string crtDate = DateTime.Now.ToString("yyyyMMdd HH:mm:ss");
            string sql = "INSERT INTO AttrDefine " +  $"(TmpId, AttrId, AttrNm, CrtDate, Crter) Values ('{tmpId}','{attrId}', '{attrNm}', '{crtDate}','{crter}')";
            using (SqlCommand command = new SqlCommand(sql, sqlConnection))
            {
                command.ExecuteNonQuery();
            }
        }
    }
}
