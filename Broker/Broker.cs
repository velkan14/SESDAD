﻿using CommonTypes;
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
        string url;

        List<BrokerToBrokerInterface> dad = new List<BrokerToBrokerInterface>();
        List<BrokerToBrokerInterface> sons = new List<BrokerToBrokerInterface>();
        Dictionary<string, List<SubscriberInterface>> subscribersByTopic = new Dictionary<string, List<SubscriberInterface>>();

        Dictionary<string, List<BrokerToBrokerInterface>> brokersByTopic = new Dictionary<string, List<BrokerToBrokerInterface>>();

        Dictionary<BrokerToBrokerInterface, List<string>> topicsProvidedByBroker = new Dictionary<BrokerToBrokerInterface, List<string>>();

        List<Event> events = new List<Event>();
        string routing, ordering;

        public Broker(string routing, string ordering)
        {
            this.routing = routing;
            this.ordering = ordering;
        }

        public void setUrl(string url)
        {
            this.url = url;
        }

        public string getURL() { return url; }

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
                        
                    }
                    else if (routing.Equals("filter"))
                    {
                        List<BrokerToBrokerInterface> flattenBrokerList;
                        if (brokersByTopic.TryGetValue(evt.Topic, out flattenBrokerList))
                        {
                            foreach (BrokerToBrokerInterface broker in flattenBrokerList)
                            {
                                broker.forwardEvent(evt);
                            }
                        }
                    }

                    List<SubscriberInterface> flattenList;
                    if (subscribersByTopic.TryGetValue(evt.Topic, out flattenList))
                    {
                        foreach (SubscriberInterface sub in flattenList)
                        {
                            sub.deliverToSub(evt);
                        }
                    }
                }
            }
        }

        public void addDad(string url)
        {
            lock (this)
            {
                BrokerToBrokerInterface brokerDad = (BrokerToBrokerInterface)Activator.GetObject(typeof(BrokerToBrokerInterface), url + "B");
                dad.Add(brokerDad);
                if (routing.Equals("filter")) topicsProvidedByBroker.Add(brokerDad, null);
            }
        }

        public void addSon(string url)
        {
            lock (this)
            {
                BrokerToBrokerInterface brokerSon = (BrokerToBrokerInterface)Activator.GetObject(typeof(BrokerToBrokerInterface), url + "B");
                sons.Add(brokerSon);
                if (routing.Equals("filter")) topicsProvidedByBroker.Add(brokerSon, null);
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
            Console.WriteLine("Subscriber: " + subscriberURL + " topic: " + topic);
            
            if (routing.Equals("filter"))
            {
                foreach (KeyValuePair<BrokerToBrokerInterface, List<string>> entry in topicsProvidedByBroker)
                {
                    if (!entry.Value.Contains(topic))
                    {
                        entry.Key.forwardInterest(this.url, topic);
                        Console.WriteLine("Forward interest to " + entry.Key.getURL() + " on topic " + topic);
                        entry.Value.Add(topic);
                    }
                    
                }
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

        public void forwardInterest(string url, string topic)
        {
            Console.WriteLine("Received forwardInterest(" + url + ", " + topic + ")");
            BrokerToBrokerInterface interestedBroker = (BrokerToBrokerInterface)Activator.GetObject(typeof(BrokerToBrokerInterface), url + "B");

            List<BrokerToBrokerInterface> brokers;
            brokersByTopic.TryGetValue(topic, out brokers);

            if (brokersByTopic.ContainsKey(topic))
            {
                Console.WriteLine("brokersByTopic.ContainsKey(" + topic + ")");
                if (!brokers.Contains(interestedBroker))
                {
                    brokersByTopic[topic].Add(interestedBroker);

                    foreach (KeyValuePair<BrokerToBrokerInterface, List<string>> entry in topicsProvidedByBroker)
                    {
                        if (!entry.Key.Equals(interestedBroker))
                        {
                            if (!entry.Value.Contains(topic))
                            {
                                entry.Key.forwardInterest(this.url, topic);
                                Console.WriteLine("Forward interest to " + entry.Key.getURL() + " on topic " + topic);
                                entry.Value.Add(topic);
                            }
                        }
                    }
                }

            } else
            {
                Console.WriteLine("!brokersByTopic.ContainsKey(" + topic + ")");
                brokersByTopic.Add(topic, new List<BrokerToBrokerInterface> { interestedBroker });
                foreach (KeyValuePair<BrokerToBrokerInterface, List<string>> entry in topicsProvidedByBroker)
                {
                    if (!entry.Key.Equals(interestedBroker))
                    {
                        if (!entry.Value.Contains(topic))
                        {
                            entry.Key.forwardInterest(this.url, topic);
                            Console.WriteLine("Forward interest to " + entry.Key.getURL() + " on topic " + topic);
                            entry.Value.Add(topic);
                        }
                    }
                }

            }
            
        }


    }
}
