using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Stroopwaffle_Shared {
    public class SharedConfiguration {
        public static IPEndPoint MasterServerEndPoint { get; set; } = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5000);
    }
}
