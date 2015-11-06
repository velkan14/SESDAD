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
        int lastMsgNumber;
        List<Event> msgQueue = new List<Event>();

        public SubAux(SubscriberInterface sub)
        {
            lastMsgNumber = -1;
            this.sub = sub;
        }

        public void addToQueue (Event evt)
        {
            msgQueue.Add(evt);
            List<Event> sortedList = msgQueue.OrderBy(o => o.MsgNumber).ToList();
            msgQueue = sortedList;
        }

        public int LastMsgNumber{
            get { return lastMsgNumber; }
            set { lastMsgNumber = value; }
        }

        public List<Event> MsgQueue
        {
            get { return msgQueue; }
        }

        public SubscriberInterface Sub
        {
            get { return sub; }
        }

        internal void updateMsgQueue(List<Event> eventsToRemove)
        {
            foreach(Event e in eventsToRemove)
            {
                msgQueue.Remove(e);
            }
            
        }
    }
}
