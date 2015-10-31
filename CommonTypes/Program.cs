using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace CommonTypes
{
    public interface SubscriberInterface {
        void deliverToSub(Event evt);
    }

    public interface BrokerPublishInterface {
        void publishEvent(Event newEvent);
    }

    public interface BrokerToBrokerInterface {
        void forwardEvent(Event evt);
    }

    public interface BrokerSubscribeInterface {
        void subscribe(string topic, string subscriberURL);
        void unsubscribe(string topic);       
    }

    [Serializable]
    public class Event {
        private string topic;
        private string content;

        public Event(string topic, string content) {
            this.topic = topic;
            this.content = content;
        }

        public string Topic {
            get { return topic; }
            set { topic = value; }
        }

        public string Content {
            get { return content; }
            set { content = value; }
        }

        public static bool operator ==(Event a, Event b)
        {
            return a.content.Equals(b.Content) && a.Topic.Equals(b.Topic);
        }

        public static bool operator !=(Event a, Event b)
        {
            return a.content.Equals(b.Content) && a.Topic.Equals(b.Topic); ;
        }

    }
    

}
