using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySerialApp
{
    public class LogItem
    {
        public DateTime Timestamp { get; set; }
        public string TX { get; set; }
        public string RX { get; set; }
    }
}
