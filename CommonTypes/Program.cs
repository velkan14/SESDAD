using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace CommonTypes
{
    public interface SubscriberInterface{
        //classe para o evento??
        void deliverToSub(Event newEvent);

    }

    public interface PublisherInterface
    {
        void publishEvent(Event newEvent);
    }

    public interface EventReceiver
    {

    }

    public interface BrokerInterface{
        void subscribe(string topic, string subscriberURL);
        void unsubscribe(string topic);
        void publish(Event newEvent);        
    }

    [Serializable]
    public class Event{
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
