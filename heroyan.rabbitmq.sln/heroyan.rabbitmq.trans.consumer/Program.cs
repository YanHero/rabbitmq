using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace heroyan.rabbitmq.trans.consumer
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
                channel.ExchangeDeclare(exchange: "exc_trans", type: "direct");

                var queueName = channel.QueueDeclare().QueueName;

                channel.QueueBind(queue: queueName, exchange: "exc_trans", routingKey: "info");

                Console.WriteLine("[*] Waiting for messages.");

                var consumer = new EventingBasicConsumer(channel);

                consumer.Received += (obj, e) =>
                {
                    var body = e.Body;
                    var message = Encoding.UTF8.GetString(body);
                    var routingKey = e.RoutingKey;
                    Console.WriteLine($"[x] Received {routingKey}:{message}");
                };

                channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);

                Console.WriteLine("Press [enter] to exit.");
                Console.ReadKey();
            }
        }
    }
}