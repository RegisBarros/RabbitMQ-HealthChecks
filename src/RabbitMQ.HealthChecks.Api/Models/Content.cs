using System.ComponentModel.DataAnnotations;

namespace RabbitMQ.HealthChecks.Api.Models 
{
    public class Content
    {
        [Required]
        public string Message { get; set; }
    }
}