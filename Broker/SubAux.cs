using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTypes;

namespace Broker
{
    class SubAux
    {
        SubscriberInterface sub;
        int initLastMsgNumber = -1;
        Dictionary<string, List<Event>> msgQueueByPub = new Dictionary<string, List<Event>>();
        Dictionary<string, int> lastMsgNumberByPub = new Dictionary<string, int>();
        //utilizado para registar os numeros de sequencia dos eventos que foram filtrados
        Dictionary<string, List<int>> filteredSeqNumbers = new Dictionary<string, List<int>>();
        Dictionary<string, HashSet<string>> topicsProvidersByTopic = new Dictionary<string, HashSet<string>>();
        //conjunto dos publishers que publicam eventos de topicos a que o sub está subscrito
        Dictionary<string, int> pubs = new Dictionary<string, int>();

        public SubAux(SubscriberInterface sub)
        {
            this.sub = sub;
        }

        public void addPub(string pubURL)
        {
            int i;
            if(!pubs.TryGetValue(pubURL, out i))
            {
                pubs.Add(pubURL, 1);
            }
            else
            {
                pubs[pubURL]++;
            }
        }

        public void updatePubs(string pubURL)
        {
            int i;
            if (pubs.TryGetValue(pubURL, out i))
            { 
                pubs[pubURL]--;
                if (pubs[pubURL] == 0)
                {
                    pubs.Remove(pubURL);
                }
            }
            else
            {
                //do nothing. We never unsub a topic we havent subbed first
            }
        }

        public bool assertPub(string pubURL)
        {
            int i;
            return pubs.TryGetValue(pubURL, out i);
        }

        public void addToQueue(Event evt)
        {
            string pubURL = evt.PublisherName;
            List<Event> auxEvt = new List<Event>();
            if(!msgQueueByPub.TryGetValue(pubURL, out auxEvt))
            {
                msgQueueByPub.Add(pubURL, new List<Event> { evt });
            }
            else
            {
                auxEvt.Add(evt);
                auxEvt.OrderBy(o => o.MsgNumber).ToList();              
            }
        }

        public int lastMsgNumber(string pubURL)
        {
            int lastMsgNumber;
            if (!lastMsgNumberByPub.TryGetValue(pubURL, out lastMsgNumber))
                return initLastMsgNumber;
            return lastMsgNumber;           
        }

        public void updateLastMsgNumber(string pubURL)
        {
            int lastMsgNumber;
            int auxMsgNumber;
            List<int> seqNumbers;
            if (lastMsgNumberByPub.TryGetValue(pubURL, out lastMsgNumber))
            {
                lastMsgNumberByPub[pubURL]++;  
                auxMsgNumber = lastMsgNumber++;
              //  Console.WriteLine("updateLastMsgNumber: " + auxMsgNumber);
            }
            else
            {
                initLastMsgNumber++;
                lastMsgNumberByPub.Add(pubURL, initLastMsgNumber);
                auxMsgNumber = initLastMsgNumber;
              //  Console.WriteLine("updateLastMsgNumberINIT: " + initLastMsgNumber);
            }
            if (filteredSeqNumbers.TryGetValue(pubURL, out seqNumbers))
            {
                if (auxMsgNumber == seqNumbers.First())
                {
                    int cnt = 0;
                    foreach(int i in seqNumbers)
                    {
                        if (auxMsgNumber == i)
                        {
                            int lastMsgNumber2;
                            if (lastMsgNumberByPub.TryGetValue(pubURL, out lastMsgNumber2))
                            {
                                lastMsgNumberByPub[pubURL]++;
                            }
                            else
                            {
                                lastMsgNumberByPub.Add(pubURL, initLastMsgNumber++);
                            }
                            auxMsgNumber++;
                        }
                        else
                        {
                            break;
                        }
                        cnt++;
                    }
                    seqNumbers.RemoveRange(0, cnt);
                }
            }
                

        }


        public List<Event> msgQueue(string pubURL)
        {
            List<Event> lst = new List<Event>();
            if (msgQueueByPub.TryGetValue(pubURL, out lst))
                return lst;
            return null;
        }


        public SubscriberInterface Sub
        {
            get { return sub; }
        }

        public void addfilteredSeqNumber(string pubURL, int seqN)
        {
            List<int> auxInt = new List<int>();
            if (!filteredSeqNumbers.TryGetValue(pubURL, out auxInt))
            {
                filteredSeqNumbers.Add(pubURL, new List<int> { seqN });
            }
            else
            {
                auxInt.Add(seqN);
                auxInt.OrderBy(o => o).ToList();
            }
        }

        public void updateMsgQueue(string pubURL, List<Event> eventsToRemove)
        {
            List<Event> lst = new List<Event>();
            if (msgQueueByPub.TryGetValue(pubURL, out lst))
            {
                foreach (Event e in eventsToRemove)
                {
                    lst.Remove(e);
                }
            }
        }

        public void addTopicProvider(string topic, string pubURL)
        {
           
            HashSet<string> hst = new HashSet<string>();
            if (!topicsProvidersByTopic.TryGetValue(topic, out hst)){
                HashSet<string> aux = new HashSet<string>();
                aux.Add(topic);
                topicsProvidersByTopic[topic] = aux;
            }
            else
            {
                hst.Add(topic);
            }

        }

        public HashSet<string> getTopicProviders(string topic)
        {
            HashSet<string> hst = new HashSet<string>();
            if (topicsProvidersByTopic.TryGetValue(topic, out hst))
            {
                return hst;
            }
            return new HashSet<string>();
        }
    }
}
