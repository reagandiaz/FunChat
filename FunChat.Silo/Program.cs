﻿using System;
using System.Threading.Tasks;
using FunChat.Grains;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace FunChat.Silo
{
    public class Program
    {
        public static int Main()
        {
            return RunMainAsync().Result;
        }

        private static async Task<int> RunMainAsync()
        {
            try
            {
                var host = await StartSilo();
                Console.WriteLine("\n\n Press Enter to terminate...\n\n");
                Console.ReadLine();

                await host.StopAsync();

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 1;
            }
        }

        private static async Task<ISiloHost> StartSilo()
        {
            // define the cluster configuration
            var builder = new SiloHostBuilder()
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "OrleansBasics";
                })
                .AddSimpleMessageStreamProvider("FunChat")
                .ConfigureLogging(logging => logging.AddConsole())
                .AddMemoryGrainStorage("PubSubStore");

            var host = builder.Build();
            await host.StartAsync();
            return host;
        }
    }
}