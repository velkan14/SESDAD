using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTypes;

namespace Broker
{
    class BrokerPublishServices : MarshalByRefObject, BrokerPublishInterface {
        private BrokerToBrokerInterface broker;

        public BrokerPublishServices(BrokerToBrokerInterface broker) {
            this.broker = broker;
        }

        public void publishEvent(Event newEvent) {
            //Console.WriteLine(newEvent.Topic + ":" + newEvent.Content);
            broker.forwardEvent(newEvent);
        }
    }
}
