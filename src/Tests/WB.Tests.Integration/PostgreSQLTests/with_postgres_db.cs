using System;
using System.Configuration;
using Npgsql;
using NUnit.Framework;
using WB.Core.GenericSubdomains.Portable;

namespace WB.Tests.Integration.PostgreSQLTests
{
    public class with_postgres_db
    {
        [OneTimeSetUp]
        protected static void Context()
        {
            TestConnectionString = TestsConfigurationManager.ConnectionString;
            databaseName = "testdb_" + Guid.NewGuid().FormatGuid();
            ConnectionStringBuilder = new NpgsqlConnectionStringBuilder(TestConnectionString)
            {
                Database = databaseName
            };

            using (var connection = new NpgsqlConnection(TestConnectionString))
            {
                connection.Open();
                var command = $"CREATE DATABASE {databaseName} ENCODING = 'UTF8'";
                using (var sqlCommand = connection.CreateCommand())
                {
                    sqlCommand.CommandText = command;
                    sqlCommand.ExecuteNonQuery();
                }
                connection.Close();
            }
        }

        [OneTimeTearDown]
        protected static void Cleanup()
        {
            using (var connection = new NpgsqlConnection(TestConnectionString))
            {
                connection.Open();
                var command = string.Format(
                    @"SELECT pg_terminate_backend (pg_stat_activity.pid) FROM pg_stat_activity WHERE pg_stat_activity.datname = '{0}'; DROP DATABASE {0};",
                    databaseName);
                using (var sqlCommand = connection.CreateCommand())
                {
                    sqlCommand.CommandText = command;
                    sqlCommand.ExecuteNonQuery();
                }
                connection.Close();
            }
        }

        protected static NpgsqlConnectionStringBuilder ConnectionStringBuilder;
        protected static string TestConnectionString;
        private static string databaseName;
    }
}
