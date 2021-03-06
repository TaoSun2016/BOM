﻿using BOM.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Data.Common;
using BOM.DbAccess;

namespace BOM.Controllers
{
    [RoutePrefix("BOM")]
    //排产
    public class PlanController : ApiController
    {
        log4net.ILog log = log4net.LogManager.GetLogger("Plan");

        [HttpPost]
        [Route("Plan")]
        public void CreatePlan(List<PlanItem> requestItems)
        {
            log.Error("plan-1");
            foreach (PlanItem item in requestItems)
            {
                log.Error("list item - " + item.wuLBM.ToString());
                log.Error("list item - " + item.gongZLH);
                log.Error("list item - " + item.qiH);
            }

            //PlanRequest request = new PlanRequest();
            DbConnection connection = DbUtilities.GetConnection();
            DbCommand command = connection.CreateCommand();
            DbTransaction transaction = connection.BeginTransaction();
            command.Transaction = transaction;

            Plan plan = new Plan(requestItems[0].option, connection, command, transaction);
            log.Error("plan-2");
            if (!plan.CreatePlan(requestItems))
            {
                transaction.Rollback();
                command.Dispose();
                connection.Close();

                string logMessage = string.Format($"Create Plan error!\n");
                log.Error(logMessage);
                var responseMessge = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(logMessage),
                    ReasonPhrase = "Create Plan error!"
                };
                throw new HttpResponseException(responseMessge);
            }
            log.Error("plan-4");
            transaction.Commit();
            command.Dispose();
            connection.Close();
        }
    }
    public class PlanRequest
    {
        public int option { get; set; }
        public List<PlanItem> requestItems { get; set; }
    }
}
