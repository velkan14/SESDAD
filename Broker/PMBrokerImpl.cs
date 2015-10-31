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
        List<BrokerSubscribeInterface> sons;
        BrokerSubscribeInterface dad;
        public PMBrokerImpl(List<BrokerSubscribeInterface> sons, BrokerSubscribeInterface dad) {
            this.sons = sons;
            this.dad = dad;

        }
        public void addDad(string url)
        {
            dad = (BrokerSubscribeInterface)Activator.GetObject(typeof(BrokerSubscribeInterface), url);
        }

        public void addSon(string url)
        {
            sons.Add((BrokerSubscribeInterface)Activator.GetObject(typeof(BrokerSubscribeInterface), url));
        }

        public void crash()
        {
            System.Environment.Exit(1);
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
