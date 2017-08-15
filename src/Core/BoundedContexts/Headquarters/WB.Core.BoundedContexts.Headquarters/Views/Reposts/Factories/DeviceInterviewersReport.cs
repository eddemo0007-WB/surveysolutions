﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using System.Threading.Tasks;
using Dapper;
using NHibernate;
using Ninject;
using Npgsql;
using WB.Core.BoundedContexts.Headquarters.OwinSecurity;
using WB.Core.BoundedContexts.Headquarters.Services;
using WB.Core.BoundedContexts.Headquarters.Views.Reposts.Views;
using WB.Core.BoundedContexts.Headquarters.Views.User;
using WB.Core.GenericSubdomains.Portable;
using WB.Infrastructure.Native.Storage.Postgre;

namespace WB.Core.BoundedContexts.Headquarters.Views.Reposts.Factories
{
    public class DeviceInterviewersReport : IDeviceInterviewersReport
    {
        private readonly PostgresPlainStorageSettings plainStorageSettings;
        private readonly IInterviewerVersionReader interviewerVersionReader;

        public DeviceInterviewersReport(PostgresPlainStorageSettings plainStorageSettings,
            IInterviewerVersionReader interviewerVersionReader)
        {
            this.plainStorageSettings = plainStorageSettings;
            this.interviewerVersionReader = interviewerVersionReader;
        }

        public async Task<DeviceInterviewersReportView> LoadAsync(string filter, OrderRequestItem order, int pageNumber, int pageSize)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));

            if (!order.IsSortedByOneOfTheProperties(typeof(DeviceInterviewersReportLine)))
            {
                throw new ArgumentException("Invalid order by column passed", nameof(order));
            }

            var targetInterviewerVersion = interviewerVersionReader.Version;

            var sql = GetSqlTexts();

            using (var connection = new NpgsqlConnection(plainStorageSettings.ConnectionString))
            {
                var fullQuery = string.Format(sql.query, order.ToSqlOrderBy());
                var rows = await connection.QueryAsync<DeviceInterviewersReportLine>(fullQuery, new
                {
                    latestAppBuildVersion = targetInterviewerVersion,
                    neededFreeStorageInBytes = 100 * 1024,
                    minutesMismatch = 15,
                    targetAndroidSdkVersion = 20,
                    limit = pageSize,
                    offset = pageSize * (pageNumber - 1),
                    filter = filter + "%"
                });
                int totalCount = await connection.ExecuteScalarAsync<int>(sql.countQuery, new {filter = filter + "%" });

                return new DeviceInterviewersReportView
                {
                    Items = rows,
                    TotalCount = totalCount
                };
            }
        }

        public Task<ReportView> GetReport(string filter, OrderRequestItem order, int pageNumber, int pageSize)
        {
            throw new NotImplementedException();
        }

        private (string query, string countQuery) GetSqlTexts()
        {
            string query;
            string countQuery;
            var assembly = typeof(DeviceInterviewersReport).Assembly;
            using (Stream stream = assembly.GetManifestResourceStream("WB.Core.BoundedContexts.Headquarters.Views.Reposts.Factories.DeviceInterviewersReport.sql"))
            using (StreamReader reader = new StreamReader(stream))
            {
                query = reader.ReadToEnd();
            }
            using (Stream stream = assembly.GetManifestResourceStream("WB.Core.BoundedContexts.Headquarters.Views.Reposts.Factories.DeviceInterviewersReportCount.sql"))
            using (StreamReader reader = new StreamReader(stream))
            {
                countQuery = reader.ReadToEnd();
            }

            return (query, countQuery);
        }
    }
}