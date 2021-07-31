using FunChat.GrainIntefaces;
using FunChat.Grains.Tools;
using FunChat.UnitTest.Tools;
using Orleans.TestingHost;
using System;
using System.Threading.Tasks;
using Xunit;


namespace FunChat.UnitTest
{
    [Collection(ClusterCollection.Name)]
    public class PrivateChannelCreateTest
    {
        private readonly TestCluster _cluster;

        public PrivateChannelCreateTest(ClusterFixture fixture)
        {
            _cluster = fixture.Cluster;
        }

        internal async Task CreateChannel(string username, string channelpassword, bool isvalid)
        {
            var user = _cluster.GrainFactory.GetGrain<IUser>(Guid.NewGuid());
            await user.Login(username, username);
            var channelinfo = await user.CreateChannel(channelpassword);
            if (isvalid)
                Assert.True((new NameValidator(channelinfo.Name)).IsValid(6, 6));
            else
                Assert.True(channelinfo.Name == string.Empty);
        }

        //I can create a private channel with a password.
        //System will generate a random unique channel name(alphanumeric) with 6 characters.
        //The password must be alphanumeric and 6  ~ 18 characters.

        [Fact]
        public async Task Password5CharsInvalid()
        {
            await CreateChannel("userd", "12345", false);
        }
        [Fact]
        public async Task Password6CharsValid()
        {
            await CreateChannel("usere", "123456", true);
        }
        [Fact]
        public async Task Password18CharsValid()
        {
            await CreateChannel("userf", "123456789123456789", true);
        }
        [Fact]
        public async Task Password19CharsInvalid()
        {
            await CreateChannel("userg", "1234567891234567891", false);
        }
    }
}