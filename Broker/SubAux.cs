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
                auxEvt.OrderBy(o => o.MsgNumber).ToList();              
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
                auxEvt.OrderBy(o => o.MsgNumber).ToList();
                Console.WriteLine("adicionada mensagem na msgQueueByTopic. Topic: " + topic + " msg numero: " + evt.MsgNumber);
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
            Console.WriteLine("lastMsg number updatado para: " + auxMsgNumber);

            if (filteredSeqNumbers.TryGetValue(pubURL, out seqNumbers))
            {
                if (auxMsgNumber == seqNumbers.First())
                {
                    int cnt = -1;
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
                            cnt++;
                        }
                        else
                        {
                            break;
                        }                       
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

    }
}
