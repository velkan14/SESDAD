using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonTypesPM
{
    public interface PMInterface
    {
        void createSubscriber(string processName, string url, string brokerURL);
        void createBroker(string processName, string url, string routing, string ordering);
        void createPublisher(string processName, string url, string brokerURL);
        void status();
    }

    public interface NotificationReceiver
    {
        //PubEvent publisher-processname, publisher-processname, topicname, event-number
        void publishNotification();

        //BroEvent broker-processname, publisher-processname, topicname, event-number
        void forwardNotification();

        //SubEvent subscriber-processname, publisher-processname, topicname, event-number
        void receiveNotification();

        }

    public interface PMSubscriber
    {
        void subscribe(string topic);
        void unsubscribe(string topic);

        void status();
        void crash();
        void freeze();
        void unfreeze();
    }

    public interface PMPublisher
    {
        void publish(int number, string topic, int interval);

        void status();
        void crash();
        void freeze();
        void unfreeze();
    }

    public interface PMBroker
    {
        void addSon(string url);
        void addDad(string url);

        void status();
        void crash();
        void freeze();
        void unfreeze();
    }

}
