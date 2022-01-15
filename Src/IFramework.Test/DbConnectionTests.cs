using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit.Abstractions;

namespace IFramework.Test
{
    public class DbConnectionTests
    {
        private readonly ITestOutputHelper _output;

        public DbConnectionTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void MaxConnectionSizeTest()
        {
            var connectionString = "Server=(localdb)\\projects;Database=IFramework.DemoDb;Integrated Security=true;Max Pool Size = 9;Connect Timeout=1;";
            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Factory.StartNew((state)=>
                {

                    //var mySqlConnectionStringBuilder = new MySqlConnectionStringBuilder();
                    //mySqlConnectionStringBuilder["Server"] = "10.5.6.41";
                    //mySqlConnectionStringBuilder["Uid"] = "root";
                    //mySqlConnectionStringBuilder["pwd"] = "qazWSX123";
                    //mySqlConnectionStringBuilder["database"] = "dotnet_bpm_1000_zf2";xianz
                    //mySqlConnectionStringBuilder["SslMode"] = "none";
                    //mySqlConnectionStringBuilder["Allow User Variables"] = "True";
                    ////mySqlConnectionStringBuilder["Connect Timeout"] = "10";
                    ////mySqlConnectionStringBuilder["Pooling"] = "true";
                    ////mySqlConnectionStringBuilder["Maximum Pool Size"] = "180";
                    ////mySqlConnectionStringBuilder["Minimum Pool Size"] = "0";
                    //mySqlConnectionStringBuilder["Command Timeout"] = "3600";
                    //mySqlConnectionStringBuilder["Connection Idle Timeout"] = "5";
                    //mySqlConnectionStringBuilder["Connection Idle Ping Time"] = "30";

                    using (var connection =
                           new SqlConnection(connectionString))
                    {
                        using var command = connection.CreateCommand();
                        command.CommandText = @" WAITFOR DELAY '00:00:01'; select * from users
                        ";
                        if (connection.State == ConnectionState.Closed)
                        {
                            connection.Open();
                        }

                        //command.CommandTimeout = 1;
                        using var reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            var id = reader.GetString("Id");
                            _output.WriteLine($"{DateTime.Now}: {state}:{id}");
                            //Thread.Sleep(1000);

                            //Console.WriteLine(id);
                            // ...
                        }
                    }

                }, i));
            } 
            Task.WaitAll(tasks.ToArray());
            _output.WriteLine($"failed count: {tasks.Count(t => t.IsFaulted)}");
        }
    }
}
