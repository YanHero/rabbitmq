using System;
using System.Text;
using RabbitMQ.Client;

namespace heroyan.rabbitmq.trans.producer
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

                channel.TxSelect(); // 开启事务

                try
                {
                    for (int i = 0; i < 10; i++)
                    {
                        var msg = Encoding.UTF8.GetBytes($"hello{i}");
                        channel.BasicPublish(exchange: "exc_trans", routingKey: "info", basicProperties: null, body: msg);                        
                    }

                    channel.TxCommit(); // 提交事务
                }
                catch
                {
                    channel.TxRollback(); // 回滚事务
                }
            }

            Console.WriteLine("Press [enter] to exit.");
            Console.ReadKey();
        }
    }
}