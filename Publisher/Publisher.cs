﻿using CommonTypes;
using CommonTypesPM;
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
        private NotificationReceiver pm;

        public delegate void Notifier(string s);

        public Publisher(BrokerPublishInterface broker, NotificationReceiver pm, string processName)
        {
            this.broker = broker;
            this.processName = processName;
            this.pm = pm;
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
            Notifier n = new Notifier(pm.notify);
            lock (this)
            {
                for (int i = 0; i < number; i++)
                {
                    Event e = new Event(processName, topic, eventContent.ToString());
                    broker.publishEvent(e);
                    string notification = String.Concat("PubEvent " + processName + ", " + processName + ", " + topic + ", ", eventContent);
                    Console.WriteLine(notification);
                    n.BeginInvoke(notification, null, null);
                    //pm.notify(notification);
                    eventContent++;
                    Thread.Sleep(interval);
                }
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
