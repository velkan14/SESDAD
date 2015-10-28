using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonTypesPM
{
    public interface PMInterface
    {
        void createSubscriber(String processName, int site, String url);
        void createBroker(String processName, int site, String url);
        void createPublisher(String processName, int site, String url);
    }

    public interface NotificationReceiver
    {

    }

    public interface PMSubscriber
    {

    }

    public interface PMPublisher
    {

    }

    public interface PMBroker
    {

    }

}
