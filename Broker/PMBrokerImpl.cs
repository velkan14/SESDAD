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
        List<BrokerInterface> sons;
        BrokerInterface dad;
        public PMBrokerImpl(List<BrokerInterface> sons, BrokerInterface dad) {
            this.sons = sons;
            this.dad = dad;

        }
        public void addDad(string url)
        {
            dad = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), url);
        }

        public void addSon(string url)
        {
            sons.Add((BrokerInterface)Activator.GetObject(typeof(BrokerInterface), url));
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
