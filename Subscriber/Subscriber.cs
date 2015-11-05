using CommonTypes;
using CommonTypesPM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Subscriber
{
    class Subscriber : PMSubscriber, SubscriberInterface
    {

        BrokerSubscribeInterface broker;
        NotificationReceiver notifier;
        string processName;
        string myUrl;
        AutoResetEvent freezeEvent = new AutoResetEvent(false);
        private bool freezeFlag = false;
        private int numberFreezes = 0;

        public delegate void Notifier(string s);

        public Subscriber(BrokerSubscribeInterface broker, NotificationReceiver notifier, string processName, string url)
        {
            this.processName = processName;
            this.notifier = notifier;
            this.broker = broker;
            this.myUrl = url;
        }

        public void deliverToSub(Event evt)
        {
            if (freezeFlag)
            {
                lock(this) numberFreezes++;
                freezeEvent.WaitOne();
            }
            Notifier n = new Notifier(notifier.notify);
            string notification = "SubEvent " + processName + ", " + evt.PublisherName +", " + evt.Topic +", "+ evt.Content;
            Console.WriteLine(notification);
            n.BeginInvoke(notification, null, null);

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

        public void status()
        {
            throw new NotImplementedException();
        }

        public void subscribe(string topic)
        {
            broker.subscribe(topic, myUrl);
            Console.WriteLine("Subscribed topic " + topic);
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

        public void unsubscribe(string topic)
        {
            broker.unsubscribe(topic);
        }
    }
}
