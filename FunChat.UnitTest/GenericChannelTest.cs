using FunChat.GrainIntefaces;
using FunChat.UnitTest.Tools;
using Orleans.Streams;
using Orleans.TestingHost;
using System;
using System.Threading;
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
        ChannelInfo channelinfo;
        MessageObserver observer;

        internal async Task<Result<UserInfo>> Login(string username)
        {
            user = _cluster.GrainFactory.GetGrain<IUser>(Guid.NewGuid());
            return await user.Login(username, username);
        }

        internal async Task AssignChannel(string channelname)
        {
            var channelguid = await user.LocateChannel(channelname);
            if (channelguid.Success == true)
            {
                channel = _cluster.GrainFactory.GetGrain<IChannel>(channelguid.Info.Key);
                channelinfo = new ChannelInfo() { Key = channelguid.Info.Key, Name = channelname };
            }
        }

        internal async Task SimulateSending(string channelname, string username, int messagecount, bool observe)
        {
            var userguid = await Login(username);
            await AssignChannel(channelname);


            if (observe)
            {
                var streamprovider = _cluster.Client.GetStreamProvider("FunChat");
                var stream = streamprovider.GetStream<Message>(channelinfo.Key, channelinfo.Name);
                observer = new MessageObserver();
                await stream.SubscribeAsync(observer);
            }

            for (int i = 0; i < messagecount; i++)
                await channel.Message(new UserInfo() { Name = username, Key = userguid.Info.Key }, new Message(i.ToString()));

            Thread.Sleep(2000);

        }

        internal async Task<int> ReadFromObserver()
        {
            return await Task.FromResult(observer.GetMessages().Count);
        }

        internal async Task<int> ReadFromChannel()
        {
            var messages = await channel.ReadHistory();
            return await Task.FromResult(messages.Info.Length);
        }

        //I can send and receive messages in the generical channel (default channel). 
        //I can see the channel’s chat history(the last 100 messages)

        const string generic = "generic";

        [Fact]
        public async Task SendAndGetMessageFromGenericChannel()
        {
            int amessages = 5;
            int bmessages = 5;
            await SimulateSending(generic, "usera", amessages, true);
            await SimulateSending(generic, "userb", bmessages, false);
            int count = await ReadFromChannel();
            Assert.Equal(amessages + bmessages, count);
        }

        [Fact]
        public async Task GetHistory()
        {
            await SimulateSending(generic, "userc", 5, false);
            int count = await ReadFromChannel();
            Assert.True(count >= 5);
        }

        //I can see the channel members.
        [Fact]
        public async Task GenericChannelCheckMembers()
        {
            var newuser = "userc";
            await Login(newuser);
            await AssignChannel(generic);
            var members = await user.GetChannelMembers(generic);
            Assert.True(members.Info.Length >= 1);
        }
    }
}

