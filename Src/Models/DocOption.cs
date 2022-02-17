using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroAutomation.Swagger.Models;

internal class DocOption
{
    public string Version { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public ContactOption Contact { get; set; }

    public DocOption()
    {
        Contact = new ContactOption();
    }
}