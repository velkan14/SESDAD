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
        List<BrokerToBrokerInterface> sons;
        List<BrokerToBrokerInterface> dad;
        public PMBrokerImpl(List<BrokerToBrokerInterface> dad, List<BrokerToBrokerInterface> sons) {
            this.sons = sons;
            this.dad = dad;

        }
        public void addDad(string url)
        {
            dad.Add((BrokerToBrokerInterface)Activator.GetObject(typeof(BrokerToBrokerInterface), url + "B"));
        }

        public void addSon(string url)
        {
            sons.Add((BrokerToBrokerInterface)Activator.GetObject(typeof(BrokerToBrokerInterface), url + "B"));
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
