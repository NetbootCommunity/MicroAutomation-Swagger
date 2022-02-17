using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroAutomation.Swagger.Models;

internal class SwaggerOption
{
    public string BasePath { get; set; }
    public string RoutePrefix { get; set; }
    public AuthOption Authentication { get; set; }
    public List<DocOption> Documents { get; set; }
    public FeatureOption Features { get; set; }

    public SwaggerOption()
    {
        BasePath = "/";
        RoutePrefix = "docs";
        Authentication = new AuthOption();
        Documents = new List<DocOption>();
        Features = new FeatureOption();
    }
}