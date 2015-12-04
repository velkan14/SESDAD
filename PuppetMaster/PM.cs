using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTypesPM;
using System.Diagnostics;
using System.IO;

namespace PuppetMaster
{
    class PM : MarshalByRefObject, PMInterface
    {

        private string loggingLevel;

        //The publication of an event by a publisher should appear in the log as:
        //PubEvent publisher-processname, publisher-processname, topicname, event-number

        //The forwarding of an event by a broker should appear in the log as:
        //BroEvent broker-processname, publisher-processname, topicname, event-number

        //The delivery of an event to a subscriber should appear in the log as:
        //SubEvent subscriber-processname, publisher-processname, topicname, event-number


        public void log(string logMessage)
        {
            lock (this)
            {
                using (StreamWriter w = File.AppendText(@"..\..\..\log.txt"))
                {
                    w.WriteLine("{0} {1} : {2}", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString(), logMessage);
                }
            }
        }

        public void createSubscriber(string processName, string url, string urlBroker0, string urlBroker1, string urlBroker2, string pmURL)
        {
            Process.Start(@"..\..\..\Subscriber\bin\Debug\subscriber.exe", processName + " " + url + " " + urlBroker0 + " " + urlBroker1 + " " + urlBroker2 + " " + pmURL);
        }
        public void createBroker(string processName, string url, string routing, string ordering, string loggingLevel, string pmURL, int leaderCount)
        {
            Process.Start(@"..\..\..\Broker\bin\Debug\broker.exe", processName + " " + url +" "+ routing +" "+ ordering + " "+ loggingLevel +" " + pmURL + " " + leaderCount.ToString());
        }
        public void createPublisher(string processName, string url, string urlBroker0, string urlBroker1, string urlBroker2, string pmURL)
        {
            Process.Start(@"..\..\..\Publisher\bin\Debug\publisher.exe", processName + " " + url + " " + urlBroker0 + " " + urlBroker1 + " " + urlBroker2 + " " + pmURL);
        }

        
        public void status() { }

        
    }
}
