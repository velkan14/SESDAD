using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTypes;

namespace Broker
{
    class SubscriberImpl : MarshalByRefObject, SubscriberInterface
    {
        public void deliverToSub(Event newEvent)
        {
            throw new NotImplementedException();
        }
    }
}
