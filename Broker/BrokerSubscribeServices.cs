using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTypes;

namespace Broker
{
    class BrokerSubscribeServices : MarshalByRefObject, BrokerSubscribeInterface
    {
        Broker broker;

        public BrokerSubscribeServices(Broker broker)
        {
            this.broker = broker;
        }
        public delegate void Subscribe(string topic, string subscriberURL);
        public delegate void Unsubscribe(string topic, string subscriberURL);

        public void subscribe(string topic, string subscriberURL)
        {
            Subscribe sub = new Subscribe(broker.subscribe);
            IAsyncResult RemAr = sub.BeginInvoke(topic, subscriberURL, null, null);
        }

        public void unsubscribe(string topic, string subscriberURL)
        {
            Unsubscribe unsub = new Unsubscribe(broker.unsubscribe);
            IAsyncResult RemAr = unsub.BeginInvoke(topic, subscriberURL, null, null);
        }

    }
}
