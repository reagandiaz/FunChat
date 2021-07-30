using FunChat.GrainIntefaces;
using FunChat.UnitTest.Tools;
using Orleans.TestingHost;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FunChat.UnitTest
{
    [Collection(ClusterCollection.Name)]
    public class HelloGrainTests
    {
        private readonly TestCluster _cluster;

        public HelloGrainTests(ClusterFixture fixture)
        {
            _cluster = fixture.Cluster;
        }

        [Fact]
        public async Task SaysHelloCorrectly()
        {
            var hello = _cluster.GrainFactory.GetGrain<IHello>(Guid.NewGuid());
            var greeting = await hello.SayHello("Good morning, HelloGrain!");
            Assert.Equal("Client said: 'Good morning, HelloGrain!', so HelloGrain says: Hello!", greeting);
        }
    }
}
