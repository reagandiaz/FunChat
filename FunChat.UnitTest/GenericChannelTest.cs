using FunChat.GrainIntefaces;
using FunChat.UnitTest.Tools;
using Orleans.TestingHost;
using System;
using System.Threading.Tasks;
using Xunit;


namespace FunChat.UnitTest
{
    [Collection(ClusterCollection.Name)]
    public class GenericChannelTest
    {
        private readonly TestCluster _cluster;

        public GenericChannelTest(ClusterFixture fixture)
        {
            _cluster = fixture.Cluster;
        }


        IUser user;
        IChannel channel;

        internal async Task<Guid> Login(string username)
        {
            user = _cluster.GrainFactory.GetGrain<IUser>(Guid.NewGuid());
            return await user.Login(username, username);
        }

        internal async Task AssignChannel(string channelname)
        {
            var channelguid = await user.LocateChannel(channelname);
            channel = _cluster.GrainFactory.GetGrain<IChannel>(channelguid);
        }

        internal async Task SimulateSending(string channelname, string username, int messagecount)
        {
            var userguid = await Login(username);
            await AssignChannel(channelname);
            for (int i = 0; i < messagecount; i++)
                await channel.Message(new UserInfo() { Name = username, Key = userguid }, new Message(i.ToString()));
        }

        internal async Task<int> SimulateRead()
        {
            var messages = await channel.ReadHistory(100);
            return messages.Length;
        }


        //I can send and receive messages in the generical channel (default channel). 
        //I can see the channel’s chat history(the last 100 messages)

        const string generic = "generic";

        [Fact]
        public async Task SendAndGetMessageFromGenericChannel()
        {
            int amessages = 5;
            int bmessages = 5;
            await SimulateSending(generic, "usera", amessages);
            await SimulateSending(generic, "userb", bmessages);
            int count = await SimulateRead();
            Assert.Equal(amessages + bmessages, count);
        }

        //I can see the channel members.
        [Fact]
        public async Task GenericChannelCheckMembers()
        {
            var newuser = "userc";
            await Login(newuser);
            await AssignChannel(generic);
            var members = await user.GetChannelMembers(generic);
            Assert.True(members.Length >= 1);
        }
    }
}

