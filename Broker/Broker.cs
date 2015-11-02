using CommonTypes;
using CommonTypesPM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Broker
{
    class Broker :  BrokerToBrokerInterface, PMBroker, BrokerSubscribeInterface, BrokerPublishInterface
    {
        List<BrokerToBrokerInterface> dad = new List<BrokerToBrokerInterface>();
        List<BrokerToBrokerInterface> sons = new List<BrokerToBrokerInterface>();
        Dictionary<string, List<SubscriberInterface>> subscribersByTopic = new Dictionary<string, List<SubscriberInterface>>();
        List<Event> events = new List<Event>();
        string routing, ordering;

        public Broker(string routing, string ordering)
        {
            this.routing = routing;
            this.ordering = ordering;
        }

        public void forwardEvent(Event evt)
        {
            lock (this)
            {
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
                        if (subscribersByTopic.TryGetValue(evt.Topic, out flattenList))
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

        public void addDad(string url)
        {
            lock (this)
            {
                dad.Add((BrokerToBrokerInterface)Activator.GetObject(typeof(BrokerToBrokerInterface), url + "B"));
            }
        }

        public void addSon(string url)
        {
            lock (this)
            {
                sons.Add((BrokerToBrokerInterface)Activator.GetObject(typeof(BrokerToBrokerInterface), url + "B"));
            }
        }

        public void crash()
        {
            System.Environment.Exit(1);
        }

        public void freeze()
        {
            throw new NotImplementedException();
        }

        public void status()
        {
            throw new NotImplementedException();
        }

        public void unfreeze()
        {
            throw new NotImplementedException();
        }

        public void subscribe(string topic, string subscriberURL)
        {
            lock (this)
            {
                SubscriberInterface newSubscriber =
                    (SubscriberInterface)Activator.GetObject(
                           typeof(SubscriberInterface), subscriberURL);

                if (subscribersByTopic.ContainsKey(topic))
                    subscribersByTopic[topic].Add(newSubscriber);
                else
                    subscribersByTopic.Add(topic, new List<SubscriberInterface> { newSubscriber });
            }
        }

        public void unsubscribe(string topic)
        {
            throw new NotImplementedException();
        }

        public void publishEvent(Event newEvent)
        {
            forwardEvent(newEvent);
        }
    }
}
