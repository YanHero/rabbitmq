# 一、RabbitMQ简介

标签（空格分隔）： RabbitMQ学习笔记

------

RabbitMQ官网：http://www.rabbitmq.com/。

## 1、RabbitMQ是什么

> 1、MQ全称为Message Queue，消息队列（MQ）是一种应用程序对应用程序的通信方法；
> 2、消息传递指的是程序之间通过在消息中发送数据进行通信，而不是通过直接调用彼此来通信，直接调用通常是用于诸如远程过程调用的技术；
> 3、排队指的是应用程序通过队列来通信，队列的使用除去了接收和发送应用程序同时执行的要求；
> 4、 RabbitMQ是使用Erlang开发的，开源的，一个在高级消息队列协议(AMQP)基础上完整的，可复用的企业消息系统；
> 5、RabbitMQ支持你能想到的大多数开发语言如Java，Ruby，Python，.Net，C/C++，Erlang等等。

## 2、RabbitMQ的优点

> 1、安装部署方便：RabbitMQ安装方便，有详细的安装文档；
> 2、可靠性：RabbitMQ 提供了多种多样的特性让你在可靠性，可用性和性能之间作出权衡，包括持久化，发送应答，发布确认等；
> 3、强大地路由功能： 所有的消息都会通过路由器转发到各个消息队列中，RabbitMQ内建了几个常用的路由器，并且可以通过路由器的组合以及自定义路由器插件来完成复杂的路由功能；
> 4、Clustering：RabbitMQ服务端可以在一个局域网中集群部署，作为一个逻辑服务器；
> 5、Federation：Federration模式可以让RabbitMQ服务器热备部署，当系统中其中一个服务器失效而无法运作时，另一个服务器即可自动接手原失效系统所执行的工作；
> 6、Completing Consumer：内置的竞争的消费者模式可以实现消费者的负载均衡；
> 7、管理工具：RabbitMQ提供了一个简单易用的管理界面，让用户可以通过浏览器监控和控制你的消息队列的方方面面；
> 8、跟踪：如果你在使用RabbitMQ过程中出现了问题，或者得到了你不期望的结果，你可以通过打开内置Tracer功能进行跟踪，当然这时性能就有所下降；
> 9、插件系统： 提供了强大地插件系统，让用户能够方便的对MQ进行扩展。上面所说到的管理工具和Federation组件都是作为一个插件发布的。你可以选择安装或者不安装；
> 10、客户端支持：RabbitMQ支持你能想到的大多数开发语言如Java，Ruby，Python，.Net，C/C++，Erlang等等；
> 11、强大的社区和商业支持：强大的社区可以帮助你快速的解决关于RabbitMQ的各种问题，如果还是不能解决你的问题，VMWare还提供商业技术支持。

## 3、安装

> 1、Erlang环境：http://www.erlang.org/downloads
> 2、设置Erlang环境变量：ERLANG_HOME=C:\Program Files\erl9.3
> 3、RabbitMQ服务：http://www.rabbitmq.com/download.html
> 4、.net客户端类库：http://www.rabbitmq.com/dotnet.html

## 4、插件
> 1、启动web管理工具：rabbitmq-plugins enable rabbitmq_management，rabbitmq-plugins路径：C:\Program Files\RabbitMQ Server\rabbitmq_server-3.7.5\sbin；
> 2、重启服务：使用管理员打开cmd，执行 net stop RabbitMQ && net start RabbitMQ；
> 3、web管理工具的地址是：http://localhost:15672，初始用户名：guest 初始密码：guest。

## 5、RabbitMQ的一些概念

> 1、Connection：与RabbitMQ Server建立的一个连接，由ConnectionFactory创建，每个connection只与一个物理的Server进行连接，此连接是基于Socket进行连接的，这个可以相似的理解为像一个DB Connection。AMQP一般使用TCP链接来保证消息传输的可靠性；
> 2、Channel：建立在Connection基础上的一个通道，相对于Connection来说，它是轻量级的；Channel 主要进行相关定义，发送消息，获取消息，事务处理等；Channel可以在多线程中使用，但是在必须保证任何时候只有一个线程执行命令；一个Connection可以有多个Channel；客户端程序有时候会是一个多线程程序，每一个线程都想要和RabbitMQ进行连接，但是又不想共享一个连接，这种需求还是比较普遍的；因为一个Connection就是一个TCP链接，RabbitMQ在设计的时候不希望与每一个客户端保持多个TCP连接，但这确实是有些客户端的需求，所以在设计中引入了Channel的概念，每一个Channel之间没有任何联系，是完全分离的。多个Channel来共享一个Connection。
> 3、Exchange：它是发送消息的实体；
> 4、Queue：这是接收消息的实体；
> 5、Bind：将交换器和队列连接起来，并且封装消息的路由信息。

## 6、Docker部署

### 6.1、单机环境

> 1、docker pull rabbitmq:management
> 2、docker run -d --name rabbitmq -p 5671:5671 -p 5672:5672 -p 4369:4369 -p 25672:25672 -p 15671:15671 -p 15672:15672 rabbitmq:management

### 6.2、伪集群环境 => 同一台机器部署多个实例

> 1、Docker-Compose 是一个用户定义和运行多个容器的 Docker 应用程序；
> 2、编写docker-compose.yml，运行docker-compose up -d，-d后台运行；
> 3、docker-compose stop：停止；
> 4、docker exec -i -t ca9d909c529c bash：进入ID为ca9d909c529c的容器；

### 6.3、集群环境 => 多台机器部署相同实例