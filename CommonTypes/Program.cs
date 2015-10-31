using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace CommonTypes
{
    public interface SubscriberInterface {
        void deliverToSub(Event newEvent);
    }

    public interface BrokerPublishInterface {
        void publishEvent(Event newEvent);
    }

    public interface EventReceiver {

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

    }
    

}
