using Microsoft.Extensions.Configuration;
using Orleans;
using Orleans.Hosting;
using Orleans.TestingHost;
using System;

namespace FunChat.UnitTest.Tools
{
    public class ClusterFixture : IDisposable
    {
        public ClusterFixture()
        {
            var builder = new TestClusterBuilder();
            builder.Options.ServiceId = Guid.NewGuid().ToString();

            builder.AddSiloBuilderConfigurator<TestSiloConfigurations>();
            builder.AddClientBuilderConfigurator<TestClientConfigurations>();

            var cluster = builder.Build();

            cluster.Deploy();
            this.Cluster = cluster;
        }

        public void Dispose()
        {
            this.Cluster.StopAllSilos();
        }

        public TestCluster Cluster { get; private set; }
    }


    public class TestSiloConfigurations : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.AddMemoryGrainStorage("PubSubStore");
            siloBuilder.AddSimpleMessageStreamProvider("FunChat");
        }
    }

    public class TestClientConfigurations : IClientBuilderConfigurator
    {
        public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
        {
            clientBuilder.AddSimpleMessageStreamProvider("FunChat");
        }
    } 
}
