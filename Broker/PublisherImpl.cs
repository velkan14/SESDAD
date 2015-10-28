using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTypes;

namespace Broker
{
    class PublisherImpl : MarshalByRefObject, PublisherInterface
    {
        public void publishEvent(Event newEvent)
        {
            throw new NotImplementedException();
        }
    }
}
