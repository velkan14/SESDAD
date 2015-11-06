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
        private List<string> subscribedTopics = new List<string>();
        public delegate void Notifier(string s);

        public Subscriber(BrokerSubscribeInterface broker, NotificationReceiver notifier, string processName, string url)
        {
            this.processName = processName;
            this.notifier = notifier;
            this.broker = broker;
            this.myUrl = url;
        }

        public string getURL()
        {
            return myUrl;
        }

        public void deliverToSub(Event evt)
        {
            if (freezeFlag)
            {
                lock(this) numberFreezes++;
                freezeEvent.WaitOne();
            }
            //Notifier n = new Notifier(notifier.notify);
            string notification = "SubEvent " + processName + ", " + evt.PublisherName +", " + evt.Topic +", "+ evt.Content;
            Console.WriteLine(notification);
            //n.BeginInvoke(notification, null, null);
            notifier.notify(notification);
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
            Console.WriteLine("Subscribed Topics:");
            foreach (string s in subscribedTopics) Console.WriteLine("- " + s);
        }

        public void subscribe(string topic)
        {
            broker.subscribe(topic, myUrl);
            subscribedTopics.Add(topic);
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
            broker.unsubscribe(topic, myUrl);
            subscribedTopics.Remove(topic);
            Console.WriteLine("Unsubscribed topic " + topic);
        }
    }
}
