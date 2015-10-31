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
        BrokerSubscribeInterface broker;
        string myUrl;

        public PMSubscriberImpl(BrokerSubscribeInterface broker, string url)
        {
            this.broker = broker;
            this.myUrl = url;
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
            broker.subscribe(topic, myUrl);
        }

        public void unfreeze()
        {
            throw new NotImplementedException();
        }

        public void unsubscribe(string topic)
        {
            broker.unsubscribe(topic);
        }
    }
}
