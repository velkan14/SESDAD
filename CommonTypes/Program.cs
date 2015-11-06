using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace CommonTypes
{
    public interface SubscriberInterface {
        void deliverToSub(Event evt);
        string getURL();
    }

    public interface BrokerPublishInterface {
        void publishEvent(Event newEvent);
    }

    public interface BrokerToBrokerInterface {
        void forwardEvent(Event evt);
        void forwardInterest(string url, string topic);
        void forwardDisinterest(string url, string topic);
        string getURL();
    }

    public interface BrokerSubscribeInterface {
        void subscribe(string topic, string subscriberURL);
        void unsubscribe(string topics, string subscriberURL);       
    }

    [Serializable]
    public class Event {
        private string topic;
        private string content;
        private string publisherName;
        private int msgNumber;

        public Event(string publisherName, string topic, string content, int msgNumber) {
            this.publisherName = publisherName;
            this.topic = topic;
            this.content = content;
            this.msgNumber = msgNumber;
        }

        public string Topic {
            get { return topic; }
            set { topic = value; }
        }

        public string PublisherName
        {
            get { return publisherName; }
            set { publisherName = value; }
        }

        public string Content {
            get { return content; }
            set { content = value; }
        }

        public int MsgNumber {
            get { return msgNumber; }
        }

        public static bool operator ==(Event a, Event b)
        {
            return a.content.Equals(b.Content) && a.Topic.Equals(b.Topic) && a.PublisherName.Equals(b.publisherName);
        }

        public static bool operator !=(Event a, Event b)
        {
            return !(a.content.Equals(b.Content) && a.Topic.Equals(b.Topic) && a.PublisherName.Equals(b.publisherName));
        }

        public override bool Equals(object obj)
        {
            Event e = obj as Event;

            return e.content.Equals(this.Content) && e.Topic.Equals(this.Topic) && e.PublisherName.Equals(this.publisherName);
        }

    }
    

}
