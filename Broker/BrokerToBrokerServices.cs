using CommonTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Broker
{
    class BrokerToBrokerServices : MarshalByRefObject, BrokerToBrokerInterface  {
        List<BrokerToBrokerInterface> dad;
        List<BrokerToBrokerInterface> sons;
        List<Event> events = new List<Event>();
        Dictionary<string, List<SubscriberInterface>> subscribersByTopic;
        string routing;

        public BrokerToBrokerServices(List<BrokerToBrokerInterface> dad, List<BrokerToBrokerInterface> sons, 
            Dictionary<string, List<SubscriberInterface>> subscribersByTopic, string routing) {
            this.dad = dad;
            this.sons = sons;
            this.subscribersByTopic = subscribersByTopic;
            this.routing = routing;
        }

        public void forwardEvent(Event evt) {
            
            bool exists = false;
            foreach (Event e in events) if (evt == e) exists = true;
            if (!exists)
            {
                //Console.WriteLine(evt.Topic + ":" + evt.Content);
                events.Add(evt);

                if (routing.Equals("flooding"))
                {
                    foreach (BrokerToBrokerInterface d in dad) d.forwardEvent(evt);
                    foreach (BrokerToBrokerInterface son in sons)
                    {
                        son.forwardEvent(evt);
                    }
                    //var flattenList = subscribersByTopic.SelectMany(x => x.Value);
                    List<SubscriberInterface> flattenList;
                    if(subscribersByTopic.TryGetValue(evt.Topic, out flattenList))
                        foreach (SubscriberInterface sub in flattenList)
                        {
                            sub.deliverToSub(evt);
                        }
                }
                else
                {
                    //TODO
                }
            }
        }
    }
}
