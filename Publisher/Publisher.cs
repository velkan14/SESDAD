using CommonTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Publisher
{
    class Publisher
    {
        private BrokerPublishInterface broker;
        private bool freezeFlag = false;
        private string processName;
        private int eventContent = 0;

        public Publisher(BrokerPublishInterface broker, string processName)
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
            lock (this)
            {
                freezeFlag = true;
            }
            //Monitor.Wait(this);
        }

        public void publish(int number, string topic, int interval)
        {
            for (int i = 0; i < number; i++)
            {
                Event e = new Event(topic, String.Concat(processName + " :", eventContent++));
                broker.publishEvent(e);
                Thread.Sleep(interval);
            }
        }

        public void status()
        {
            throw new NotImplementedException();
        }

        public void unfreeze()
        {
            lock (this)
            {
                freezeFlag = false;
            }

            //Monitor.Pulse(broker);
        }
    }
}
