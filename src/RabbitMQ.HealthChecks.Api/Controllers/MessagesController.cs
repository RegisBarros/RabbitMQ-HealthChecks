using System;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using RabbitMQ.HealthChecks.Api.Configurations;
using RabbitMQ.HealthChecks.Api.Models;

namespace RabbitMQ.HealthChecks.Api.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private static Counter _counter = new Counter();

        [HttpGet]
        public object Get()
        {
            return new
            {
                Messages = _counter.Value
            };
        }

        [HttpPost]
        public object Post([FromServices] RabbitMQConfigurations configuration, [FromBody] Content content)
        {
            lock (_counter)
            {
                _counter.Increment();

                var factory = new ConnectionFactory()
                {
                    HostName = configuration.HostName,
                    Port = configuration.Port,
                    UserName = configuration.UserName,
                    Password = configuration.Password
                };

                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: "tests-queue",
                                         durable: false,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);

                    string message =
                        $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")} - " +
                        $"Message: {content.Message}";

                    var body = Encoding.UTF8.GetBytes(message);

                    channel.BasicPublish(exchange: "",
                                         routingKey: "tests-queue",
                                         basicProperties: null,
                                         body: body);
                }
            }

            return new
            {
                Result = "Message Sent"
            };
        }
    }
}