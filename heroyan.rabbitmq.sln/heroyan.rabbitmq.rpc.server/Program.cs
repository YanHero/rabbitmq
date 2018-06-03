using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace heroyan.rabbitmq.rpc.server
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
                channel.QueueDeclare(queue: "rpc_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);
                channel.BasicQos(0, 1, false);

                var consumer = new EventingBasicConsumer(channel);
                channel.BasicConsume(queue: "rpc_queue", autoAck: false, consumer: consumer);

                consumer.Received += (obj, e) =>
                {
                    string response = null;
                    
                    var body = e.Body;
                    var props = e.BasicProperties;
                    var replyProps = channel.CreateBasicProperties();
                    replyProps.CorrelationId = props.CorrelationId;

                    try
                    {
                        var message = Encoding.UTF8.GetString(body);
                        int n = int.Parse(message);
                        Console.WriteLine($"[.] fib({message})");
                        response = fib(n).ToString();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(" [.] " + ex.Message);
                        response = "";
                    }
                    finally
                    {
                        var responseBytes = Encoding.UTF8.GetBytes(response);
                        channel.BasicPublish(exchange: "", routingKey: props.ReplyTo, basicProperties: replyProps, body: responseBytes);
                        channel.BasicAck(deliveryTag: e.DeliveryTag, multiple: false);
                    }
                };

                Console.WriteLine(" [x] Awaiting RPC requests");

                Console.WriteLine("Press [enter] to exit.");
                Console.ReadKey();
            }
        }

        /// <summary>
        /// 斐波那契数列
        /// </summary>
        /// <returns>The fib.</returns>
        /// <param name="n">N.</param>
        private static int fib(int n)
        {
            if (n == 0 || n == 1)
            {
                return n;
            }

            return fib(n - 1) + fib(n - 2);
        }
    }
}