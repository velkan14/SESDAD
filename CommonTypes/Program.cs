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
        void forwardEvent(string url, Event evt);
        void forwardInterest(string url, string topic);
        void forwardDisinterest(string url, string topic);
        string getURL();
        void reqSequence(string url, Event evt);
        void rcvSeqNumber(int seqNumber, Event evt);
        void rcvGlobalInfo(int seqNb, string topic);
        void sendHeartBeat(string url);
        string findRootNode();
        void publishEventRep(Event evt);
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
        private bool order;
        private int seq;

        public Event(string publisherName, string topic, string content, int msgNumber) {
            this.publisherName = publisherName;
            this.topic = topic;
            this.content = content;
            this.msgNumber = msgNumber;
            this.order = false;
        }

        public bool Order
        {
            get { return order; }
            set { order = value; }
        }

        public int Seq
        {
            get { return seq; }
            set { seq = value; }
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
            set { msgNumber = value; }
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

            return e.content.Equals(this.Content) && e.Topic.Equals(this.Topic) && e.PublisherName.Equals(this.publisherName) && e.Order==this.Order;
        }

    }

    public class SameSubscriberComparer : EqualityComparer<SubscriberInterface>
    {
        public override bool Equals(SubscriberInterface s1, SubscriberInterface s2)
        {
            return s1.getURL() == s2.getURL();
        }


        public override int GetHashCode(SubscriberInterface s)
        {
            return base.GetHashCode();
        }
    }

    public class SameBrokerComparer : IEqualityComparer<BrokerToBrokerInterface>
    {
        public int GetHashCode(BrokerToBrokerInterface co)
        {
            if (co == null)
            {
                return 0;
            }
            return co.getURL().GetHashCode();
        }

        public bool Equals(BrokerToBrokerInterface x1, BrokerToBrokerInterface x2)
        {
            if (object.ReferenceEquals(x1, x2))
            {
                return true;
            }
            if (object.ReferenceEquals(x1, null) ||
                object.ReferenceEquals(x2, null))
            {
                return false;
            }
            return x1.getURL() == x2.getURL();
        }
    }

    public class SameEventTOTALComparer : IEqualityComparer<Event>
    {
        public int GetHashCode(Event e)
        {
            int hash = 13;
            hash = (hash * 7) + e.Topic.GetHashCode();
            hash = (hash * 7) + e.PublisherName.GetHashCode();
            return hash;
        }

        public bool Equals(Event x1, Event x2)
        {
            if (object.ReferenceEquals(x1, x2))
            {
                return true;
            }
            if (object.ReferenceEquals(x1, null) ||
                object.ReferenceEquals(x2, null))
            {
                return false;
            }

            return x1.Content.Equals(x2.Content) && x1.Topic.Equals(x2.Topic) && x1.PublisherName.Equals(x2.PublisherName);
        }
    }



}
