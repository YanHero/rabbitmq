# 七、RabbitMQ之远程过程调用

标签（空格分隔）： RabbitMQ学习笔记

------

## 1、简介

@import "python-six.png"

> 1、当客户端启动时，它创建一个匿名独占回调队列；
> 2、对于RPC请求，客户端发送一个具有两个属性的消息：replyTo，它被设置为回调队列和correlationId，它被设置为每个请求的唯一值；
> 3、请求被发送到rpc_queue队列；
> 4、RPC worker（aka：server）正在等待队列上的请求。 当请求出现时，它将执行该作业，并使用replyTo字段中的队列将结果发送回客户端；
> 5、客户端等待回呼队列中的数据。 当信息出现时，它检查correlationId属性。如果它与请求中的值相匹配，则返回对应用程序的响应。

## 2、服务端

```

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

```

## 3、客户端

```

using System;
using System.Collections.Concurrent;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace heroyan.rabbitmq.rpc.client
{
    class Program
    {
        static void Main(string[] args)
        {
            var rpcClient = new RpcClient();

            Console.WriteLine("[x] Requesting fib(30)");
            var response = rpcClient.Call("30");

            Console.WriteLine($"[.] Got '{response}'");
            rpcClient.Close();
        }
    }


    class RpcClient
    {
        private readonly IConnection connection;
        private readonly IModel channel;
        private readonly string replyQueueName;
        private readonly EventingBasicConsumer consumer;
        private readonly BlockingCollection<string> respQueue = new BlockingCollection<string>();
        private readonly IBasicProperties props;

        /// <summary>
        /// 空参构造
        /// </summary>
        public RpcClient()
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost",
                Port = 5672
            };

            connection = factory.CreateConnection();
            channel = connection.CreateModel();
            replyQueueName = channel.QueueDeclare().QueueName;
            consumer = new EventingBasicConsumer(channel);

            props = channel.CreateBasicProperties();
            var correlationId = Guid.NewGuid().ToString();
            props.CorrelationId = correlationId;
            props.ReplyTo = replyQueueName;

            consumer.Received += (model, ea) =>
            {
                var body = ea.Body;
                var response = Encoding.UTF8.GetString(body);
                if (ea.BasicProperties.CorrelationId == correlationId)
                {
                    respQueue.Add(response);
                }
            };
        }

        /// <summary>
        /// 调用
        /// </summary>
        /// <returns>The call.</returns>
        /// <param name="message">Message.</param>
        public string Call(string message)
        {
            var messageBytes = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(exchange: "", routingKey: "rpc_queue", basicProperties: props, body: messageBytes);
            channel.BasicConsume(consumer: consumer, queue: replyQueueName, autoAck: true);

            return respQueue.Take();
        }

        /// <summary>
        /// 关闭
        /// </summary>
        public void Close()
        {
            connection.Close();
        }
    }
}

```