# 七、RabbitMQ事务机制

标签（空格分隔）： RabbitMQ学习笔记

------

    在使用abbitMQ的时候，我们可以通过消息持久化操作来解决因为服务器的异常奔溃导致的消息丢失，我们还会遇到一个问题，当消息的发布者在将消息发送出去之后，消息到底有没有正确到达broker代理服务器呢？默认情况下我们的生产者是不知道消息有没有正确到达broker的，如果在消息到达broker之前已经丢失的话，持久化操作也解决不了这个问题，因为消息根本就没到达代理服务器。

## 1、简介 => 两种方式实现事务

> 1、方式一：通过AMQP事务机制实现，这也是从AMQP协议层面提供的解决方案，但是采用事务机制实现会降低RabbitMQ的消息吞吐量；
> 2、方式二：通过将channel设置成confirm模式来实现，更加高效的解决方式。

## 2、方式一生产者

```

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

                        if (i == 1)
                        {
                            var result = i / 0;
                        }
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

```

## 3、方式一消费者

```

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

```





## 4、生产者确认模式实现原理

> 1、生产者将信道设置成confirm模式，一旦信道进入confirm模式，所有在该信道上面发布的消息都将会被指派一个唯一的ID(从1开始)；
> 2、一旦消息被投递到所有匹配的队列之后，broker就会发送一个确认给生产者(包含消息的唯一ID)，这就使得生产者知道消息已经正确到达目的队列了；
> 3、如果消息和队列是可持久化的，那么确认消息会在将消息写入磁盘之后发出；
> 4、confirm模式最大的好处在于它是异步的，如果RabbitMQ因为自身内部错误导致消息丢失，就会发送一条nack消息，生产者同样可以在回调方法中处理该nack消息；

## 5、方式二生产者

```

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

                channel.BasicAcks += (obj, e) =>
                {
                    Console.WriteLine($"ack：deliveryTag = {e.DeliveryTag} multiple = {e.Multiple}");
                };

                channel.BasicNacks += (obj, e) =>
                {
                    Console.WriteLine($"nack：deliveryTag = {e.DeliveryTag} multiple = {e.Multiple}");
                };

                #endregion

                for (int i = 0; i < 10; i++)
                {
                    var msg = Encoding.UTF8.GetBytes($"hello{i}");
                    channel.BasicPublish(exchange: "exc_trans", routingKey: "info", basicProperties: null, body: msg);

                    #region 普通confirm模式

                    //if (channel.WaitForConfirms())
                    //{
                    //    Console.WriteLine($"hello{i} send success");
                    //}

                    #endregion
                }

                #region 批量confirm模式

                //channel.WaitForConfirmsOrDie(); // 阻塞

                Console.WriteLine("send all messages");

                #endregion
                
                Console.WriteLine("Press [enter] to exit.");
                Console.ReadKey();
            }
        }
    }
}

```

## 6、方式二消费者

```

using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace heroyan.rabbitmq.confirm.consumer
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

```