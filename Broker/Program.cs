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
    class Program
    {
       
        static void Main(string[] args)
        {
            string processName = args[0];
            string url = args[1];
            string routing = args[2];
            string ordering = args[3];
            string loggingLevel = args[4];
            string pmURL = args[5];
            string leaderCount = args[6];
            string port = url.Split(':')[2].Split('/')[0];
            string remotingName = url.Split('/')[3];
            Console.WriteLine("Name: "+ processName+"; Url: " +url+ "; \n\rRouting: " +routing+"; Ordering: " + ordering);

            TcpChannel channel = new TcpChannel(Int32.Parse(port));
            ChannelServices.RegisterChannel(channel, false);

            NotificationReceiver pm = (NotificationReceiver)Activator.GetObject(typeof(NotificationReceiver), pmURL);

            Broker broker = new Broker(pm, processName, routing, ordering, loggingLevel, Convert.ToInt32(leaderCount));
            broker.setUrl(url);


            PMBrokerImpl PMbroker = new PMBrokerImpl(broker);
            RemotingServices.Marshal(PMbroker, remotingName + "PM", typeof(PMBroker));

            BrokerSubscribeServices brkSubscribe = new BrokerSubscribeServices(broker);
            RemotingServices.Marshal(brkSubscribe, remotingName + "S", typeof(BrokerSubscribeServices));

            BrokerToBrokerServices brkToBrk = new BrokerToBrokerServices(broker);
            RemotingServices.Marshal(brkToBrk, remotingName + "B", typeof(BrokerToBrokerServices));

            BrokerPublishServices brkPublish = new BrokerPublishServices(broker);
            RemotingServices.Marshal(brkPublish, remotingName +"P", typeof(BrokerPublishServices));

            Console.WriteLine("New broker listening at " + url);
            System.Console.WriteLine("Press <enter> to terminate Broker...");
            System.Console.ReadLine();
        }   
    }

    

}
