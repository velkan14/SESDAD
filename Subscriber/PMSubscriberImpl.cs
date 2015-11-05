using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTypesPM;
using CommonTypes;

namespace Subscriber
{
    class PMSubscriberImpl : MarshalByRefObject, PMSubscriber
    {
        Subscriber sub;

        public delegate void SubscriberAsyncString(string topic);
        public delegate void SubscriberAsyncDoSomething();

        public PMSubscriberImpl(Subscriber sub)
        {
            this.sub = sub;
        }
        public void crash()
        {
            throw new NotImplementedException();
        }

        public void freeze()
        {
            throw new NotImplementedException();
        }

        public void status()
        {
            throw new NotImplementedException();
        }

        public void subscribe(string topic)
        {
            SubscriberAsyncString s = new SubscriberAsyncString(sub.subscribe);
            s.BeginInvoke(topic, null, null);
        }

        public void unfreeze()
        {
            throw new NotImplementedException();
        }

        public void unsubscribe(string topic)
        {
            SubscriberAsyncString s = new SubscriberAsyncString(sub.unsubscribe);
            s.BeginInvoke(topic, null, null);
        }
    }
}
