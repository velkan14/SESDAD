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
        string brokerURL1, brokerURL2;
        private bool freezeFlag = false;
        private string processName;
        private int eventContent = 0;
        private NotificationReceiver pm;

        internal void changeBroker(string newUrl)
        {
            broker = (BrokerPublishInterface)Activator.GetObject(typeof(BrokerPublishInterface), newUrl + "P");
        }

        private int numberFreezes = 0;
        public delegate void Notifier(string s);

        AutoResetEvent freezeEvent = new AutoResetEvent(false);

        public Publisher(BrokerPublishInterface broker, NotificationReceiver pm, string processName, string brokerURL1, string brokerURL2)
        {
            this.broker = broker;
            this.processName = processName;
            this.pm = pm;
            this.brokerURL1 = brokerURL1;
            this.brokerURL2 = brokerURL2;
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
        }

        public void publish(int number, string topic, int interval)
        {
            //Notifier n = new Notifier(pm.notify);
            
            for (int i = 0; i < number; i++)
            {
                if (freezeFlag)
                {
                    numberFreezes++;
                    freezeEvent.WaitOne();
                }
                lock (this)
                {
                    Event e = new Event(processName, topic, processName + ": " + eventContent.ToString(), eventContent);
                    broker.publishEvent(e);
                    string notification = String.Concat("PubEvent " + processName + ", " + processName + ", " + topic + ", ", eventContent);
                    Console.WriteLine(notification);
                    //n.BeginInvoke(notification, null, null);
                    pm.notify(notification);
                    eventContent++;
                    Thread.Sleep(interval);
                }
            }
        }

        public void status()
        {
            if(freezeFlag) Console.WriteLine(String.Concat("I'm freezed and my sequence number is: ", eventContent));
            else Console.WriteLine(String.Concat("My sequence number is: ", eventContent));
        }
        
        public void unfreeze()
        {
            lock (this)
            {
                freezeFlag = false;
            }
            for (int i = 0; i < numberFreezes; i++)
                freezeEvent.Set();
            numberFreezes = 0;
        }
    }
}
