using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTypes;

namespace Subscriber
{
    class SubscriberImpl : MarshalByRefObject, SubscriberInterface
    {
        private Subscriber sub;

        public delegate void DeliverEvent(Event evt);

        public SubscriberImpl(Subscriber sub)
        {
            this.sub = sub;
        }
        public void deliverToSub(Event evt)
        {
            DeliverEvent de = new DeliverEvent(sub.deliverToSub);
            de.BeginInvoke(evt, null, null);
        }
    }
}
