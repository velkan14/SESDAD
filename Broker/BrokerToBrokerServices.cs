using CommonTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Broker
{
    class BrokerToBrokerServices : MarshalByRefObject, BrokerToBrokerInterface  {
        BrokerToBrokerInterface dad;
        List<BrokerToBrokerInterface> sons;
        Dictionary<string, List<SubscriberInterface>> subscribersByTopic;
        string routing;

        public BrokerToBrokerServices(BrokerToBrokerInterface dad, List<BrokerToBrokerInterface> sons, 
            Dictionary<string, List<SubscriberInterface>> subscribersByTopic, string routing) {
            this.dad = dad;
            this.sons = sons;
            this.subscribersByTopic = subscribersByTopic;
            this.routing = routing;
        }

        public void forwardEvent(Event evt) {
            if (routing.Equals("flooding")) {
                if (dad != null) {
                    dad.forwardEvent(evt);
                }
                foreach (BrokerToBrokerInterface son in sons) {
                    son.forwardEvent(evt);
                }
                var flattenList = subscribersByTopic.SelectMany(x => x.Value);
                foreach (SubscriberInterface sub in flattenList) {
                    sub.deliverToSub(evt);
                }
            } else {
                //TODO
            }
        }
    }
}
