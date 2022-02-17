using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroAutomation.Swagger.Models;

public class AuthOption
{
    public Uri AuthorizationUrl { get; set; }
    public Uri TokenUrl { get; set; }
    public Dictionary<string, string> Scopes { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
}