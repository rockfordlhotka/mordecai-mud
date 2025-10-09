using System.Reflection;
using RabbitMQ.Client;

var connType = typeof(IConnection);
Console.WriteLine($"IConnection: {connType.FullName}");
foreach(var m in connType.GetMethods())
{
    Console.WriteLine(m);
}

var channelType = typeof(IChannel);
Console.WriteLine($"IChannel: {channelType.FullName}");
foreach(var m in channelType.GetMethods())
{
    Console.WriteLine(m);
}
