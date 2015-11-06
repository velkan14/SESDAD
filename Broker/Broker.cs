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

        public bool isSubtopicOf(string subtopic, string topic)
        {
            string[] arrayTopicsSub = subtopic.Split('/');
            string[] arrayTopics = topic.Split('/');
            
            if (arrayTopicsSub.Length > arrayTopics.Length)
            {
                for (int i = 0; i < arrayTopics.Length; i++)
                {
                    if (!arrayTopicsSub[i].Equals(arrayTopics[i])) return false;
                }
                return true;
            }
            return false;
            
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
                        
                    }
                    else if (routing.Equals("filter"))
                    {

                        string keyTopic;
                        //List<SubscriberInterface> flattenSubscriberList;
                        foreach (KeyValuePair<string, List<BrokerToBrokerInterface>> entry in brokersByTopic)
                        {
                            keyTopic = entry.Key;
                            if (keyTopic.Equals(evt.Topic))
                            {
                                //subscribersByTopic.TryGetValue(evt.Topic, out flattenSubscriberList);
                                foreach (BrokerToBrokerInterface broker in entry.Value) broker.forwardEvent(evt);
                            }
                            else if (isSubtopicOf(evt.Topic, keyTopic))
                            {
                                foreach (BrokerToBrokerInterface broker in entry.Value) broker.forwardEvent(evt);
                            }
                        }

                        /*
                        List<BrokerToBrokerInterface> flattenBrokerList;
                        if (brokersByTopic.TryGetValue(evt.Topic, out flattenBrokerList))
                        {
                            foreach (BrokerToBrokerInterface broker in flattenBrokerList)
                            {
                                broker.forwardEvent(evt);
                            }
                        }
                        */
                    }

                    string topic;
                    //List<SubscriberInterface> flattenSubscriberList;
                    foreach (KeyValuePair<string, List<SubscriberInterface>> entry in subscribersByTopic)
                    {
                        topic = entry.Key;
                        if (topic.Equals(evt.Topic))
                        {
                            //subscribersByTopic.TryGetValue(evt.Topic, out flattenSubscriberList);
                            foreach (SubscriberInterface sub in entry.Value) sub.deliverToSub(evt);
                        }
                        else if(isSubtopicOf(evt.Topic, topic)) { 
                            foreach (SubscriberInterface sub in entry.Value) sub.deliverToSub(evt);
                        }
                    }

                    /*
                    List<SubscriberInterface> flattenList;
                    if (subscribersByTopic.TryGetValue(evt.Topic, out flattenList))
                    {
                        foreach (SubscriberInterface sub in flattenList)
                        {
                            sub.deliverToSub(evt);
                        }
                    }
                    */
                }
            }
        }

        public void addDad(string url)
        {
            lock (this)
            {
                BrokerToBrokerInterface brokerDad = (BrokerToBrokerInterface)Activator.GetObject(typeof(BrokerToBrokerInterface), url + "B");
                dad.Add(brokerDad);
                if (routing.Equals("filter")) topicsProvidedByBroker.Add(brokerDad, new List<string>());
            }
        }

        public void addSon(string url)
        {
            lock (this)
            {
                BrokerToBrokerInterface brokerSon = (BrokerToBrokerInterface)Activator.GetObject(typeof(BrokerToBrokerInterface), url + "B");
                sons.Add(brokerSon);
                if (routing.Equals("filter")) topicsProvidedByBroker.Add(brokerSon, new List<string>());
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
                SubscriberInterface newSubscriber = (SubscriberInterface)Activator.GetObject(
                           typeof(SubscriberInterface), subscriberURL);

                if (subscribersByTopic.ContainsKey(topic))
                    subscribersByTopic[topic].Add(newSubscriber);
                else
                    subscribersByTopic.Add(topic, new List<SubscriberInterface> { newSubscriber });

                Console.WriteLine("Subscriber: " + subscriberURL + " topic: " + topic);

                if (routing.Equals("filter"))
                {
                    foreach (KeyValuePair<BrokerToBrokerInterface, List<string>> entry in topicsProvidedByBroker)
                    {
                        if (!entry.Value.Contains(topic))
                        {
                            foreach (string topicValue in entry.Value)
                                if (isSubtopicOf(topic, topicValue)) return;

                            entry.Key.forwardInterest(this.url, topic);
                            Console.WriteLine("Forward interest to " + entry.Key.getURL() + " on topic " + topic);
                            entry.Value.Add(topic);
                        }
                    }
                }

            }
        }

        public void unsubscribe(string topic, string subscriberURL)
        {
            Console.WriteLine("BTBI Unsubscribe " + subscriberURL + " topic: " + topic);
            SubscriberInterface subscriber = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), subscriberURL);
            foreach(SubscriberInterface sub in subscribersByTopic[topic])
            {
                Console.WriteLine("foreach sub in subscribersByTopic[topic]");
                if (sub.getURL().Equals(subscriberURL))
                {
                    Console.WriteLine("sub.getURL().Equals(subscriberURL)");
                    subscribersByTopic[topic].Remove(sub);

                    if (subscribersByTopic[topic].Count == 0)
                    {
                        Console.WriteLine("subscribersByTopic[topic].Count == 0");
                        subscribersByTopic.Remove(topic);
                    }

                    break;
                }
            }

            if (routing.Equals("filter"))
            {
                Console.WriteLine("filter: forward insterest");
                forwardDisinterest(url, topic);                
            }
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
                //Console.WriteLine("brokersByTopic.ContainsKey(" + topic + ")");
                foreach (BrokerToBrokerInterface broker in brokers)
                {
                    //Console.WriteLine("foreach broker brokersByTopic");
                    if (!broker.getURL().Equals(url))
                    {
                        //Console.WriteLine("!broker.getURL().Equals(" + url + ")");
                        brokersByTopic[topic].Add(interestedBroker);

                        forwardInterestAux(url, topic);
                    }
                }

            } else
            {
                //Console.WriteLine("!brokersByTopic.ContainsKey(" + topic + ")");
                brokersByTopic.Add(topic, new List<BrokerToBrokerInterface> { interestedBroker });

                forwardInterestAux(url, topic);
            }
            
        }

        //para cada keyvaluepair<broker, topics que recebe desse broker> de topicsProvidedByBroker,
        //se ainda nao recebemos o topico "topic" de um broker vizinho,
        //enviamos lhe forwardInterest
        public void forwardInterestAux(string url, string topic)
        {
            foreach (KeyValuePair<BrokerToBrokerInterface, List<string>> entry in topicsProvidedByBroker)
            {
                //Console.WriteLine("foreach topicsProvidedByBroker");
                if (!entry.Key.getURL().Equals(url))
                {
                    //Console.WriteLine("!entry.Key.getURL().Equals(" + url + ")");
                    if (!entry.Value.Contains(topic))
                    {
                        //Console.WriteLine("!entry.Value.Contains(" + topic + ")");
                        entry.Key.forwardInterest(this.url, topic);
                        Console.WriteLine("Forwarded interest to " + entry.Key.getURL() + " on topic " + topic);
                        entry.Value.Add(topic);
                    }
                }
            }
        }

        public void forwardDisinterestAux(string url, string topic)
        {
            foreach (KeyValuePair<BrokerToBrokerInterface, List<string>> entry in topicsProvidedByBroker)
            {
                //Console.WriteLine("foreach topicsProvidedByBroker");
                if (!entry.Key.getURL().Equals(url))
                {
                    if (entry.Value.Contains(topic))
                    {
                        entry.Key.forwardDisinterest(this.url, topic);
                        Console.WriteLine("Forwarded disinterest to " + entry.Key.getURL() + " on topic " + topic);
                        entry.Value.Remove(topic);
                    }
                }
            }
        }

        public void forwardDisinterestAux2(string url, string topic)
        {
            string auxTopicSub = "";
            string auxTopicBro = "";
            if (!subscribersByTopic.ContainsKey(topic))
            {
                foreach (KeyValuePair<string, List<SubscriberInterface>> item in subscribersByTopic)
                {
                    if (isSubtopicOf(item.Key, topic))
                    {
                        if (item.Key.Length > auxTopicSub.Length) auxTopicSub = item.Key;
                    }
                }
            }
            else { return; }

            if (!brokersByTopic.ContainsKey(topic))
            {
                foreach (KeyValuePair<string, List<BrokerToBrokerInterface>> item in brokersByTopic)
                {
                    if (isSubtopicOf(item.Key, topic))
                    {
                        if (item.Key.Length > auxTopicBro.Length) auxTopicBro = item.Key;
                    }
                }
            }
            else { return; }


            if ((!auxTopicSub.Equals("")) || (!auxTopicBro.Equals("")))
            {
                forwardDisinterestAux(url, topic);
                if (auxTopicSub.Length >= auxTopicBro.Length) forwardInterestAux(url, auxTopicBro);
                else forwardInterestAux(url, auxTopicSub);
            }
            else
            {
                forwardDisinterestAux(url, topic);
            }
        }


        public void forwardDisinterest(string url, string topic)
        {
            BrokerToBrokerInterface disinterestedBroker = (BrokerToBrokerInterface)Activator.GetObject(typeof(BrokerToBrokerInterface), url + "B");
            
            brokersByTopic[topic].Remove(disinterestedBroker);

            forwardDisinterestAux2(url, topic);
            
        }


    }
}
