using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTypes;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using CommonTypesPM;

namespace Broker
{
    class Broker
    {
       
        static void Main(string[] args)
        {
            string processName = args[0];
            string url = args[1];
            string routing = args[2];
            string ordering = args[3];
            string port = url.Split(':')[2].Split('/')[0];
            string remotingName = url.Split('/')[3];
            Console.WriteLine("Name: "+ processName+"; Url: " +url+ "; \n\rRouting: " +routing+"; Ordering: " + ordering);

            List<BrokerToBrokerInterface> dad = new List<BrokerToBrokerInterface>();
            List<BrokerToBrokerInterface> sons = new List<BrokerToBrokerInterface>();
            Dictionary<string, List<SubscriberInterface>> subscribersByTopic = new Dictionary<string, List<SubscriberInterface>>();

            TcpChannel channel = new TcpChannel(Int32.Parse(port));
            ChannelServices.RegisterChannel(channel, false);

            PMBrokerImpl PMbroker = new PMBrokerImpl(dad, sons);
            RemotingServices.Marshal(PMbroker, remotingName + "PM", typeof(PMBroker));

            BrokerSubscribeServices brkSubscribe = new BrokerSubscribeServices(subscribersByTopic);
            RemotingServices.Marshal(brkSubscribe, remotingName + "S", typeof(BrokerSubscribeServices));

            BrokerToBrokerServices brk = new BrokerToBrokerServices(dad, sons, subscribersByTopic, routing);
            RemotingServices.Marshal(brk, remotingName + "B", typeof(BrokerToBrokerServices));

            BrokerPublishServices brkPublish = new BrokerPublishServices(brk);
            RemotingServices.Marshal(brkPublish, remotingName +"P", typeof(BrokerPublishServices));

            Console.WriteLine("New broker listening at " + url);
            System.Console.WriteLine("Press <enter> to terminate Broker...");
            System.Console.ReadLine();
        }   
    }

    

}
