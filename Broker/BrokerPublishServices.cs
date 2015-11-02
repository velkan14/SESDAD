using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTypes;

namespace Broker
{
    class BrokerPublishServices : MarshalByRefObject, BrokerPublishInterface {

        private Broker broker;

        public BrokerPublishServices(Broker broker) {
            this.broker = broker;
        }

        public delegate void Publish(Event newEvent);

        public void publishEvent(Event newEvent) {
            //Console.WriteLine(newEvent.Topic + ":" + newEvent.Content);
            Publish pub = new Publish(broker.publishEvent);
            IAsyncResult RemAr = pub.BeginInvoke(newEvent, null, null);
        }
    }
}
