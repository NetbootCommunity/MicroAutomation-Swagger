using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroAutomation.Swagger.Models;

public class FeatureOption
{
    public bool HealthCheckEndpoint { get; set; } = true;
    public bool BearerAuthentication { get; set; } = false;
    public bool OauthAuthentication { get; set; } = true;
}