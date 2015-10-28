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

        void subscribe() { }
        void unsubscribe() { }
        void publish() { }
        void status() { }
        void crash() { }
        void freeze() { }
        void unfreeze() { }
        void wait() { }
    }
}
