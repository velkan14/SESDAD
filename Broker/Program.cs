﻿using System;
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
            string processName = args[0];
            string url = args[1];
            string routing = args[2];
            string ordering = args[3];
            string port = url.Split(':')[2].Split('/')[0];

            Console.WriteLine("Name: "+ processName+"; Url: " +url+ "; \n\rRouting: " +routing+"; Ordering: " + ordering); 

           
            TcpChannel channel = new TcpChannel(Int32.Parse(port));
            ChannelServices.RegisterChannel(channel, false);
            BrokerServices brk = new BrokerServices();
            RemotingServices.Marshal(brk, "BrokerServices", typeof(BrokerServices));

            Console.WriteLine("New broker listening at " + url);
            System.Console.WriteLine("Press <enter> to terminate Broker...");
            System.Console.ReadLine();
        }   
    }

    

}
