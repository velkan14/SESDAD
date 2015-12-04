using CommonTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Publisher
{
    class PublisherImpl : MarshalByRefObject, PublisherInterface
    {
        Publisher p;
        public PublisherImpl(Publisher p)
        {
            this.p = p;
        }
        public void changeBroker(string newUrl)
        {
            p.changeBroker(newUrl);
        }
    }
}
