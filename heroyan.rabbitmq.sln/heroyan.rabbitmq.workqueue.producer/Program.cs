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
                channel.QueueDeclare(queue: "task_queue", durable: true, exclusive: false, autoDelete: false, arguments: null); // 队列持久化                

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true; // 消息持久化

                var msg = "hello";

                for (int i = 0; i < 20; i++)
                {
                    msg += ".";

                    var body = Encoding.UTF8.GetBytes(msg);

                    channel.BasicPublish(exchange: "", routingKey: "task_queue", basicProperties: properties, body: body);

                    Console.WriteLine($"[x] Sent {msg}");
                }                
            }

            Console.WriteLine("Press [enter] to exit.");
            Console.ReadKey();
        }
    }
}
