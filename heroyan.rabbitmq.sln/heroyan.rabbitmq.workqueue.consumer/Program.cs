using System;
using System.Text;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace heroyan.rabbitmq.workqueue.consumer
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
                channel.QueueDeclare(queue: "task_queue", durable: true, exclusive: false, autoDelete: false, arguments: null); // 持久化队列

                channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false); // 公平调度

                Console.WriteLine("[*] Waiting for messages.");

                var consumer = new EventingBasicConsumer(channel);

                consumer.Received += (obj, e) =>
                {
                    var body = e.Body;
                    var msg = Encoding.UTF8.GetString(body);
                    Console.WriteLine("[x] Received {0}", msg);

                    int dots = msg.Split('.').Length - 1;
                    Thread.Sleep(dots * 1000);
                    Console.WriteLine("[x] Done");

                    channel.BasicAck(deliveryTag: e.DeliveryTag, multiple: false); // 消息确认
                };

                channel.BasicConsume(queue: "task_queue", autoAck: false, consumer: consumer);

                Console.WriteLine("Press [enter] to exit.");
                Console.ReadKey();
            }
        }
    }
}
