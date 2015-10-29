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
        /**
        * processName portPublisher urlBroker
        **/
        static void Main(string[] args)
        {

            string processName = args[0];
            int portPublisher = Int32.Parse(args[1]);
            string urlBroker = args[2];

            TcpChannel channel = new TcpChannel(portPublisher);
            ChannelServices.RegisterChannel(channel, false);
            BrokerInterface broker = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), urlBroker);
            PMPublisherImpl publisher = new PMPublisherImpl(broker, processName);
            RemotingServices.Marshal(publisher, "PMPublisher", typeof(PMPublisher));
            Console.ReadLine();
            publisher.freeze();
            publisher.publish(5, "Toy", 1000);
            Console.ReadLine();
        }
    }
}
