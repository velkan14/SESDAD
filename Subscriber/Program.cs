using CommonTypes;
using CommonTypesPM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;

namespace Subscriber
{
    class Program
    {
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

            BrokerSubscribeInterface brk = (BrokerSubscribeInterface)Activator.GetObject(typeof(BrokerSubscribeInterface), urlBroker + "S");
            NotificationReceiver notifier = (NotificationReceiver)Activator.GetObject(typeof(NotificationReceiver), pmURL);

            Subscriber sub = new Subscriber(brk, notifier, processName, myURL);

            PMSubscriberImpl pm = new PMSubscriberImpl(sub);
            RemotingServices.Marshal(pm, remotingName + "PM", typeof(PMSubscriber));

            SubscriberImpl subscriber = new SubscriberImpl(sub);
            RemotingServices.Marshal(subscriber, remotingName, typeof(SubscriberInterface));

            Console.WriteLine(processName + " started on " + myURL);
            Console.ReadLine();
        }
    }
}
