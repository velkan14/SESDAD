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
        Dictionary<string, List<SubscriberInterface>> subscribersByTopic;

        public BrokerSubscribeServices(Dictionary<string, List<SubscriberInterface>> subscribersByTopic)
        {
            this.subscribersByTopic = subscribersByTopic; 
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

    }
}
