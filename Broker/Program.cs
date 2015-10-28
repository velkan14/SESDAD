using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTypes;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace Broker
{
    class Broker
    {
       
        static void Main(string[] args)
        {
            //deve receber o porto nos argumentos. Hope so...
            //tcpchannel dadURL routingPolicy processURL
           
            // TcpChannel channel = new TcpChannel(Int32.Parse(args[0]));
            TcpChannel channel = new TcpChannel(8086);
            ChannelServices.RegisterChannel(channel, false);
            BrokerServices brk = new BrokerServices();
            RemotingServices.Marshal(brk,
                "SonBroker",
                typeof(BrokerServices));
              
            Console.WriteLine("New broker listening at tcp://localhost:8086/Broker");  
            System.Console.WriteLine("Press <enter> to terminate Broker...");
            System.Console.ReadLine();
        }   
    }

    

}
