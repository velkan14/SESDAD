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
    class PM : MarshalByRefObject, PMInterface, NotificationReceiver
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
            using (StreamWriter w = File.AppendText(@"..\..\..\log.txt"))
            {
                w.Write("\r\nLog Entry : ");
                w.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
                        DateTime.Now.ToLongDateString());
                w.WriteLine("  :");
                w.WriteLine("  :{0}", logMessage);
                w.WriteLine("-------------------------------");
            }
        }

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

        
        public void status() { }

        public void publishNotification()
        {
            throw new NotImplementedException();
        }

        public void forwardNotification()
        {
            throw new NotImplementedException();
        }

        public void receiveNotification()
        {
            throw new NotImplementedException();
        }
    }
}
