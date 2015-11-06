using CommonTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace Broker
{
    class BrokerToBrokerServices : MarshalByRefObject, BrokerToBrokerInterface  {
        Broker broker;

        public BrokerToBrokerServices(Broker broker) {
            this.broker = broker;
        }
        public delegate void forwardEventAsync(Event evt);
        public static void OurRemoteAsyncCallBack(IAsyncResult ar)
        {
            forwardEventAsync del = (forwardEventAsync)((AsyncResult)ar).AsyncDelegate;
            del.EndInvoke(ar);

            return;
        }

        public string getURL() { return broker.getURL(); }

        public void forwardEvent(Event evt) {
            forwardEventAsync forwardDelegate = new forwardEventAsync(broker.forwardEvent);
            AsyncCallback RemoteCallback = new AsyncCallback(BrokerToBrokerServices.OurRemoteAsyncCallBack);
            IAsyncResult RemAr = forwardDelegate.BeginInvoke(evt, RemoteCallback, null);
        }

        public delegate void forwardInterestAsync(string url, string topic);
        public void forwardInterest(string url, string topic)
        {
            forwardInterestAsync forwardDelegate = new forwardInterestAsync(broker.forwardInterest);
            IAsyncResult RemAr = forwardDelegate.BeginInvoke(url, topic, null, null);
        }

        public void forwardDisinterest(string url, string topic)
        {
            forwardInterestAsync forwardDelegate = new forwardInterestAsync(broker.forwardDisinterest);
            IAsyncResult RemAr = forwardDelegate.BeginInvoke(url, topic, null, null);
        }
    }
}
