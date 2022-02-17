using System;

namespace MicroAutomation.Swagger.Models
{
    public class ContactOption
    {
        public string Name { get; set; } = "Thomas ILLIET";
        public string Email { get; set; } = "contact@thomas-illiet.fr";
        public Uri Url { get; set; } = new Uri("https://www.thomas-illiet.fr/");
    }
}