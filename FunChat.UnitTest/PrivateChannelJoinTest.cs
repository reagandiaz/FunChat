using FunChat.GrainIntefaces;
using FunChat.UnitTest.Tools;
using Orleans.TestingHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;


namespace FunChat.UnitTest
{
    [Collection(ClusterCollection.Name)]
    public class PrivateChannelJoinTest
    {
        private readonly TestCluster _cluster;

        public PrivateChannelJoinTest(ClusterFixture fixture)
        {
            _cluster = fixture.Cluster;
        }

        internal async Task<ChannelInfo[]> CreateChannels(string username, int channelcount)
        {
            List<ChannelInfo> channelinfo = new List<ChannelInfo>();
            var user = _cluster.GrainFactory.GetGrain<IUser>(Guid.NewGuid());
            await user.Login(username, username);
            for (int i = 0; i < channelcount; i++)
                channelinfo.Add(await user.CreateChannel(password));
            return channelinfo.ToArray();
        }


        internal async Task JoinChannels(string creator, string subscriber, int channelcount, bool isvalid)
        {
            var data = await CreateChannels(creator, channelcount);
            var user = _cluster.GrainFactory.GetGrain<IUser>(Guid.NewGuid());
            await user.Login(subscriber, subscriber);
            ChannelInfo channelinfo = new ChannelInfo();
            for (int i = 0; i < channelcount; i++)
                channelinfo = await user.JoinChannel(data[i].Name, password);

            if (isvalid)
                Assert.True(channelinfo.Name != string.Empty);
            else
                Assert.True(channelinfo.Name == string.Empty);
        }


        internal async Task JoinLeave(string creator, string subscriber)
        {
            int channelcount = 1;
            var data = await CreateChannels(creator, channelcount);
            var user = _cluster.GrainFactory.GetGrain<IUser>(Guid.NewGuid());
            await user.Login(subscriber, subscriber);
            ChannelInfo channelinfo = new ChannelInfo();
            for (int i = 0; i < channelcount; i++)
                channelinfo = await user.JoinChannel(data[i].Name, password);

            var channel = _cluster.GrainFactory.GetGrain<IChannel>(channelinfo.Key);
            var oldmembers = await channel.GetMembers();
            await user.LeaveChannelByName(channelinfo.Name);
            var members = await channel.GetMembers();
            Assert.True(oldmembers.Length > members.Length);
        }


        //I can join a private channel with the channel name and password.
        //Maximum join 2 channels.
        //I can left any channel other than the default channel

        const string password = "password";

        [Fact]
        public async Task Join1ChannelValid()
        {
            await JoinChannels("userh", "useri", 1, true);
        }
        [Fact]
        public async Task Join2ChannelValid()
        {
            await JoinChannels("userj", "userk", 2, true);
        }
        [Fact]
        public async Task Join3ChannelInvalid()
        {
            await JoinChannels("userl", "userm", 3, false);
        }
        [Fact]
        public async Task LeavePrivateChannelValid()
        {
            await JoinLeave("usern", "usero");
        }
        [Fact]
        public async Task LeaveGenericChannelInvalid()
        {
            var subscriber = "userp";
            var user = _cluster.GrainFactory.GetGrain<IUser>(Guid.NewGuid());
            await user.Login(subscriber, subscriber);
            var guid = await user.LocateChannel("generic");
            var channel = _cluster.GrainFactory.GetGrain<IChannel>(guid);
            await user.LeaveChannelByName("generic");
            var members = await channel.GetMembers();
            bool t = members.Contains(subscriber);
            Assert.True(t);
        }
    }
}
