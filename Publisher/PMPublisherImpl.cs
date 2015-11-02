using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTypes;
using CommonTypesPM;
using System.Threading;
using System.Runtime.Remoting.Messaging;

namespace Publisher
{
    class PMPublisherImpl : MarshalByRefObject, PMPublisher
    {

        Publisher pub;
        List<publisherAsyncPublish> asyncPublishList = new List<publisherAsyncPublish>();
        List<publisherAsyncDoSomething> asyncSomethingList = new List<publisherAsyncDoSomething>();
        public delegate void publisherAsyncPublish(int number, string topic, int interval);
        public delegate void publisherAsyncDoSomething();

        public static void OurRemoteAsyncCallBack(IAsyncResult ar)
        {
            // Alternative 2: Use the callback to get the return value
            publisherAsyncPublish del = (publisherAsyncPublish)((AsyncResult)ar).AsyncDelegate;
            del.EndInvoke(ar);

            return;
        }

        public PMPublisherImpl(BrokerPublishInterface broker, string processName)
        {
            pub = new Publisher(broker, processName);
        }

        public void crash()
        {
            publisherAsyncDoSomething RemoteDel = new publisherAsyncDoSomething(pub.crash);
            AsyncCallback RemoteCallback = new AsyncCallback(PMPublisherImpl.OurRemoteAsyncCallBack);
            IAsyncResult RemAr = RemoteDel.BeginInvoke(RemoteCallback, null);
        }

        public void freeze()
        {
            /*publisherAsyncDoSomething RemoteDel = new publisherAsyncDoSomething(pub.freeze);
            AsyncCallback RemoteCallback = new AsyncCallback(PMPublisherImpl.OurRemoteAsyncCallBack);
            IAsyncResult RemAr = RemoteDel.BeginInvoke(RemoteCallback, null);*/
            //Monitor.Wait(broker);
        }

        public void publish(int number, string topic, int interval)
        {
            publisherAsyncPublish RemoteDel = new publisherAsyncPublish(pub.publish);
            // Create delegate to local callback
            AsyncCallback RemoteCallback = new AsyncCallback(PMPublisherImpl.OurRemoteAsyncCallBack);
            // Call remote method
            IAsyncResult RemAr = RemoteDel.BeginInvoke(number, topic, interval, RemoteCallback, null);
            asyncPublishList.Add(RemoteDel);
        }

        public void status()
        {
            publisherAsyncDoSomething RemoteDel = new publisherAsyncDoSomething(pub.status);

            AsyncCallback RemoteCallback = new AsyncCallback(PMPublisherImpl.OurRemoteAsyncCallBack);
            IAsyncResult RemAr = RemoteDel.BeginInvoke(RemoteCallback, null);
            
        }

        public void unfreeze()
        {
            /*publisherAsyncDoSomething RemoteDel = new publisherAsyncDoSomething(pub.unfreeze);
            AsyncCallback RemoteCallback = new AsyncCallback(PMPublisherImpl.OurRemoteAsyncCallBack);
            IAsyncResult RemAr = RemoteDel.BeginInvoke(RemoteCallback, null);*/

            //Monitor.Pulse(broker);
        }
    }
}
