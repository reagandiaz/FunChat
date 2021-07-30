using FunChat.GrainIntefaces;
using FunChat.UnitTest.Tools;
using Orleans.TestingHost;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;


namespace FunChat.UnitTest
{
    [Collection(ClusterCollection.Name)]
    public class UserTest
    {
        private readonly TestCluster _cluster;

        public UserTest(ClusterFixture fixture)
        {
            _cluster = fixture.Cluster;
        }


        //I can login to FunChat using any(and only) alphanumeric characters as login name, the password is the same as the user’s login name.

        [Fact]
        public async Task LoginTwoChar()
        {
            var expected = Guid.NewGuid();
            var user = _cluster.GrainFactory.GetGrain<IUser>(expected);

            Guid actual = Guid.Empty;

            if (user != null)
                actual = await user.Login("ab", "ab");

            Assert.NotEqual(expected, actual);
        }

        [Fact]
        public async Task LoginFIRST()
        {
            var expected = Guid.NewGuid();
            var user = _cluster.GrainFactory.GetGrain<IUser>(expected);

            Guid actual = Guid.Empty;

            if (user != null)
                actual = await user.Login("abc", "abc");

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task LoginNEXT()
        {
            var expected = Guid.NewGuid();
            var user = _cluster.GrainFactory.GetGrain<IUser>(expected);

            Guid actual = Guid.Empty;

            if (user != null)
                actual = await user.Login("abc", "abc");

            Assert.NotEqual(expected, actual);
        }

        [Fact]
        public async Task Logout()
        {
            var user = _cluster.GrainFactory.GetGrain<IUser>(Guid.NewGuid());

            var oldguid = await user.Login("abcde", "abcde");

            await user.Logout();

            user = _cluster.GrainFactory.GetGrain<IUser>(Guid.NewGuid());

            var newguid = await user.Login("abcde", "abcde");

            Assert.NotEqual(oldguid, newguid);
        }

        //I can send and receive messages in the generical channel (default channel). 
        //I can see the channel’s chat history(the last 100 messages)
        //I can see the channel members.
        [Fact]
        public async Task LocateGenericChannel()
        {
            var expected = Guid.NewGuid();
            var user = _cluster.GrainFactory.GetGrain<IUser>(expected);

            await user.Login("abcd", "abcd");

            var channelinfo = await user.JoinChannel("generic", string.Empty);

            var guid = await user.LocateChannel("generic");

            Assert.Equal(channelinfo.Key, guid);

            var channel = _cluster.GrainFactory.GetGrain<IChannel>(guid);

            var members = await channel.GetMembers();

            Assert.True(members.Length == 1);

            var name = await channel.Name();

            Assert.Equal("generic", name);

            bool issent = await channel.Message(new Message("abcd", "Hey"));

            Assert.True(issent);

            for (int i = 0; i < 101; i++)
            {
                await channel.Message(new Message("abcd", Convert.ToString(i)));
            }

            var messages = await channel.ReadHistory(101);

            Assert.True(messages.Length == 100);

            messages = await channel.ReadHistory(100);

            Assert.True(messages.Length == 100);

            messages = await channel.ReadHistory(99);

            Assert.True(messages.Length == 99);

            messages = await channel.ReadHistory(0);

            Assert.True(messages.Length == 0);
        }

        //I can create a private channel with a password.
        //System will generate a random unique channel name(alphanumeric) with 6 characters.
        //The password must be alphanumeric and 6  ~ 18 characters.


        [Fact]
        public async Task CreateChannel()
        {
            var user = _cluster.GrainFactory.GetGrain<IUser>(Guid.NewGuid());
            await user.Login("abcdef", "abcdef");
            var info = await user.CreateChannel("passwo");
            var channel = _cluster.GrainFactory.GetGrain<IChannel>(info.Key);
            var name = await channel.Name();
            Assert.Equal(info.Name, name);
        }

        [Fact]
        public async Task CreateChannelBadPassword()
        {
            var user = _cluster.GrainFactory.GetGrain<IUser>(Guid.NewGuid());
            await user.Login("abcdef", "abcdef");
            var info = await user.CreateChannel("passw");
            Assert.Equal(info.Name, String.Empty);
        }

        //I can join a private channel with the channel name and password.
        //Maximum join 2 channels.
        //I can left any channel other than the default channel


        [Fact]
        public async Task JoinChannelGoodPassword()
        {
            string username = "abcdefg";
            var user = _cluster.GrainFactory.GetGrain<IUser>(Guid.NewGuid());
            await user.Login(username, username);
            var channel1 = await user.CreateChannel("password1");

            var channel2 = await user.CreateChannel("password2");

            var channel3 = await user.CreateChannel("password3");

            var joinchannel1 = await user.JoinChannel(channel1.Name, "password1");

            Assert.Equal(joinchannel1.Name, channel1.Name);

            var joinchannel2 = await user.JoinChannel(channel2.Name, "password2");

            Assert.Equal(joinchannel2.Name, channel2.Name);

            var joinchannel3 = await user.JoinChannel(channel3.Name, "password3");

            //failed joining 3 since only 2 is allowed
            Assert.Equal(joinchannel3.Name, String.Empty);

            //in number 1
            var channel = _cluster.GrainFactory.GetGrain<IChannel>(channel1.Key);
            var members = await channel.GetMembers();
            Assert.True((members.ToList().Contains(username)));

            //not in number 3
            channel = _cluster.GrainFactory.GetGrain<IChannel>(channel3.Key);
            members = await channel.GetMembers();
            Assert.True(!(members.ToList().Contains(username)));

            //join generic
            var channelinfo = await user.JoinChannel("generic", string.Empty);
            channel = _cluster.GrainFactory.GetGrain<IChannel>(channelinfo.Key);
            members = await channel.GetMembers();
            Assert.True((members.ToList().Contains(username)));

            //cannot leave generic channel
            var genericguid = channelinfo.Key;
            await user.LeaveChannelByKey(channelinfo.Key);
            channel = _cluster.GrainFactory.GetGrain<IChannel>(genericguid);
            members = await channel.GetMembers();
            Assert.True((members.ToList().Contains(username)));


            //can leave other channels
            channel2 = await user.LeaveChannelByKey(channel2.Key);
            channel = _cluster.GrainFactory.GetGrain<IChannel>(channel2.Key);
            members = await channel.GetMembers();
            Assert.True(!(members.ToList().Contains(username)));
        }

        [Fact]
        public async Task JoinChannelBadPassword()
        {
            var user = _cluster.GrainFactory.GetGrain<IUser>(Guid.NewGuid());
            await user.Login("abcdefg", "abcdefg");
            var info = await user.CreateChannel("password");
            var channelInfo = await user.JoinChannel(info.Name, "passwordx");
            Assert.Equal(channelInfo.Key, Guid.Empty);
        }

        //I can list any channel’s members even it’s private
        //I can delete any channel by channel’s name, after deleting the channel, all channel members will be kicked off from the channel and no longer receive any message.


        [Fact]
        public async Task AdminFunction()
        {
            string username = "abcdefgh";
            var user = _cluster.GrainFactory.GetGrain<IUser>(Guid.NewGuid());
            await user.Login(username, username);
            var channel1 = await user.CreateChannel("password1");

            var channel2 = await user.CreateChannel("password2");

            await user.CreateChannel("password3");

            var joinchannel1 = await user.JoinChannel(channel1.Name, "password1");

            Assert.Equal(joinchannel1.Name, channel1.Name);

            await user.JoinChannel(channel2.Name, "password2");

            string adminname = "Admin";
            var admin = _cluster.GrainFactory.GetGrain<IUser>(Guid.NewGuid());
            await admin.Login(adminname, adminname);

            var channels = await admin.GetAllChannels();

            Assert.True(channels.Length > 4);

            for (int i = 0; i < channels.Length; i++)
            {
                if (channels[i].Name != "generic")
                    await admin.RemoveChannel(channels[i].Name);
            }

            channels = await admin.GetAllChannels();
            Assert.True(channels.Length == 1);

        }

    }
}

