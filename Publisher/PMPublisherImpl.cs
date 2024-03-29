﻿using System;
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
        public delegate void publisherAsyncPublish(int number, string topic, int interval);
        public delegate void publisherAsyncDoSomething();

        public PMPublisherImpl(Publisher pub)
        {
            this.pub = pub;
        }

        public void crash()
        {
            publisherAsyncDoSomething RemoteDel = new publisherAsyncDoSomething(pub.crash);
            RemoteDel.BeginInvoke(null, null);
        }

        public void freeze()
        {
            publisherAsyncDoSomething RemoteDel = new publisherAsyncDoSomething(pub.freeze);
            IAsyncResult RemAr = RemoteDel.BeginInvoke(null, null);
            //Monitor.Wait(broker);
        }

        public void publish(int number, string topic, int interval)
        {
            publisherAsyncPublish RemoteDel = new publisherAsyncPublish(pub.publish);
            RemoteDel.BeginInvoke(number, topic, interval, null, null);
        }

        public void status()
        {
            publisherAsyncDoSomething RemoteDel = new publisherAsyncDoSomething(pub.status);
            RemoteDel.BeginInvoke(null, null);
            
        }

        public void unfreeze()
        {
            /*publisherAsyncDoSomething RemoteDel = new publisherAsyncDoSomething(pub.unfreeze);
            AsyncCallback RemoteCallback = new AsyncCallback(PMPublisherImpl.OurRemoteAsyncCallBack);
            IAsyncResult RemAr = RemoteDel.BeginInvoke(RemoteCallback, null);*/
            
            pub.unfreeze();
        }
    }
}
