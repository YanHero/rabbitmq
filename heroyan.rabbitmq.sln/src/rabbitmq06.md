# 六、RabbitMQ之主题

标签（空格分隔）： RabbitMQ学习笔记

------

## 1、简介

> 1、【Topic】类型【消息交换机】的消息不能有任意的routing_key - 它必须是由点分隔的单词列表；
> 2、路由关键字中可以有任意多的单词，最多可达255个字节；
> 3、绑定键也必须是相同的形式。【Topic】类型的【消息交换机】背后的逻辑类似于【Direct】类型的【消息交换机】使用特定【路由键】发送的消息将被传递到与匹配的【绑定键】绑定的所有队列。但是【绑定键】有两个重要的特殊情况：* 可以替代一个单词；＃ 可以替换零个或多个单词；
@import "python-five.png"
> 3、将【路由键】设置为"quick.orange.rabbit"、"lazy.orange.elephant"的消息将传递给两个队列；
> 4、"quick.orange.fox"只会发送到第一个队列，而"lazy.brown.fox"只能发送到第二个队列；
> 5、"lazy.pink.rabbit"将被传递到第二个队列只有一次，即使它匹配两个绑定。 "quick.brown.fox"不匹配任何绑定，所以它将被丢弃；
> 6、当队列用＃【绑定键】绑定时，它将接收所有消息，而不管【路由键】，就像使用【fanout】类型的【消息交换机】；
> 7、当特殊字符*和＃不用于绑定时，【Topic】类型的【消息交换机】将表现得像一个使用【Direct】类型的【消息交换机】。

## 2、生产者

```

using System;
using System.Linq;
using System.Text;
using RabbitMQ.Client;

namespace heroyan.rabbitmq.topic.producer
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
                channel.ExchangeDeclare(exchange: "topic_logs", type: "topic");

                var routingKey = args.Length > 0 ? args[0] : "product.info";
                var message = args.Length > 1 ? string.Join(" ", args.Skip(1).ToArray()) : "Hello World!";

                var body = Encoding.UTF8.GetBytes(message);
                channel.BasicPublish(exchange: "topic_logs", routingKey: routingKey, basicProperties: null, body: body);

                Console.WriteLine($"[x] Sent {routingKey}:{message}");
            }

            Console.WriteLine("Press [enter] to exit.");
            Console.ReadKey();
        }
    }
}

```

## 3、消费者

```

using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace heroyan.rabbitmq.topic.consumer
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
                channel.ExchangeDeclare(exchange: "topic_logs", type: "topic");

                var queueName = channel.QueueDeclare().QueueName;

                foreach (var bindingKey in args)
                {
                    channel.QueueBind(queue: queueName, exchange: "topic_logs", routingKey: bindingKey);
                }

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

```