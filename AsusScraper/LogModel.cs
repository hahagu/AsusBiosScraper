using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsusScraper;

public class LogModel
{
    public string Sender { get; set; }
    public string Message { get; set; }
    public string VerboseMessage { get; set; } = string.Empty;
}