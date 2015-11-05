using CommonTypes;
using CommonTypesPM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Subscriber
{
    class Subscriber : PMSubscriber, SubscriberInterface
    {

        BrokerSubscribeInterface broker;
        NotificationReceiver notifier;
        string processName;
        string myUrl;

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
            Notifier n = new Notifier(notifier.notify);
            string notification = "SubEvent " + processName + ", " + evt.PublisherName +", " + evt.Topic +", "+ evt.Content;
            Console.WriteLine(notification);
            n.BeginInvoke(notification, null, null);

        }
        
        public void crash()
        {
            throw new NotImplementedException();
        }

        public void freeze()
        {
            throw new NotImplementedException();
        }

        public void status()
        {
            throw new NotImplementedException();
        }

        public void subscribe(string topic)
        {
            broker.subscribe(topic, myUrl);
        }

        public void unfreeze()
        {
            throw new NotImplementedException();
        }

        public void unsubscribe(string topic)
        {
            broker.unsubscribe(topic);
        }
    }
}
