using CommonTypesPM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuppetMaster
{
    class NotificationReceiverImpl : MarshalByRefObject, NotificationReceiver
    {
        public delegate void Log(string s);

        PM pm;

        public NotificationReceiverImpl(PM pm) {
            this.pm = pm;
        }

        public void publishNotification()
        {
            throw new NotImplementedException();
        }

        public void forwardNotification()
        {
            throw new NotImplementedException();
        }

        public void receiveNotification()
        {
            throw new NotImplementedException();
        }

        public void notify(string notification)
        {
            Console.WriteLine(notification);
            pm.log(notification);
            /*Log RemoteDel = new Log(pm.log);
            RemoteDel.BeginInvoke(notification, null, null);*/
        }
    }
}
