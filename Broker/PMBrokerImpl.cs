using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTypesPM;
using CommonTypes;

namespace Broker
{
    

    class PMBrokerImpl : MarshalByRefObject, PMBroker
    {
        Broker broker;

        public delegate void Add(string url);
        public delegate void Something();

        public PMBrokerImpl(Broker broker) {
            this.broker = broker;
        }
        public void addDad(string url)
        {
            Add delegated = new Add(broker.addDad);
            IAsyncResult RemAr = delegated.BeginInvoke(url, null, null);
        }

        public void addSon(string url)
        {
            Add delegated = new Add(broker.addSon);
            IAsyncResult RemAr = delegated.BeginInvoke(url, null, null);
        }

        public void crash()
        {
            Something delegated = new Something(broker.crash);
            IAsyncResult RemAr = delegated.BeginInvoke(null, null);
        }

        public void freeze()
        {
            throw new NotImplementedException();
        }

        public void status()
        {
            throw new NotImplementedException();
        }

        public void unfreeze()
        {
            throw new NotImplementedException();
        }
    }
}
