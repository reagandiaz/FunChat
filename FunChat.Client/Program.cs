using FunChat.GrainIntefaces;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using System;
using System.Collections.Generic;
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

        private static async Task DoClientWork(IClusterClient client)
        {
            string input;

            Console.WriteLine("/login [user] [password]");
            Console.WriteLine("/logout");
            Console.WriteLine("/join [channel] [password]");
            Console.WriteLine("/message [channel] [message]");
            Console.WriteLine("/read [channel] [100max]");
            Console.WriteLine("/members [channel]");
            Console.WriteLine("/create [password]");
            Console.WriteLine("/leave [channel]");
            Console.WriteLine("/channels");
            Console.WriteLine("/delete [channel]");
            Console.WriteLine("/exit");


            IUser user = null;
            string username = string.Empty;
            Guid userguid;
            Dictionary<string, IChannel> channels = new Dictionary<string, IChannel>();

            bool end = false;

            do
            {
                input = Console.ReadLine();

                var parameters = input.Split(' ');

                switch (parameters[0])
                {
                    case "/login":
                        {
                            if (parameters.Length == 3)
                            {
                                user = client.GetGrain<IUser>(Guid.NewGuid());
                                if (user != null)
                                {
                                    userguid = await user.Login(parameters[1], parameters[2]);

                                    if (userguid != Guid.Empty)
                                    {
                                        Console.WriteLine($"login success: {userguid}");
                                        username = parameters[1];
                                    }
                                    else
                                        Console.WriteLine("login failed!");
                                }
                            }
                            else
                                Console.WriteLine("Invalid Parameters");
                        }
                        break;
                    case "/logout":
                        {
                            if (parameters.Length == 1)
                            {
                                if (user != null)
                                {
                                    Console.WriteLine($"logout Success: {username}");
                                    await user.Logout();
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
                                if (user != null)
                                {
                                    ChannelInfo channel;

                                    if (parameters[1] == "generic")
                                        channel = await user.JoinChannel(parameters[1], string.Empty);
                                    else
                                        channel = await user.JoinChannel(parameters[1], parameters[2]);

                                    if (channel.Name == parameters[1])
                                    {
                                        Console.WriteLine($"join success: {channel.Name}");
                                        channels.Add(channel.Name, client.GetGrain<IChannel>(channel.Key));
                                    }
                                    else
                                        Console.WriteLine($"join failed: {parameters[1]}");
                                }
                                else
                                    Console.WriteLine("user null: please login");
                            }
                            else
                                Console.WriteLine("Invalid Parameters");

                        }
                        break;
                    case "/message":
                        {
                            if (parameters.Length >= 3)
                            {
                                channels.TryGetValue(parameters[1], out IChannel ichannel);
                                if (ichannel != null)
                                {

                                    var offset = parameters[0].Length + parameters[1].Length + 2;

                                    var message = input.Substring(offset, input.Length - offset);

                                    bool success = await ichannel.Message(new Message(username, message));
                                    if (success)
                                        Console.WriteLine($"message success:{message}");
                                    else
                                        Console.WriteLine($"message failed:{message}");
                                }
                            }
                            else
                                Console.WriteLine("Invalid Parameters");

                        }
                        break;
                    case "/read":
                        {
                            if (parameters.Length == 3 && int.TryParse(parameters[2], out int result))
                            {
                                channels.TryGetValue(parameters[1], out IChannel ichannel);
                                if (ichannel != null)
                                {
                                    var messages = await ichannel.ReadHistory(result);
                                    Console.WriteLine($"read count:{messages.Length}");
                                    for (int i = 0; i < messages.Length; i++)
                                        Console.WriteLine($"read[{i}]:{messages[i].Author} : {messages[i].Text}");
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
                                var guid = await user.LocateChannel(parameters[1]);
                                if (guid != Guid.Empty)
                                {
                                    var channel = client.GetGrain<IChannel>(guid);
                                    var members = await channel.GetMembers();
                                    Console.WriteLine($"members count:{members.Length}");
                                    for (int i = 0; i < members.Length; i++)
                                        Console.WriteLine($"members[{i}]:{members[i]}");
                                }
                                else
                                    Console.WriteLine($"members failed: channel {parameters[1]} not found");
                            }
                            else
                                Console.WriteLine("Invalid Parameters");
                        }
                        break;
                    case "/create":
                        {
                            if (parameters.Length == 2)
                            {
                                var channelinfo = await user.CreateChannel(parameters[1]);
                                if (channelinfo.Key != Guid.Empty)
                                    Console.WriteLine($"create success: channel:{channelinfo.Name} password:{parameters[1]}");
                                else
                                    Console.WriteLine($"create failed: {parameters[1]}");
                            }
                            else
                                Console.WriteLine("Invalid Parameters");
                        }
                        break;
                    case "/leave":
                        {
                            if (parameters.Length == 2)
                            {
                                if (user != null)
                                {
                                    var channelinfo = await user.LeaveChannelByName(parameters[1]);
                                    if (channelinfo.Key != Guid.Empty)
                                    {
                                        Console.WriteLine($"leave success: {parameters[1]}");
                                        channels.Remove(parameters[1]);
                                    }
                                    else
                                        Console.WriteLine($"leave failed: {parameters[1]}");
                                }
                                else
                                    Console.WriteLine("user null: please login");
                            }
                            else
                                Console.WriteLine("Invalid Parameters");
                        }
                        break;
                    case "/channels":
                        {
                            if (parameters.Length == 1)
                            {
                                if (user != null)
                                {
                                    var channelinfos = await user.GetAllChannels();
                                    Console.WriteLine($"channels count:{channelinfos.Length}");
                                    for (int i = 0; i < channelinfos.Length; i++)
                                        Console.WriteLine($"channels[{i}]:{channelinfos[i].Name}");
                                }
                                else
                                    Console.WriteLine("user null: please login");
                            }
                            else
                                Console.WriteLine("Invalid Parameters");
                        }
                        break;

                    case "/delete":
                        {
                            if (parameters.Length == 2)
                            {
                                if (user != null)
                                {
                                    var guid = await user.RemoveChannel(parameters[1]);
                                    if (guid != Guid.Empty)
                                    {
                                        channels.Remove(parameters[1]);
                                        Console.WriteLine($"delete success: {parameters[1]}");
                                    }
                                    else
                                        Console.WriteLine($"delete failed: {parameters[1]}");

                                }
                                else
                                    Console.WriteLine("user null: please login");
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