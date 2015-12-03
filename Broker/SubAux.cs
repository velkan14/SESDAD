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
        //para a total order. Ja nao fazemos a distincao por publisher mas sim por topico
        Dictionary<string, List<Event>> msgQueueByTopic = new Dictionary<string, List<Event>>();
        List<Event> msgQueueTotal = new List<Event>();
        List<int> filteredSeqTotal = new List<int>();
        int lastGlobalMsgNumber = -1;
        HashSet<int> eventsNotToDeliver = new HashSet<int>();

        public SubAux(SubscriberInterface sub)
        {
            this.sub = sub;
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
                msgQueueByPub[pubURL] = auxEvt.OrderBy(o => o.MsgNumber).ToList();              
            }
        }

        public void addToQueueTopic(Event evt)
        {
            string topic = evt.Topic;
            List<Event> auxEvt = new List<Event>();
            if (!msgQueueByTopic.TryGetValue(topic, out auxEvt))
            {
                msgQueueByTopic.Add(topic, new List<Event> { evt });
                Console.WriteLine("criei entrada na msgQueueByTopic. Topic: " + topic + " msg numero: " + evt.MsgNumber);
            }
            else
            {
                auxEvt.Add(evt);
                msgQueueByTopic[topic] = auxEvt.OrderBy(o => o.MsgNumber).ToList();
                Console.WriteLine("adicionada mensagem na msgQueueByTopic. Topic: " + topic + " msg numero: " + evt.MsgNumber);
            }
        }

        public void addToQueueTotal(Event evt)
        {
            msgQueueTotal.Add(evt);
            msgQueueTotal = msgQueueTotal.OrderBy(o => o.MsgNumber).ToList();
        }

        public List<Event> updateQueueTotal(int msgNb)
        {

            return msgQueueTotal;
        }

        public List<Event> getQueueTotal()
        {
            return msgQueueTotal;
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
            Console.WriteLine("lastMsg number updatado para: " + auxMsgNumber);

            if (filteredSeqNumbers.TryGetValue(pubURL, out seqNumbers))
            {
                List<int> toRemove = new List<int>();
                if (auxMsgNumber == seqNumbers.First())
                {
                    //int cnt = -1;
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
                            toRemove.Add(i);
                        }
                        else
                        {
                            break;
                        }                       
                    }
                    foreach(int j in toRemove){
                        seqNumbers.Remove(j);
                    }
                    //seqNumbers.RemoveRange(0, cnt);
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

        public List<Event> msgQueueTopic(string topic)
        {
            List<Event> lst = new List<Event>();
            if (msgQueueByTopic.TryGetValue(topic, out lst))
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
                filteredSeqNumbers[pubURL] = auxInt.OrderBy(o => o).ToList();
            }
        }

        public void addfilteredSeqTotalNumber(int seqN)
        {
            filteredSeqTotal.Add(seqN);
            filteredSeqTotal = filteredSeqTotal.OrderBy(o => o).ToList();
        }

        public void updatefilteredSeqTotal(int seqN)
        {
            List<int> toRemove = new List<int>();
            foreach (int i in filteredSeqTotal)
            {
                if (lastGlobalMsgNumber+1 == i)
                {
                    updateLastGlobalMsgNumber();
                    toRemove.Add(i);
                }
                else
                {
                    break;
                }
            }
            foreach (int j in toRemove)
            {
                filteredSeqTotal.Remove(j);
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

        public void updateMsgQueueTopic(string topic, List<Event> eventsToRemove)
        {
            List<Event> lst = new List<Event>();
            if (msgQueueByTopic.TryGetValue(topic, out lst))
            {
                foreach (Event e in eventsToRemove)
                {
                    lst.Remove(e);
                }
            }
        }

        public int LastGlobalMsgNumber
        {
            get { return lastGlobalMsgNumber; }
            set { lastGlobalMsgNumber = value; }
        }

        public void updateLastGlobalMsgNumber()
        {
            lastGlobalMsgNumber++;
            Console.WriteLine("lastglobalNumber update: " + lastGlobalMsgNumber);
        }

        public void updateLastGlobalMsgNumber(int nb)
        {
            lastGlobalMsgNumber = nb;
            Console.WriteLine("lastglobalNumber update: " + lastGlobalMsgNumber);
        }

        public void updateMsgQueueTotal(List<Event> eventsToRemove)
        {
            foreach (Event e in eventsToRemove)
            {
                msgQueueTotal.Remove(e);
            }
        }

        public void addEventNotToDeliver(int msgNb)
        {
            eventsNotToDeliver.Add(msgNb);
        }
        public bool checkEventNotToDeliver(Event evt)
        {
            return eventsNotToDeliver.Contains(evt.MsgNumber);
        }
    }
}
