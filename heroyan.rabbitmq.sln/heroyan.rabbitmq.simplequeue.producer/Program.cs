using System;
using System.Text;
using RabbitMQ.Client;

namespace heroyan.rabbitmq.simplequeue.producer
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
                channel.QueueDeclare(queue: "hello", durable: false, exclusive: false, autoDelete: false, arguments: null);

                var msg = "Hello World";
                var body = Encoding.UTF8.GetBytes(msg);

                channel.BasicPublish(exchange: "", routingKey: "hello", basicProperties: null, body: body);

                Console.WriteLine($"[x] Sent {msg}");
            }           

            Console.WriteLine("Press [enter] to exit.");
            Console.ReadKey();
        }
    }
}