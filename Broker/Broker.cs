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
                    events.Add(evt);

                    if (routing.Equals("flooding"))
                    {
                        foreach (BrokerToBrokerInterface d in dad) d.forwardEvent(evt);
                        foreach (BrokerToBrokerInterface son in sons)
                        {
                            son.forwardEvent(evt);
                        }
                        
                    }
                    else if (routing.Equals("filter"))
                    {

                        string keyTopic;
                        foreach (KeyValuePair<string, List<BrokerToBrokerInterface>> entry in brokersByTopic)
                        {
                            keyTopic = entry.Key;
                            if (keyTopic.Equals(evt.Topic))
                            {
                                foreach (BrokerToBrokerInterface broker in entry.Value) broker.forwardEvent(evt);
                            }
                            else if (isSubtopicOf(evt.Topic, keyTopic))
                            {
                                foreach (BrokerToBrokerInterface broker in entry.Value) broker.forwardEvent(evt);
                            }
                        }
                        
                    }

                    string topic;
                    foreach (KeyValuePair<string, List<SubscriberInterface>> entry in subscribersByTopic)
                    {
                        topic = entry.Key;
                        if (topic.Equals(evt.Topic))
                        {
                            foreach (SubscriberInterface sub in entry.Value) sub.deliverToSub(evt);
                        }
                        else if(isSubtopicOf(evt.Topic, topic)) { 
                            foreach (SubscriberInterface sub in entry.Value) sub.deliverToSub(evt);
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
        }

        public void publishEvent(Event newEvent)
        {
            forwardEvent(newEvent);
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
