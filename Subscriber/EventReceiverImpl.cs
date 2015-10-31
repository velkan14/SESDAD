using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTypes;

namespace Publisher
{
    class EventReceiverImpl : MarshalByRefObject, SubscriberInterface
    {
        public void deliverToSub(Event evt)
        {
            throw new NotImplementedException();
        }
    }
}
