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

        //conjunto dos publishers que publicam eventos de topicos a que o sub está subscrito (e respectivos topicos)
      //  Dictionary<string, HashSet<string>> pubs = new Dictionary<string, HashSet<string>>();

        public SubAux(SubscriberInterface sub)
        {
            this.sub = sub;
        }

      /*  public void addPub(string pubURL, string msgTopic)
        {
            HashSet<string> auxHst;
            if(pubs.TryGetValue(pubURL, out auxHst))
            {
                if(!auxHst.Contains(msgTopic))
                    auxHst.Add(msgTopic);
            }
            else
            {
                pubs.Add(pubURL, new HashSet<string>(){ msgTopic });
            }
        }

        public void updatePubs(string topic)
        {
            foreach(KeyValuePair<string, HashSet<string>> entry in pubs)
            {
                entry.Value.Remove(topic);
            }
        }

        public bool assertPub(string pubURL)
        {
            HashSet<string> auxHst;
            if (pubs.TryGetValue(pubURL, out auxHst))
            {
                if (auxHst.Count == 0) return false;
                return true;
            }
            return false; 
        }

        //sees if, for a specific publisher, the sub is subscribed to more topics from that pub
        public bool assertMoreTopics(string pubURL, string topic)
        {
            HashSet<string> auxHst;
            if (pubs.TryGetValue(pubURL, out auxHst))
            {
                HashSet<string> tempHst = auxHst;
                tempHst.Remove(topic);
                if (tempHst.Count > 0) return true;
            }
            return false;
        }
        */
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
                auxMsgNumber = lastMsgNumberByPub[pubURL];
            }
            else
            {
                auxMsgNumber = initLastMsgNumber + 1;
                lastMsgNumberByPub.Add(pubURL, auxMsgNumber);
                
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
    }
}
