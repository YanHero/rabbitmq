# 三、RabbitMQ之工作队列

标签（空格分隔）： RabbitMQ学习笔记

------

## 1、简介

> 1、工作队列：避免立刻执行耗时的工作任务，并且一直要等到它结束为止，这个队列将用于在多个工人之间分配耗时的任务。

@import "python-two.png"

## 2、(使用 Net/C# 客户端)

> 1、现在我们发送一个代表复杂任务的字符串，通过采用Thread.Sleep()功能来实现复杂和繁忙；
> 2、我们将根据字符串中的点的数量作为它的复杂性，每一个点将占一秒钟的“工作”。例如，一个假的任务描述Hello…，有三个点，我们就需要三秒。

## 3、轮询调度

> 1、使用任务队列的好处之一就是使任务可以并行化，增加系统的并行处理能力；
> 2、如果我们正在建立一个积压的工作，我们可以紧紧增加更多的Worker实例就可以完成大量工作的处理，修改和维护就很容易；
> 3、默认情况下，RabbitMQ将会发送每一条消息给序列中每一个消费者。每个消费者都会得到相同数量的信息。这种分发消息的方式叫做轮询。

## 4、消息确认

> 1、处理一个任务可能需要几秒钟。如果有一个消费者开始了一个长期的任务，并且只做了一部分就发生了异常：
> 2、目前演示的代码一旦RabbitMQ发送一个消息给客户立即从内存中移除；
> 3、如果你关掉了一个Worker，我们将失去它正在处理的信息。我们也将丢失发送给该特定员工但尚未处理的所有信息；
> 4、为了确保消息不会丢失，RabbitMQ支持消息确认机制。ACK（nowledgement）确认消息是从【消息使用者】发送回来告诉RabbitMQ结果的一种特殊消息，确认消息告诉RabbitMQ指定的接受者已经收到、处理，并且RabbitMQ你可以自由删除它。
> 5、如果一个【消费者Consumer】死亡（其通道关闭，连接被关闭，或TCP连接丢失）不会发送ACK，RabbitMQ将会知道这个消息并没有完全处理，将它重新排队。

## 5、忘记确认

> 1、 忘记调用BasicAck这是一个常见的错误。虽然这是一个简单的错误，但后果是严重的。当你你的客户端退出，消息会被重新发送，但是RabbitMQ将会使用更多的内存，因为它将无法释放任何未确认的消息。

## 6、持久化消息

> 1、如果RabbitMQ服务器停止了，我们的任务仍然会丢失的；
> 2、当RabbitMQ退出或死机会清空队列和消息，除非你告诉它即使宕机也不能丢失任何东西。要确保消息不会丢失，有两件事情我们是必需要做的：我们需要将队列和消息都标记为持久的；
> 3、QueueDeclare表示队列的声明，创建并打开队列，durable: true，队列不会丢失任何东西即使RabbitMQ重启；
> 4、我们在通过设置IBasicProperties.SetPersistent属性值为true来标记我们的消息持久化的；
> 5、 将消息标记为持久性并不能完全保证消息不会丢失。虽然该设置告诉RabbitMQ时时刻刻把保存消息到磁盘上，但是这个时间间隔还是有的，当RabbitMQ已经接受信息但并没有保存它，此时还有可能丢失。另外，RabbitMQ不会为每个消息调用fsync（2）--它可能只是保存到缓存并没有真正写入到磁盘。虽然他的持久性保证不强，但它我们简单的任务队列已经足够用了。如果您需要更强的保证，那么您可以使用Publisher Comfirms。

## 7、公平调度

> 1、轮询调度将消息均匀发送给消费者，可能会导致一个Worker忙个不停，而另一个Worker几乎没事可做；
> 2、发生这种情况是因为当有消息进入队列的时候RabbitMQ仅仅调度了消息。它根本不看【消费者】未确认消息的数量，它只是盲目的把第N个消息发送给第N个【消费者】；
> 3、为了避免上述情况的发生，我们可以使用prefetchcount = 1的设置来调用BasicQos方法；
> 4、当某个消息处理完毕，并且已经收到了消息确认之后，才可以继续发送消息给那个【Worker】。相反，它将把消息分配给给下一个不忙的【Worker】；
> 5、注意队列大小：如果所有的工人都很忙，你的队列可以填满。你要留意这一点，也许会增加更多的【Worker】，或者有其他的策略。

## 8、生产者

```

using System;
using System.Text;
using RabbitMQ.Client;

namespace heroyan.rabbitmq.workqueue.producer
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
                channel.QueueDeclare(queue: "task_queue", durable: true, exclusive: false, autoDelete: false, arguments: null); // 队列持久化                

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true; // 消息持久化

                var msg = "hello";

                for (int i = 0; i < 20; i++)
                {
                    msg += ".";

                    var body = Encoding.UTF8.GetBytes(msg);

                    channel.BasicPublish(exchange: "", routingKey: "task_queue", basicProperties: properties, body: body);

                    Console.WriteLine($"[x] Sent {msg}");
                }                
            }

            Console.WriteLine("Press [enter] to exit.");
            Console.ReadKey();
        }
    }
}

```

## 9、消费者

```

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

```