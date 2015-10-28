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
            //tcpchannel dadURL routingPolicy processURL
           
            // TcpChannel channel = new TcpChannel(Int32.Parse(args[0]));
            TcpChannel channel = new TcpChannel(8086);
            ChannelServices.RegisterChannel(channel, false);
            BrokerServices brk = new BrokerServices();
            RemotingServices.Marshal(brk,
                "SonBroker",
                typeof(BrokerServices));
              
            Console.WriteLine("New broker listening at tcp://localhost:8086/Broker");  
            System.Console.WriteLine("Press <enter> to terminate Broker...");
            System.Console.ReadLine();
        }   
    }

    public class BrokerServices : MarshalByRefObject, BrokerInterface
    {
        string processName;
        BrokerServices dad;
        List<BrokerInterface> sons;
        List<SubscriberInterface> subs; 
        Dictionary<string, List<SubscriberInterface>> subscribersByTopic;
        bool flooding;

        public BrokerServices(/*string dadURL, string routingPolicy*/){
       //     if (!dadURL.Equals("none")) {
        //        dad = (BrokerServices)Activator.GetObject(
                       //    typeof(BrokerServices), dadURL);
        //    }
       //     if (routingPolicy.Equals("flooding"))
       //         flooding = true;
        //    else
         //       flooding = false;
        }

        public void subscribe(string topic, string subscriberURL) {
            SubscriberInterface newSubscriber =
                (SubscriberInterface)Activator.GetObject(
                       typeof(SubscriberInterface), subscriberURL);

            if (subscribersByTopic.ContainsKey(topic)) 
                subscribersByTopic[topic].Add(newSubscriber);
            else 
                subscribersByTopic.Add(topic, new List<SubscriberInterface> { newSubscriber });
            
        }
       
        public void unsubscribe(string topic)
        {
            throw new NotImplementedException();
        }

        public void publish(Event newEvent) {
            if (flooding) {
                if (dad != null) { dad.publish(newEvent); }
                foreach(BrokerInterface son in sons) { son.publish(newEvent); }
                foreach (SubscriberInterface sub in subs) { sub.deliverToSub(newEvent); }
            }
            else {
            //TODO
            }



        }



    }

}
