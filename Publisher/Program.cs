using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using CommonTypes;
using CommonTypesPM;

namespace Publisher
{
    class Program
    {
        static void Main(string[] args)
        {
            
            string portBroker = "";
            TcpChannel channel = new TcpChannel();
            ChannelServices.RegisterChannel(channel, false);
            PublisherInterface broker = (PublisherInterface)Activator.GetObject(typeof(PublisherInterface), "tcp://localhost:" + portBroker + "/PublisherInterface");
            PMPublisherImpl publisher = new PMPublisherImpl(broker, "processName");
            RemotingServices.Marshal(publisher, "PMPublisher", typeof(PMPublisher));
            Console.ReadLine();
        }
    }
}
