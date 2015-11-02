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

        public void forwardEvent(Event evt) {
            forwardEventAsync forwardDelegate = new forwardEventAsync(broker.forwardEvent);
            AsyncCallback RemoteCallback = new AsyncCallback(BrokerToBrokerServices.OurRemoteAsyncCallBack);
            IAsyncResult RemAr = forwardDelegate.BeginInvoke(evt, RemoteCallback, null);
        }
    }
}
