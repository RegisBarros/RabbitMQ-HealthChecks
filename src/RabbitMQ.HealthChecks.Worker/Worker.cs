using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.HealthChecks.Worker.Configurations;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace RabbitMQ.HealthChecks.Worker
{
    public class Worker : BackgroundService
    {
        private const string QueueName = "tests-queue";
        private readonly ILogger<Worker> _logger;
        private readonly RabbitMQConfigurations _rabbitMQConfiguration;   
        private static readonly AutoResetEvent _waitHandle =
            new AutoResetEvent(false);

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            var config = configuration.GetSection("RabbitMQConfigurations");

            _rabbitMQConfiguration = new RabbitMQConfigurations();
            new ConfigureFromConfigurationOptions<RabbitMQConfigurations>(config)
                .Configure(_rabbitMQConfiguration);
            
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory()
            {
                HostName = _rabbitMQConfiguration.HostName,
                Port = _rabbitMQConfiguration.Port,
                UserName = _rabbitMQConfiguration.UserName,
                Password = _rabbitMQConfiguration.Password
            };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel()) 
            {
                channel.QueueDeclare(queue: QueueName,
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);


                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += Consumer_Received;

                channel.BasicConsume(queue: QueueName,
                     autoAck: true,
                     consumer: consumer);

                Console.WriteLine("Waiting for messages...");

                Console.CancelKeyPress += (o, e) =>
                {
                    Console.WriteLine("Exiting...");

                    _waitHandle.Set();
                    e.Cancel = true;
                };
                
                _waitHandle.WaitOne();
            }
        }

        private static void Consumer_Received(
            object sender, BasicDeliverEventArgs e)
        {
            var message = Encoding.UTF8.GetString(e.Body.ToArray());
            Console.WriteLine(Environment.NewLine +
                "[Nova mensagem recebida] " + message);
        }
    }
}
