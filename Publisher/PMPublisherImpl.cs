using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTypes;
using CommonTypesPM;
using System.Threading;

namespace Publisher
{
    class PMPublisherImpl : MarshalByRefObject, PMPublisher
    {
        private BrokerInterface broker;
        private bool freezeFlag = false;
        private string processName;

        public PMPublisherImpl(BrokerInterface broker, string processName)
        {
            this.broker = broker;
            this.processName = processName;
        }

        public void crash()
        {
            System.Environment.Exit(1);
        }

        public void freeze()
        {
            freezeFlag = true;
            //Monitor.Wait(broker);
        }

        public void publish(int number, string topic, int interval)
        {
            for(int i = 0; i < number; i++)
            {
                Event e = new Event(topic, String.Concat(processName + " :", i));
                broker.publish(e);
                Thread.Sleep(interval);
            }
        }

        public void status()
        {
            
            throw new NotImplementedException();
        }

        public void unfreeze()
        {
            freezeFlag = false;
            //Monitor.Pulse(broker);
        }
    }
}
