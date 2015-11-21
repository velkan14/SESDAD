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

        public int LastMsgNumber(string pubURL)
        {
            int lastMsgNumber;
            if (!lastMsgNumberByPub.TryGetValue(pubURL, out lastMsgNumber))
                return initLastMsgNumber;
            return lastMsgNumber;           
        }

        public void UpdateLastMsgNumber(string pubURL)
        {
            int lastMsgNumber;
            if (lastMsgNumberByPub.TryGetValue(pubURL, out lastMsgNumber))
                lastMsgNumber++;
            else
                lastMsgNumberByPub.Add(pubURL, initLastMsgNumber++);
        }


        public List<Event> MsgQueue(string pubURL)
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
