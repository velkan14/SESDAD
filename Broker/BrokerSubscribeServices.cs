using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTypes;

namespace Broker
{
    public class BrokerSubscribeServices : MarshalByRefObject, BrokerSubscribeInterface
    {
        string processName;

        List<SubscriberInterface> subs;
        Dictionary<string, List<SubscriberInterface>> subscribersByTopic;
        bool flooding;
        private BrokerSubscribeInterface dad;
        private List<BrokerSubscribeInterface> sons;

        public BrokerSubscribeServices(BrokerSubscribeInterface dad, List<BrokerSubscribeInterface> sons)
        {
            this.dad = dad;
            this.sons = sons;
            //     if (!dadURL.Equals("none")) {
            //        dad = (BrokerServices)Activator.GetObject(
            //    typeof(BrokerServices), dadURL);
            //    }
            //     if (routingPolicy.Equals("flooding"))
            //         flooding = true;
            //    else
            //       flooding = false;
        }

        public void subscribe(string topic, string subscriberURL)
        {
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

       /* public void publish(Event newEvent)
        {

            Console.WriteLine(newEvent.Topic + ":" + newEvent.Content);
            if (flooding)
            {
                if (dad != null) { dad.publish(newEvent); }
                foreach (BrokerSubscribeInterface son in sons) { son.publish(newEvent); }
                foreach (SubscriberInterface sub in subs) { sub.deliverToSub(newEvent); }
            }
            else
            {
                //TODO
            }



        }*/



    }
}
