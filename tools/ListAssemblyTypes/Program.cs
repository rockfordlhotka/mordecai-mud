using System;
using System.Linq;
using System.Reflection;
using RabbitMQ.Client;

namespace ListAssemblyTypesTool
{
    internal static class Program
    {
        private static void Main()
        {
            var asm = typeof(ConnectionFactory).Assembly;
            Console.WriteLine($"Loaded assembly: {asm.FullName}");

            void PrintMethods(Type t)
            {
                Console.WriteLine($"\nMethods for {t.FullName}:");
                foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy))
                {
                    Console.WriteLine(m.ToString());
                }
            }

            // Print all ConnectionFactory methods
            Console.WriteLine();
            Console.WriteLine("ConnectionFactory methods:");
            foreach (var m in typeof(RabbitMQ.Client.ConnectionFactory).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy))
            {
                Console.WriteLine(m.ToString());
            }

            Console.WriteLine();
                Console.WriteLine("IChannel BasicPublish overloads:");
                foreach (var m in typeof(RabbitMQ.Client.IChannel).GetMethods().Where(mi => mi.Name.Contains("BasicPublish")))
                {
                    Console.WriteLine(m.ToString());
                }

                    Console.WriteLine();
                    Console.WriteLine("IChannelExtensions methods:");
                    foreach (var m in typeof(RabbitMQ.Client.IChannelExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static))
                    {
                        Console.WriteLine(m.ToString());
                    }

                Console.WriteLine();
                var bp = typeof(RabbitMQ.Client.BasicProperties);
                Console.WriteLine($"BasicProperties type: {bp.FullName}");
                Console.WriteLine("Implements:");
                foreach (var i in bp.GetInterfaces())
                {
                    Console.WriteLine(i.FullName);
                }

                Console.WriteLine();
                var ibp = typeof(RabbitMQ.Client.IBasicProperties);
                Console.WriteLine($"IBasicProperties type: {ibp.FullName}");
                Console.WriteLine("IBasicProperties Implements:");
                foreach (var i in ibp.GetInterfaces())
                {
                    Console.WriteLine(i.FullName);
                }

                Console.WriteLine();
                Console.WriteLine("Types with methods containing 'CreateConnection':");
                foreach (var t in asm.GetTypes())
                {
                    try
                    {
                        var methods = t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                        foreach (var mm in methods)
                        {
                            if (mm.Name.Contains("CreateConnection"))
                            {
                                Console.WriteLine($"{t.FullName} -> {mm}");
                            }
                        }
                    }
                    catch
                    {
                        // ignore types we can't reflect over
                    }
                }
        }
    }
}
