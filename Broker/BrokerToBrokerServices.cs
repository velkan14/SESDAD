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
        public delegate void forwardEventAsync(string url, Event evt);
        public static void OurRemoteAsyncCallBack(IAsyncResult ar)
        {
            forwardEventAsync del = (forwardEventAsync)((AsyncResult)ar).AsyncDelegate;
            del.EndInvoke(ar);
            return;
        }

        public string getURL() { return broker.getURL(); }

        public void forwardEvent(string url, Event evt) {
            forwardEventAsync forwardDelegate = new forwardEventAsync(broker.forwardEvent);
            AsyncCallback RemoteCallback = new AsyncCallback(BrokerToBrokerServices.OurRemoteAsyncCallBack);
            IAsyncResult RemAr = forwardDelegate.BeginInvoke(url, evt, RemoteCallback, null);
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

        public delegate void rcvSeqNumberAsync(int seqNumber, Event evt);
        public static void RemoteAsyncCallBack(IAsyncResult ar)
        {
            rcvSeqNumberAsync del = (rcvSeqNumberAsync)((AsyncResult)ar).AsyncDelegate;
            del.EndInvoke(ar);
            return;
        }

        public void rcvSeqNumber(int seqNumber, Event evt)
        {
            rcvSeqNumberAsync forwardDelegate = new rcvSeqNumberAsync(broker.rcvSeqNumber);
            AsyncCallback RemoteCallback = new AsyncCallback(BrokerToBrokerServices.RemoteAsyncCallBack);
            IAsyncResult RemAr = forwardDelegate.BeginInvoke(seqNumber, evt, RemoteCallback, null);
        }


        public delegate void reqSequenceAsync(string url, Event evt);
        public static void AnotherRemoteAsyncCallBack(IAsyncResult ar)
        {
            reqSequenceAsync del = (reqSequenceAsync)((AsyncResult)ar).AsyncDelegate;
            del.EndInvoke(ar);
            return;
        }
     
        public void reqSequence(string url, Event evt)
        {
            reqSequenceAsync forwardDelegate = new reqSequenceAsync(broker.reqSequence);
            AsyncCallback RemoteCallback = new AsyncCallback(BrokerToBrokerServices.AnotherRemoteAsyncCallBack);
            IAsyncResult RemAr = forwardDelegate.BeginInvoke(url, evt, AnotherRemoteAsyncCallBack, null);
        }

        public delegate void rcvGlobalInfoAsync(int seqNb, string topic);
        public static void rcvGlobalInfoRemoteAsyncCallBack(IAsyncResult ar)
        {
            rcvGlobalInfoAsync del = (rcvGlobalInfoAsync)((AsyncResult)ar).AsyncDelegate;
            del.EndInvoke(ar);
            return;
        }

        public void rcvGlobalInfo(int seqNb, string topic)
        {
            rcvGlobalInfoAsync propagateDelegate = new rcvGlobalInfoAsync(broker.rcvGlobalInfo);
            AsyncCallback RemoteCallback = new AsyncCallback(BrokerToBrokerServices.rcvGlobalInfoRemoteAsyncCallBack);
            IAsyncResult RemAr = propagateDelegate.BeginInvoke(seqNb, topic, rcvGlobalInfoRemoteAsyncCallBack, null);
        }

        public delegate void heartBeatAsync(string url);
        public static void heartBeatAsyncAsyncCallBack(IAsyncResult ar)
        {
            heartBeatAsync del = (heartBeatAsync)((AsyncResult)ar).AsyncDelegate;
            del.EndInvoke(ar);
            return;
        }
        public void sendHeartBeat(string url)
        {
            heartBeatAsync propagateDelegate = new heartBeatAsync(broker.sendHeartBeat);
            AsyncCallback RemoteCallback = new AsyncCallback(BrokerToBrokerServices.heartBeatAsyncAsyncCallBack);
            IAsyncResult RemAr = propagateDelegate.BeginInvoke(url, heartBeatAsyncAsyncCallBack, null);
        
        }
    }
}
