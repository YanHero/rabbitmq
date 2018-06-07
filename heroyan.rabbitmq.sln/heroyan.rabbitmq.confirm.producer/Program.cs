using System;
using System.Text;
using RabbitMQ.Client;

namespace heroyan.rabbitmq.confirm.producer
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

                channel.ConfirmSelect();

                #region 异步，设置监听器

                //channel.BasicAcks += (obj, e) =>
                //{
                //    Console.WriteLine($"ack：deliveryTag = {e.DeliveryTag} multiple = {e.Multiple}");
                //};

                //channel.BasicNacks += (obj, e) =>
                //{
                //    Console.WriteLine($"nack：deliveryTag = {e.DeliveryTag} multiple = {e.Multiple}");
                //};

                #endregion

                for (int i = 0; i < 10; i++)
                {
                    var msg = Encoding.UTF8.GetBytes($"hello{i}");
                    channel.BasicPublish(exchange: "exc_trans", routingKey: "info", basicProperties: null, body: msg);

                    #region 普通confirm模式

                    if (channel.WaitForConfirms())
                    {
                        Console.WriteLine($"hello{i} send success");
                    }

                    #endregion
                }

                #region 批量confirm模式

                //channel.WaitForConfirmsOrDie();
                //Console.WriteLine("send all messages");

                #endregion
                
                Console.WriteLine("Press [enter] to exit.");
                Console.ReadKey();
            }
        }
    }
}