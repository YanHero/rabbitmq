using System;
using System.Text;
using RabbitMQ.Client;

namespace heroyan.rabbitmq.workqueue.producer
{
    class Program
    {
        static void Main(string[] args)
        {
            var factory = new ConnectionFactory
            {
                HostName = "localhost",
                Port = 5672
            };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: "logs", type: "fanout");

                var message = GetMessage(args);
                var body = Encoding.UTF8.GetBytes(message);
                channel.BasicPublish(exchange: "logs", routingKey: "", basicProperties: null, body: body);

                Console.WriteLine($"[x] Sent {message}");             
            }

            Console.WriteLine("Press [enter] to exit.");
            Console.ReadKey();
        }

        /// <summary>
        /// Gets the message.
        /// </summary>
        /// <returns>The message.</returns>
        /// <param name="args">Arguments.</param>
        private static string GetMessage(string[] args)
        {
            return args.Length > 0 ? string.Join(" ", args) : "info：Hello World!";
        }
    }
}