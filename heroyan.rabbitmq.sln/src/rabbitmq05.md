# 五、RabbitMQ之路由

标签（空格分隔）： RabbitMQ学习笔记

------

## 1、简介

> 1、上一节我们能够向许多消息接受者广播发送日志消息，现在我们将为其添加一项功能，就是我们只将订阅消息的一个子集。

## 2、绑定

> 1、【绑定】是【消息交换机】和【队列】之间的关系纽带，通过绑定把二者关联起来；
> 2、【绑定】可以占用额外的路由选择参数。 为了避免与BasicPublish参数混淆，我们将其称为【绑定键】；
> 3、【绑定键】的含义取决于交换类型。之前我们使用的【fanout】类型的【消息交换机】忽略了它的值。

## 3、直接交换

> 1、【Direct】类型的【消息交换机】，直接交换路由的背后的算法其实是很简单-把消息传递到【绑定键 binding key】和消息的【路由键 routing key】完全匹配的队列中；
@import "direct-exchange.png"
> 2、【Direct】类型的【消息交换机】X与两个队列相绑定。 第一个队列与【绑定键】的值是orange相绑定的，第二个队列有两个绑定，一个【绑定键】的值是black，另一个【绑定键】的值是green；
> 3、发布到具有【路由键】为orange的【消息交换机】的消息将被路由到队列Q1。 具有black或green【路由键】的消息将转到Q2。所有其他消息将被丢弃。

## 4、多重绑定

@import "direct-exchange-multiple.png"

> 1、使用相同的【绑定键】绑定多个队列是完全合法的。具有【路由键】是black的消息将传送到Q1和Q2。

@import "python-four.png"

## 5、生产者

```
using System;
using System.Linq;
using System.Text;
using RabbitMQ.Client;

namespace heroyan.rabbitmq.route.producer
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
                channel.ExchangeDeclare(exchange: "direct_logs", type: "direct");

                var severity = args.Length > 0 ? args[0] : "info";
                var message = args.Length > 1 ? string.Join(" ", args.Skip(1).ToArray()) : "Hello World!";

                var body = Encoding.UTF8.GetBytes(message);
                channel.BasicPublish(exchange: "direct_logs", routingKey: severity, basicProperties: null, body: body);

                Console.WriteLine($"[x] Sent {severity}:{message}");
            }

            Console.WriteLine("Press [enter] to exit.");
            Console.ReadKey();
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

namespace heroyan.rabbitmq.route.consumer
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
                channel.ExchangeDeclare(exchange: "direct_logs", type: "direct");

                var queueName = channel.QueueDeclare().QueueName;

                foreach (var severity in args)
                {
                    channel.QueueBind(queue: queueName, exchange: "direct_logs", routingKey: severity);
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