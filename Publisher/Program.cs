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
            string myURL = args[1];
            string urlBroker = args[2];
            string pmURL = args[3];
            string port = myURL.Split(':')[2].Split('/')[0];
            string remotingName = myURL.Split('/')[3];

            TcpChannel channel = new TcpChannel(Int32.Parse(port));
            ChannelServices.RegisterChannel(channel, false);

            BrokerPublishInterface broker = (BrokerPublishInterface)Activator.GetObject(typeof(BrokerPublishInterface), urlBroker + "P");
            Console.WriteLine(pmURL);
            NotificationReceiver pm = (NotificationReceiver)Activator.GetObject(typeof(NotificationReceiver), pmURL);

            Publisher pub = new Publisher(broker, pm, processName);

            PublisherImpl pi = new PublisherImpl(pub);
            RemotingServices.Marshal(pi, remotingName + "P", typeof(PMPublisher));

            PMPublisherImpl publisher = new PMPublisherImpl(pub);
            RemotingServices.Marshal(publisher, remotingName + "PM", typeof(PMPublisher));
            
            Console.WriteLine(processName + " started on " + myURL);
            Console.ReadLine();
        }
    }
}
