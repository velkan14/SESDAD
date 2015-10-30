using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTypesPM;

namespace Broker
{
    class PMBrokerImpl : MarshalByRefObject, PMBroker
    {
        public void addDad(string url)
        {
           // throw new NotImplementedException();
        }

        public void addSon(string url)
        {
            //throw new NotImplementedException();
        }

        public void crash()
        {
            throw new NotImplementedException();
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
