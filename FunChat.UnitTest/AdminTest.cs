using FunChat.GrainIntefaces;
using FunChat.UnitTest.Tools;
using Orleans.TestingHost;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;


namespace FunChat.UnitTest
{
    [Collection(ClusterCollection.Name)]
    public class AdminTest
    {
        private readonly TestCluster _cluster;

        public AdminTest(ClusterFixture fixture)
        {
            _cluster = fixture.Cluster;
        }

        internal async Task Login(string username, string password, bool isvalid)
        {
            var user = _cluster.GrainFactory.GetGrain<IUser>(Guid.NewGuid());
            Result<UserInfo> result = new Result<UserInfo>();

            if (user != null)
                result = await user.Login(username, password);

            if (isvalid)
                Assert.True(result.Success == true);
            else
                Assert.True(result.Success == false);
        }

        const string password = "password";
        const string admin = "Admin";

        internal async Task<ChannelInfo[]> CreateChannels(string username, int channelcount)
        {
            List<ChannelInfo> channelinfo = new List<ChannelInfo>();
            var user = _cluster.GrainFactory.GetGrain<IUser>(Guid.NewGuid());
            var result = await user.Login(username, username);
            if (result.Success == true)
            {
                for (int i = 0; i < channelcount; i++)
                {
                    var cresult = await user.CreateChannel(password);
                    if (cresult.Success == true)
                        channelinfo.Add(cresult.Info);
                }
            }
            return channelinfo.ToArray();
        }

        internal async Task<ChannelInfo> JoinChannels(string creator, string subscriber, int channelcount)
        {
            var data = await CreateChannels(creator, channelcount);
            var user = _cluster.GrainFactory.GetGrain<IUser>(Guid.NewGuid());
            await user.Login(subscriber, subscriber);
            Result<ChannelInfo> channelinfo = new Result<ChannelInfo>();
            for (int i = 0; i < channelcount; i++)
            {
                var cinfo = await user.JoinChannel(data[i].Name, password);
                if (cinfo.Success == true)
                    channelinfo = cinfo;
            }
            return channelinfo.Info;
        }

        //I can login to FunChat just like a normal user, if the login name is “Admin”, it will be an admin account.
        //I can list all channels include private channels
        //I can list any channel’s members even it’s private
        //I can delete any channel by channel’s name, after deleting the channel, all channel members will be kicked off from the channel and no longer receive any message.

        [Fact]
        public async Task LoginAdminValid()
        {
            await Login(admin, admin, true);
        }

        [Fact]
        public async Task GetAllChannels()
        {
            int channelcount = 10;
            await CreateChannels("userq", channelcount);

            var adminuser = _cluster.GrainFactory.GetGrain<IUser>(Guid.NewGuid());
            await adminuser.Login(admin, admin);

            var result = await adminuser.GetAllChannels();

            Assert.True(result.Info.Length > 1);
        }

        [Fact]
        public async Task GetMembers()
        {
            int channelcount = 1;
            var channel = await JoinChannels("userr", "users", channelcount);

            var adminuser = _cluster.GrainFactory.GetGrain<IUser>(Guid.NewGuid());
            await adminuser.Login(admin, admin);

            var result = await adminuser.GetChannelMembers(channel.Name);

            Assert.True(result.Info.Length > 0);
        }

        [Fact]
        public async Task DeleteChannel()
        {
            int channelcount = 1;
            var channelinfo = await JoinChannels("usert", "useru", channelcount);

            var adminuser = _cluster.GrainFactory.GetGrain<IUser>(Guid.NewGuid());
            await adminuser.Login(admin, admin);

            var members = await adminuser.GetChannelMembers(channelinfo.Name);

            var oldmembercount = members.Info.Length;

            await adminuser.RemoveChannel(channelinfo.Name);

            var newmembers = await adminuser.GetChannelMembers(channelinfo.Name);

            //should be failed since the channel is not in the registry

            Assert.True(oldmembercount > 0 && newmembers.Success == false);
        }
    }
}