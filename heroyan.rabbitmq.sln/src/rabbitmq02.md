# 二、RabbitMQ之简单队列

标签（空格分隔）： RabbitMQ学习笔记

------

## 1、简介

> 1、生产：发送消息的程序是一个生产者，**P** 代表一个消息的生产者；  
> 2、队列：这里的队列是指一个名称，但是名称所代表的队列实体寄存在RabbitMQ服务器端中。虽然消息流过RabbitMQ和您的应用程序，但它们只能存储在队列中。 队列只受主机的内存和磁盘的限制，它本质上是一个大的消息缓冲区。 许多生产者可以发送消息到一个队列，许多消费者可以尝试从一个队列接收数据；
> 3、消费：消费者是一个主要等待接收消息的程序，**C** 代表一个消息的消费者。

@import "python-one.png"

## 2、(使用 Net/C# 客户端)

> 1、我们将用C#写两个程序;一个是生产者，用于发送一个简单消息；一个是消费者，用于接收消息和把他们打印出来；
> 2、"P"是我们的消息生产者，"C"是我们消息的消费者。在中间的红色矩形框代表一个队列，也就是消息的缓冲区，RabbitMQ代表是消息的消费者。

## 3、生产者

```

using System;
using System.Text;
using RabbitMQ.Client;

namespace heroyan.rabbitmq.simplequeue.producer
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
                channel.QueueDeclare(queue: "hello", durable: false, exclusive: false, autoDelete: false, arguments: null);

                var msg = "Hello World";
                var body = Encoding.UTF8.GetBytes(msg);

                channel.BasicPublish(exchange: "", routingKey: "hello", basicProperties: null, body: body);

                Console.WriteLine($"[x] Sent {msg}");
            }           

            Console.WriteLine("Press [enter] to exit.");
            Console.ReadKey();
        }
    }
}

```

## 4、消费者

```

using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace heroyan.rabbitmq.simplequeue.consumer
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
                channel.QueueDeclare(queue: "hello", durable: false, exclusive: false, autoDelete: false, arguments: null);

                var consumer = new EventingBasicConsumer(channel);

                consumer.Received += (obj, e) =>
                {
                    var body = e.Body;
                    var msg = Encoding.UTF8.GetString(body);
                    Console.WriteLine("[x] Received {0}", msg);
                };

                channel.BasicConsume(queue: "hello", autoAck: true, consumer: consumer);

                Console.WriteLine("Press [enter] to exit.");
                Console.ReadKey();
            }            
        }
    }
}

```