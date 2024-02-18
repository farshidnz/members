using Amazon.S3.Model;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using System;
using System.IO;

namespace SettingsAPI.Web
{

    class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                // Change the following line to LogEventLevel.Information if you need to debug SQL queries,
                // but remember to turn it off when you're done or the ghost of Josh K will smack you
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            WebHost.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                var env = Environment.GetEnvironmentVariable("APP_ENV");
                var devops4Enabled = Environment.GetEnvironmentVariable("Devops4Enabled");    

                if(devops4Enabled != "true")
                {
                    // To enable running locally the old way
                    if (File.Exists("local.json"))
                        config.AddJsonFile("local.json", optional: false, reloadOnChange: true);
                }
                
                config.AddEnvironmentVariables();

            })
            .UseSerilog()
            .UseStartup<Startup>()
            .Build()
            .Run();

        }
    }
}
