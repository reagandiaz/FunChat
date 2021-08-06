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
            {
                var cresult = await user.CreateChannel(password);
                if (cresult.Success == true)
                    channelinfo.Add(cresult.Info);
            }
            return channelinfo.ToArray();
        }


        internal async Task JoinChannels(string creator, string subscriber, int channelcount, bool isvalid)
        {
            var data = await CreateChannels(creator, channelcount);
            var user = _cluster.GrainFactory.GetGrain<IUser>(Guid.NewGuid());
            await user.Login(subscriber, subscriber);
            Result<ChannelInfo> channelinfo = new Result<ChannelInfo>();
            for (int i = 0; i < channelcount; i++)
            {
                channelinfo = await user.JoinChannel(data[i].Name, password);
            }

            //The last should be failed

            var channels = await user.CurrentChannels();

            if (isvalid)
                Assert.True(channelinfo.Success == true && channels.Info.Length <= 2);
            else
                Assert.True(channelinfo.Success == false && channels.Info.Length == 2);
        }


        internal async Task JoinLeave(string creator, string subscriber)
        {
            int channelcount = 1;
            var data = await CreateChannels(creator, channelcount);
            var user = _cluster.GrainFactory.GetGrain<IUser>(Guid.NewGuid());
            await user.Login(subscriber, subscriber);
            Result<ChannelInfo> channelinfo = new Result<ChannelInfo>();
            for (int i = 0; i < channelcount; i++)
                channelinfo = await user.JoinChannel(data[i].Name, password);

            var oldmembers = await user.GetChannelMembers(channelinfo.Info.Name);
            await user.LeaveChannelByName(channelinfo.Info.Name);

            var adminuser = _cluster.GrainFactory.GetGrain<IUser>(Guid.NewGuid());
            await adminuser.Login(admin, admin);

            var members = await adminuser.GetChannelMembers(channelinfo.Info.Name);

            Assert.True(oldmembers.Info.Length > members.Info.Length);
        }


        //I can join a private channel with the channel name and password.
        //Maximum join 2 channels.
        //I can left any channel other than the default channel

        const string password = "password";
        const string admin = "Admin";
        const string generic = "generic";

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
            await user.LeaveChannelByName(generic);
            var members = await user.GetChannelMembers(generic);
            Assert.True(members.Success == true && members.Info.Contains(subscriber));
        }
    }
}
