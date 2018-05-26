# 四、RabbitMQ之发布订阅

标签（空格分隔）： RabbitMQ学习笔记

------

## 1、简介

> 1、我们向多个【消费者】传递信息。这种模式被称为“发布/订阅”。

@import "exchanges.png"

## 2、消息交换机【Exchange】

> 1、RabbitMQ消息传递模型的核心思想是，【生产者】不直接发送任何信息到队列。事实上，【生产者】根本就不知道消息是否会被传送到任何队列；
> 2、【生产者】只能发送消息到【消息交换机】，一方面它接收来自【生产者】的消息，另一方面是将接收到消息推送到队列中；
> 3、【消息交换机】的类型：【Direct】直接，【Topic】主题，【Headers】标题和【Fanout】扇出；
> 4、【Fanout】类型的【消息交换机】非常简单。它只是传播它收到的所有消息去它知道所有的队列中；【Fanout】类型的【消息交换机】会容忽视routingKey的值的；
> 5、默认的消息交换机：之前我们使用了默认的【消息交换机】，这些默认的消息 交换机我用使用两个双引号""来标识。
> 6、空字符串表示默认或未命名的消息交换机：消息会被路由到指定的routingkey名称的队列，如果它存在的话。

## 3、临时队列

> 1、我们可以创建一个具有随机名称的队列，或者，甚至更好一点-让服务器为我们选择一个随机队列名称；
> 2、其次，一旦我们断开与【消费者】的队列就应该自动删除该队列。

## 4、绑定【Binding】

> 1、消息交换机】和【队列】之间的关系称为绑定。

@import "bindings.png"

## 5、生产者

```

using System;
using System.Text;
using RabbitMQ.Client;

namespace heroyan.rabbitmq.pubsub.producer
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

```

## 6、消费者

```

using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace heroyan.rabbitmq.pubsub.consumer
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

                var queueName = channel.QueueDeclare().QueueName;
                channel.QueueBind(queue: queueName, exchange: "logs", routingKey: "");

                Console.WriteLine("[*] Waiting for logs.");

                var consumer = new EventingBasicConsumer(channel);

                consumer.Received += (obj, e) =>
                {
                    var body = e.Body;
                    var message = Encoding.UTF8.GetString(body);
                    Console.WriteLine("[x] Received {0}", message);
                };

                channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);

                Console.WriteLine("Press [enter] to exit.");
                Console.ReadKey();
            }
        }
    }
}

```