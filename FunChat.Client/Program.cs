using FunChat.GrainIntefaces;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Streams;
using System;
using System.Threading.Tasks;

namespace FunChat.Client
{
    public class Program
    {
        static int Main()
        {
            return RunMainAsync().Result;
        }
        private static async Task<int> RunMainAsync()
        {
            try
            {
                using (var client = await ConnectClient())
                {
                    await DoClientWork(client);
                    Console.ReadKey();
                }

                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine($"\nException while trying to run client: {e.Message}");
                Console.WriteLine("Make sure the silo the client is trying to connect to is running.");
                Console.WriteLine("\nPress any key to exit.");
                Console.ReadKey();
                return 1;
            }
        }
        private static async Task<IClusterClient> ConnectClient()
        {
            IClusterClient client;
            client = new ClientBuilder()
                .UseLocalhostClustering()
                .AddSimpleMessageStreamProvider("FunChat")
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "OrleansBasics";
                })
                .Build();

            await client.Connect();
            Console.WriteLine("Client successfully connected to silo host \n");
            return client;
        }
        public static async Task UpdateChannelSubscription(ClientState clientstate, string channelname)
        {
            var channelresult = await clientstate.User.LocateChannel(channelname);
            if (channelresult.State == ResultState.Success)
            {
                var channel = clientstate.Client.GetGrain<IChannel>(channelresult.Info.Key);
                var subscription = await Subscribe(clientstate.Client, channelresult.Info.Key, channelname);
                if (subscription != null)
                {
                    clientstate.Subscriptions.Add(channelname, subscription);
                    clientstate.Channels.Add(channelname, channel);
                }
                else
                    Console.WriteLine("failed subscription");
            }
            else
                Console.WriteLine($"can't find generic channel");
        }
        private static async Task<StreamSubscriptionHandle<Message>> Subscribe(IClusterClient client, Guid channelguid, string channelname)
        {
            StreamSubscriptionHandle<Message> handler = null;
            var stream = client.GetStreamProvider("FunChat").GetStream<Message>(channelguid, channelname);
            if (stream != null)
                handler = await stream.SubscribeAsync(new ChannelObserver(channelname));
            return handler;
        } 
        private static async Task Login(ClientState clientstate, string user, string password)
        {
            const string generic = "generic";

            await clientstate.Clear();

            clientstate.User = clientstate.Client.GetGrain<IUser>(Guid.NewGuid());

            var loginresult = await clientstate.User.Login(user, password);

            if (loginresult.State == ResultState.Success)
            {
                clientstate.Key = loginresult.Info.Key;
                Console.WriteLine($"login success: {clientstate.Key}");
                clientstate.UserName = user;

                await UpdateChannelSubscription(clientstate, generic);
                var result = await clientstate.User.CurrentChannels();
                if (result.State == ResultState.Success)
                {
                    for (int i = 0; i < result.Items.Length; i++)
                        await UpdateChannelSubscription(clientstate, result.Items[i]);
                }
            }
            else
                Console.WriteLine($"login {loginresult.State}!");
        }
        private static async Task Join(ClientState clientstate, string channelname, string password)
        {
            if (clientstate.User != null)
            {
                var channelinforesult = await clientstate.User.JoinChannel(channelname, password);
                if (channelinforesult.State == ResultState.Success)
                {
                    //remove old handle
                    if (clientstate.Channels.ContainsKey(channelinforesult.Info.Name))
                    {
                        clientstate.Channels.Remove(channelinforesult.Info.Name);
                        if (clientstate.Subscriptions.ContainsKey(channelinforesult.Info.Name))
                        {
                            await clientstate.Subscriptions[channelinforesult.Info.Name].UnsubscribeAsync();
                            clientstate.Subscriptions.Remove(channelinforesult.Info.Name);
                        }
                    }

                    clientstate.Channels.Add(channelinforesult.Info.Name, clientstate.Client.GetGrain<IChannel>(channelinforesult.Info.Key));
                    var subscription = await Subscribe(clientstate.Client, channelinforesult.Info.Key, channelinforesult.Info.Name);
                    if (subscription != null)
                    {
                        clientstate.Subscriptions.Add(channelinforesult.Info.Name, subscription);
                        Console.WriteLine($"join success: {channelinforesult.Info.Name}");
                    }
                    else
                        Console.WriteLine($"join failed subscription : {channelname}");
                }
                else
                    Console.WriteLine($"join failed: {channelname}");
            }
            else
                Console.WriteLine("user null: please login");
        }
        private static async Task Message(ClientState clientstate, string channelname, string message)
        {
            clientstate.Channels.TryGetValue(channelname, out IChannel ichannel);
            if (ichannel != null)
            {
                bool success = await ichannel.Message(new UserInfo() { Name = clientstate.UserName, Key = clientstate.Key }, new Message(message));
                if (success)
                    Console.WriteLine($"message success:{message}");
                else
                    Console.WriteLine($"message failed:{message}");
            }
            else
                Console.WriteLine($"message failed: please join {channelname}");

        }
        private static async Task Read(ClientState clientstate, string channelname)
        {
            clientstate.Channels.TryGetValue(channelname, out IChannel ichannel);
            if (ichannel != null)
            {
                var result = await ichannel.ReadHistory();
                if (result.State == ResultState.Success)
                {
                    Console.WriteLine($"read count:{result.Messages.Length}");
                    for (int i = 0; i < result.Messages.Length; i++)
                        Console.WriteLine($"read[{i}]:{result.Messages[i].Author} : {result.Messages[i].Text}");
                }
                else
                    Console.WriteLine($"read failed { channelname }");
            }
            else
                Console.WriteLine($"read failed: please join {channelname}");
        }
        private static async Task Create(ClientState clientstate, string password)
        {
            var result = await clientstate.User.CreateChannel(password);
            if (result.State == ResultState.Success)
                Console.WriteLine($"create {result.State}: channel:{result.Info.Name} password:{password}");
            else
                Console.WriteLine($"create {result.State} {password}");
        }
        private static async Task Members(ClientState clientstate, string channelname)
        {
            var members = await clientstate.User.GetChannelMembers(channelname);
            if (members.State == ResultState.Success)
            {
                Console.WriteLine($"members count:{members.Items.Length}");
                for (int i = 0; i < members.Items.Length; i++)
                    Console.WriteLine($"members[{i}]:{members.Items[i]}");
            }
            else
                Console.WriteLine($"members failed: please join {channelname}");
        }
        private static async Task Leave(ClientState clientstate, string channelname)
        {
            if (clientstate.User != null)
            {
                var result = await clientstate.User.LeaveChannelByName(channelname);
                if (result.State == ResultState.Success)
                {
                    Console.WriteLine($"leave success: {result.Info.Name}");
                    clientstate.Subscriptions.Remove(result.Info.Name);
                    clientstate.Channels.Remove(result.Info.Name);
                }
                else
                    Console.WriteLine($"leave failed: {channelname}");
            }
            else
                Console.WriteLine("user null: please login");
        }
        private static async Task Channels(ClientState clientstate)
        {
            if (clientstate.User != null)
            {
                var result = await clientstate.User.GetAllChannels();
                if (result.State == ResultState.Success)
                {
                    Console.WriteLine($"channels count:{result.Infos.Length}");
                    for (int i = 0; i < result.Infos.Length; i++)
                        Console.WriteLine($"channels[{i}]:{result.Infos[i].Name}");
                }
                else
                    Console.WriteLine("channels failed");

            }
            else
                Console.WriteLine("user null: please login");
        }
        private static async Task Delete(ClientState clientstate, string channelname)
        {
            if (clientstate.User != null)
            {
                var result = await clientstate.User.RemoveChannel(channelname);
                if (result.State == ResultState.Success)
                {
                    clientstate.Channels.Remove(result.Info.Name);
                    Console.WriteLine($"delete success: {result.Info.Name}");
                }
                else
                    Console.WriteLine($"delete failed: {channelname}");
            }
            else
                Console.WriteLine("user null: please login");
        }
        private static async Task DoClientWork(IClusterClient client)
        {
            string input;
            Console.WriteLine("please type /help");

            ClientState clientState = new ClientState() { Client = client };
            bool end = false;

            do
            {
                input = Console.ReadLine().Trim();

                var parameters = input.Split(' ');

                switch (parameters[0])
                {
                    case "/help":
                        {
                            Console.WriteLine("\n");
                            Console.WriteLine("/login [user] [password]");
                            Console.WriteLine("/join [channel] [password]");
                            Console.WriteLine("/message [channel] [message]");
                            Console.WriteLine("/read [channel]");
                            Console.WriteLine("/create [password]");
                            Console.WriteLine("/members [channel]");
                            Console.WriteLine("/leave [channel]");
                            Console.WriteLine("/channels");
                            Console.WriteLine("/delete [channel]");
                            Console.WriteLine("/exit");
                            Console.WriteLine("\n");
                        }
                        break;
                    case "/login":
                        {
                            if (parameters.Length == 3)
                            {
                                try
                                {
                                    await Login(clientState, parameters[1], parameters[2]);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                    Console.WriteLine(ex.StackTrace);
                                }
                            }
                            else
                                Console.WriteLine("Invalid Parameters");
                        }
                        break;
                    case "/join":
                        {
                            if (parameters.Length == 3)
                            {
                                try
                                {
                                    await Join(clientState, parameters[1], parameters[2]);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                    Console.WriteLine(ex.StackTrace);
                                }
                            }
                            else
                                Console.WriteLine("Invalid Parameters");
                        }
                        break;
                    case "/message":
                        {
                            if (parameters.Length >= 3)
                            {
                                var offset = parameters[0].Length + parameters[1].Length + 2;
                                var message = input[offset..];
                                try
                                {
                                    await Message(clientState, parameters[1], message);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                    Console.WriteLine(ex.StackTrace);
                                }
                            }
                            else
                                Console.WriteLine("Invalid Parameters");
                        }
                        break;

                    case "/read":
                        {
                            if (parameters.Length == 2)
                            {
                                try
                                {
                                    await Read(clientState, parameters[1]);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                    Console.WriteLine(ex.StackTrace);
                                }
                            }
                            else
                                Console.WriteLine("Invalid Parameters");
                        }
                        break;
                    case "/create":
                        {
                            if (parameters.Length == 2)
                            {
                                try
                                {
                                    await Create(clientState, parameters[1]);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                    Console.WriteLine(ex.StackTrace);
                                }
                            }
                            else
                                Console.WriteLine("Invalid Parameters");
                        }
                        break;
                    case "/members":
                        {
                            if (parameters.Length == 2)
                            {
                                try
                                {
                                    await Members(clientState, parameters[1]);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                    Console.WriteLine(ex.StackTrace);
                                }
                            }
                            else
                                Console.WriteLine("Invalid Parameters");
                        }
                        break;
                    case "/leave":
                        {
                            if (parameters.Length == 2)
                            {
                                try
                                {
                                    await Leave(clientState, parameters[1]);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                    Console.WriteLine(ex.StackTrace);
                                }
                            }
                            else
                                Console.WriteLine("Invalid Parameters");
                        }
                        break;
                    case "/channels":
                        {
                            if (parameters.Length == 1)
                            {
                                try
                                {
                                    await Channels(clientState);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                    Console.WriteLine(ex.StackTrace);
                                }
                            }
                            else
                                Console.WriteLine("Invalid Parameters");
                        }
                        break;
                    case "/delete":
                        {
                            if (parameters.Length == 2)
                            {
                                try
                                {
                                    await Delete(clientState, parameters[1]);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                    Console.WriteLine(ex.StackTrace);
                                }
                            }
                            else
                                Console.WriteLine("Invalid Parameters");
                        }
                        break;
                    case "/exit":
                        {
                            end = true;
                        }
                        break;
                    default:
                        {
                            Console.WriteLine("Invalid Command!");
                        }
                        break;
                }
            } while (!end);
        }
    }
}