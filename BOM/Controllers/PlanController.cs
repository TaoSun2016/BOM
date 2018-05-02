using BOM.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace BOM.Controllers
{
    [RoutePrefix("Plan")]
    public class PlanController : ApiController
    {
        log4net.ILog log = log4net.LogManager.GetLogger("Plan");

        [HttpPost]
        [Route("Create")]
        public void CreatePlan(int option, List<PlanItem> requestItems)
        {
            SqlConnection sqlConnection = DBConnection.OpenConnection();
            SqlCommand command = new SqlCommand();

            SqlTransaction transaction = sqlConnection.BeginTransaction();
            command.Connection = sqlConnection;
            command.Transaction = transaction;

            Plan plan = new Plan(option, sqlConnection, command, transaction);

            if (!plan.CreatePlan(requestItems))
            {
                transaction.Rollback();
                command.Dispose();
                DBConnection.CloseConnection(sqlConnection);

                string logMessage = string.Format($"Create Plan error!\n");
                log.Error(logMessage);
                var responseMessge = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(logMessage),
                    ReasonPhrase = "Create Plan error!"
                };
                throw new HttpResponseException(responseMessge);
            }
            transaction.Commit();
            command.Dispose();
            DBConnection.CloseConnection(sqlConnection);
        }
    }
}
