using CommonTypes;
using CommonTypesPM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Broker
{
    class Broker :  BrokerToBrokerInterface, PMBroker, BrokerSubscribeInterface, BrokerPublishInterface
    {
        string url;
        private int numberFreezes = 0;
        private bool freezeFlag = false;
        AutoResetEvent freezeEvent = new AutoResetEvent(false);

        List<BrokerToBrokerInterface> dad = new List<BrokerToBrokerInterface>();
        List<BrokerToBrokerInterface> sons = new List<BrokerToBrokerInterface>();
        Dictionary<string, List<SubscriberInterface>> subscribersByTopic = new Dictionary<string, List<SubscriberInterface>>();
        List<SubAux> subLastMsgReceived = new List<SubAux>();

        Dictionary<string, List<KeyValuePair<string, int>>> subLastMsgReceivedByPub = new Dictionary<string, List<KeyValuePair<string, int>>>();

        Dictionary<string, List<BrokerToBrokerInterface>> brokersByTopic = new Dictionary<string, List<BrokerToBrokerInterface>>();
        Dictionary<BrokerToBrokerInterface, List<string>> topicsProvidedByBroker = new Dictionary<BrokerToBrokerInterface, List<string>>();

        List<Event> events = new List<Event>();
        string routing, ordering, loggingLevel;
        string processName;
        NotificationReceiver pm;

        public Broker(NotificationReceiver pm, string processName, string routing, string ordering, string loggingLevel)
        {
            this.pm = pm;
            this.processName = processName;
            this.routing = routing;
            this.ordering = ordering;
            this.loggingLevel = loggingLevel;
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

        public void forwardEvent(string url, Event evt)
        {
            if (freezeFlag)
            {
                lock(this) numberFreezes++;
                freezeEvent.WaitOne();
            }
            lock (this)
            {
                bool exists = false;
                foreach (Event e in events) if (evt == e) exists = true;
                if (!exists)
                {
                    events.Add(evt);

                    if (routing.Equals("flooding"))
                    {
                        foreach (BrokerToBrokerInterface d in dad) d.forwardEvent(this.url,evt);
                        foreach (BrokerToBrokerInterface son in sons)
                        {
                            son.forwardEvent(this.url, evt);
                            notifyPM(evt);
                        }
                        
                    }
                    else if (routing.Equals("filter"))
                    {

                        string keyTopic;
                        foreach (KeyValuePair<string, List<BrokerToBrokerInterface>> entry in brokersByTopic)
                        {
                            keyTopic = entry.Key;
                            if (keyTopic.Equals(evt.Topic) || isSubtopicOf(evt.Topic, keyTopic))
                            {
                                foreach (BrokerToBrokerInterface broker in entry.Value)
                                {
                                    
                                    if (!broker.getURL().Equals(url))
                                    {
                                        broker.forwardEvent(this.url, evt);
                                        notifyPM(evt);
                                    }
                                }
                            }
                            
                        }
                        
                    }

                    string topic;
                    foreach (KeyValuePair<string, List<SubscriberInterface>> entry in subscribersByTopic)
                    {
                        topic = entry.Key;
                        if (topic.Equals(evt.Topic) || isSubtopicOf(evt.Topic, topic))
                        {
                            foreach (SubscriberInterface sub in entry.Value)
                            {
                                if (ordering == "FIFO")
                                {
                                    string pubURL = evt.PublisherName;                                  
                                    List<Event> eventsToRemove = new List<Event>();
                                    SubAux subAux = subLastMsgReceived.Find(o => o.Sub.getURL() == sub.getURL());
                                    int nextLastMsgNumber = subAux.LastMsgNumber(pubURL) + 1;
                                    if (evt.MsgNumber == nextLastMsgNumber)
                                    {
                                        sub.deliverToSub(evt);
                                        subAux.UpdateLastMsgNumber(pubURL);
                                        if (subAux.MsgQueue(pubURL) != null)
                                        {
                                            foreach (Event pendingEvent in subAux.MsgQueue(pubURL))
                                            {
                                                if (pendingEvent.MsgNumber == nextLastMsgNumber)
                                                {
                                                    sub.deliverToSub(pendingEvent);
                                                    subAux.UpdateLastMsgNumber(pubURL);
                                                    eventsToRemove.Add(pendingEvent);
                                                }
                                            }
                                            subAux.updateMsgQueue(pubURL, eventsToRemove);
                                            eventsToRemove.Clear();
                                        }
                                        
                                    }
                                    else
                                    {
                                        subAux.addToQueue(evt);
                                    }
                                } else sub.deliverToSub(evt);
                            }
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
            lock (this)
            {
                freezeFlag = true;
            }
        }

        public void status()
        {
            Console.WriteLine("Dad:");
            foreach(BrokerToBrokerInterface bb in dad) Console.WriteLine(bb.getURL());
            Console.WriteLine("Sons:");
            foreach (BrokerToBrokerInterface bb in sons) Console.WriteLine(bb.getURL());
        }

        public void unfreeze()
        {
            lock (this)
            {
                freezeFlag = false;
            }
            for (int i = 0; i < numberFreezes; i++)
                freezeEvent.Set();
            numberFreezes = 0;
        }

        public void subscribe(string topic, string subscriberURL)
        {
            if (freezeFlag)
            {
                numberFreezes++;
                freezeEvent.WaitOne();
            }
            lock (this)
            {
                bool subExists = false;
                SubscriberInterface newSubscriber = (SubscriberInterface)Activator.GetObject(
                           typeof(SubscriberInterface), subscriberURL);

                if (ordering == "FIFO")
                {
                    foreach (SubAux sub in subLastMsgReceived)
                    {
                        if (sub.Sub.getURL().Equals(subscriberURL))
                        {
                            subExists = true;
                            break;
                        }
                    }
                    if (!subExists)
                    {
                        subLastMsgReceived.Add(new SubAux(newSubscriber));
                    }
                        
                    subExists = false;
                }
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
            if (freezeFlag)
            {
                numberFreezes++;
                freezeEvent.WaitOne();
            }
            lock (this)
            {
                SubscriberInterface subscriber = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), subscriberURL);

                int index = 0;
                bool topicRemoved = false;
                foreach (SubscriberInterface sub in subscribersByTopic[topic])
                {
                    if (sub.getURL().Equals(subscriberURL))
                    {
                        if (subscribersByTopic[topic].Count == 1)
                        {
                            subscribersByTopic.Remove(topic);
                            topicRemoved = true;
                        }
                        else
                        {
                            subscribersByTopic[topic].RemoveAt(index);
                            return;
                        }
                        
                        break;
                    }
                    index++;
                }

                if (routing.Equals("filter") && topicRemoved)
                {
                    if (!brokersByTopic.ContainsKey(topic))
                    {
                        List<string> wantedTopics = mergeInterestedSubsAndBrokers(topic);
                        
                        foreach (KeyValuePair<BrokerToBrokerInterface, List<string>> entry in topicsProvidedByBroker)
                        {
                            if (entry.Value.Contains(topic))
                            {
                                entry.Key.forwardDisinterest(this.url, topic);
                                Console.WriteLine("Forward disinterest to " + entry.Key.getURL() + " on topic " + topic);
                                entry.Value.Remove(topic);

                                foreach (string newTopic in wantedTopics)
                                {
                                    entry.Key.forwardInterest(this.url, newTopic);
                                    Console.WriteLine("Forward interest to " + entry.Key.getURL() + " on topic " + newTopic);
                                    entry.Value.Add(topic);
                                }
                            }
                        }
                    }
                    else return;
                }
            }
            Console.WriteLine("Unsubscriber: " + subscriberURL + " topic: " + topic);
        }

        public void publishEvent(Event newEvent)
        {
            forwardEvent(this.url, newEvent);
            Console.WriteLine("Event Forwarded by Publisher: " + newEvent.PublisherName + ", topic: " + newEvent.Topic);
        }

        public void forwardInterest(string url, string topic)
        {
            Console.WriteLine("Received forwardInterest(" + url + ", " + topic + ")");
            BrokerToBrokerInterface interestedBroker = (BrokerToBrokerInterface)Activator.GetObject(typeof(BrokerToBrokerInterface), url + "B");

            List<BrokerToBrokerInterface> brokers;
            brokersByTopic.TryGetValue(topic, out brokers);

            if (brokersByTopic.ContainsKey(topic))
            {
                foreach (BrokerToBrokerInterface broker in brokers)
                {
                    if (!broker.getURL().Equals(url))
                    {
                        brokersByTopic[topic].Add(interestedBroker);

                        forwardInterestAux(url, topic);
                    }
                }

            } else
            {
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
                if (!entry.Key.getURL().Equals(url))
                {
                    if (!entry.Value.Contains(topic))
                    {
                        entry.Key.forwardInterest(this.url, topic);
                        Console.WriteLine("Forwarded interest to " + entry.Key.getURL() + " on topic " + topic);
                        entry.Value.Add(topic);
                    }
                }
            }
        }

        public void notifyPM(Event evt)
        {
            string notification = "BroEvent " + processName + ", " + evt.PublisherName + ", " + evt.Topic + ", " + evt.MsgNumber.ToString();
            if (loggingLevel.Equals("full")) pm.notify(notification);
            Console.WriteLine(notification);
        }
        
        public void forwardDisinterest(string url, string topic)
        {
            Console.WriteLine("Received forwardDisinterest(" + url + ", " + topic + ")");
            int index = 0;
            bool topicRemoved = false;
            foreach (BrokerToBrokerInterface broker in brokersByTopic[topic])
            {
                if (broker.getURL().Equals(url))
                {
                    if (brokersByTopic[topic].Count == 1)
                    {
                        brokersByTopic.Remove(topic);
                        topicRemoved = true;
                    }
                    else
                    {
                        brokersByTopic[topic].RemoveAt(index);
                        return;
                    }
                    break;
                }
                index++;
            }

            if (topicRemoved)
            {
                if (!subscribersByTopic.ContainsKey(topic))
                {
                    List<string> wantedTopics = mergeInterestedSubsAndBrokers(topic);
                    foreach (KeyValuePair<BrokerToBrokerInterface, List<string>> entry in topicsProvidedByBroker)
                    {
                        if (entry.Value.Contains(topic))
                        {
                            entry.Key.forwardDisinterest(this.url, topic);
                            Console.WriteLine("Forward disinterest to " + entry.Key.getURL() + " on topic " + topic);
                            entry.Value.Remove(topic);

                            foreach (string newTopic in wantedTopics)
                            {
                                entry.Key.forwardInterest(this.url, newTopic);
                                Console.WriteLine("Forward interest to " + entry.Key.getURL() + " on topic " + newTopic);
                                entry.Value.Add(topic);
                            }
                        }
                    }
                }
                else return;

            }
            
        }

      
        public List<string> subscribersByTopicSubtopicsOf(string topic)
        {
            List<string> subtopics = new List<string>();
            foreach (KeyValuePair<string, List<SubscriberInterface>> entry in subscribersByTopic)
            {
                if (isSubtopicOf(entry.Key, topic))
                {
                    subtopics.Add(entry.Key);
                }
            }
            
            return reorganizeSubtopics(subtopics);

        }

        public List<string> brokersByTopicSubtopicsOf(string topic)
        {
            List<string> subtopics = new List<string>();
            foreach (KeyValuePair<string, List<BrokerToBrokerInterface>> entry in brokersByTopic)
            {
                if (isSubtopicOf(entry.Key, topic))
                {
                    subtopics.Add(entry.Key);
                }
            }
            
            return reorganizeSubtopics(subtopics);

        }

        public List<string> mergeInterestedSubsAndBrokers(string topic)
        {
            var stillInterestedSubscribers = subscribersByTopicSubtopicsOf(topic);
            var stillInterestedBrokers = brokersByTopicSubtopicsOf(topic);

            stillInterestedSubscribers.Union(stillInterestedBrokers);
            
            return reorganizeSubtopics(stillInterestedSubscribers);
        }

        public List<string> reorganizeSubtopics(List<string> subtopics)
        {
            List<string> stillInterestedTopics = new List<string>();
            bool isSubtopic = false;
            foreach (string a in subtopics)
            {
                isSubtopic = false;
                if (stillInterestedTopics.Count > 0)
                {
                    int index = 0;
                    foreach (string b in stillInterestedTopics)
                    {
                        if (isSubtopicOf(a, b))
                        {
                            isSubtopic = true;
                            break;
                        }
                        else if (isSubtopicOf(b, a))
                        {
                            stillInterestedTopics.RemoveAt(index);
                            break;
                        }
                        index++;
                    }
                }
                
                if (!isSubtopic) stillInterestedTopics.Add(a);
            }
            return stillInterestedTopics;
        }


    }
}
