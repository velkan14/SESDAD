using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTypes;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace Broker
{
    class Broker
    {
        static void Main(string[] args)
        {
            //deve receber o porto nos argumentos. Hope so...
            TcpChannel channel = new TcpChannel(8086);
            ChannelServices.RegisterChannel(channel, false);
            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(BrokerServices), "BrokerServices",
                WellKnownObjectMode.Singleton);
            System.Console.WriteLine("Press <enter> to terminate Broker...");
            System.Console.ReadLine();

        }
    }

    public class BrokerServices : MarshalByRefObject, BrokerInterface
    {
        string daddy;
        List<string> sons;
        Dictionary<string, List<SubscriverInterface>> subscribersByTopic;

        BrokerServices(){


        }


        public void subscribe(string topic, string subscriberURL) {
            Console.WriteLine("New subscriber listening at " + subscriberURL);
            SubscriverInterface newSubscriber =
                (SubscriverInterface)Activator.GetObject(
                       typeof(SubscriverInterface), subscriberURL);

            if (subscribersByTopic.ContainsKey(topic)) 
                subscribersByTopic[topic].Add(newSubscriber);
            else 
                subscribersByTopic.Add(topic, new List<SubscriverInterface> { newSubscriber });
            
        }

        public void unsubscribe(string topic)
        {
            throw new NotImplementedException();
        }
    }

}
