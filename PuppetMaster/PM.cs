using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTypesPM;

namespace PuppetMaster
{
    class PM : MarshalByRefObject, PMInterface
    {
        public void createSubscriber(String processName, int site, String url)
        {

        }
        public void createBroker(String processName, int site, String url)
        {

        }
        public void createPublisher(String processName, int site, String url)
        {

        }

        public void subscribe() { }
        public void unsubscribe() { }
        public void publish() { }
        public void status() { }
        public void crash() { }
        public void freeze() { }
        public void unfreeze() { }
        public void wait() { }
    }
}
