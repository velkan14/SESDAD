using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTypesPM;
using System.Diagnostics;

namespace PuppetMaster
{
    class PM : MarshalByRefObject, PMInterface
    {
        public void createSubscriber(string processName, string url, string urlBroker)
        {
            Process.Start(@"..\..\..\Subscriber\bin\Debug\subscriber.exe", processName + " " + url + " " + urlBroker);
        }
        public void createBroker(string processName, string url, string routing, string ordering)
        {
            Process.Start(@"..\..\..\Broker\bin\Debug\broker.exe", processName + " " + url +" "+ routing +" "+ ordering);
        }
        public void createPublisher(string processName, string url, string urlBroker)
        {
            Process.Start(@"..\..\..\Publisher\bin\Debug\publisher.exe", processName + " " + url + " " + urlBroker);
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
