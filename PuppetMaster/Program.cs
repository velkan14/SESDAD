﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using CommonTypesPM;

namespace PuppetMaster
{
    class Program
    {
        private const int PUPPETMASTER_IP = 8086;
        private const int MAX_CONFIG_NUM = 9;
        private const int MAX_COMMAND_NUM = 8;
        

        static void Main(string[] args)
        {
            string routingPolicy = "flooding";
            string ordering = "FIFO";

            TcpChannel channel = new TcpChannel(PUPPETMASTER_IP);
            ChannelServices.RegisterChannel(channel, false);
            PM pm = new PM();
            RemotingServices.Marshal(pm, "PMInterface",typeof(PMInterface));

            Console.WriteLine("Reading config file");
            //read configuration file
            string[] configFile = System.IO.File.ReadAllLines(@"..\..\..\config.txt");

            /**
            * Site sitename Parent sitename|none
            * Process processname Is publisher |subscriber |broker On sitename URL process-url
            * RoutingPolicy flooding|filter
            * Ordering NO|FIFO|TOTAL
            **/

            String[] configFormat = new String[MAX_CONFIG_NUM]{ "Site \\w+ Parent \\w+",
                                            "Process \\w+ Is publisher On \\w+ URL \\w+",
                                            "Process \\w+ Is subscriber On \\w+ URL \\w+",
                                            "Process \\w+ Is broker On \\w+ URL \\w+",
                                            "RoutingPolicy flooding",
                                            "RoutingPolicy filter",
                                            "Ordering NO",
                                            "Ordering FIFO",
                                            "Ordering TOTAL"
                                          };

            Match config;
            for (int configFileLine = 0; configFileLine < configFile.Length; configFileLine++)
            {
                for (int configFormatLine = 0; configFormatLine < MAX_CONFIG_NUM; configFormatLine++)
                {
                    config = Regex.Match(configFile[configFileLine], configFormat[configFormatLine]);
                    if (config.Success)
                    {

                        string[] splitedConfig = configFile[configFileLine].Split(' ');

                        switch (configFormatLine)
                        {
                            case 0:
                                //Site \\w + Parent \\w +
                                break;
                            case 1:
                                //Process \\w+ Is publisher On \\w+ URL \\w+
                                string ipPM = splitedConfig[7].Split(':')[1].Split('/')[2];
                                PMInterface obj = (PMInterface)Activator.GetObject(typeof(PMInterface),
                                    "tcp://" + ipPM + ":8086/PMInterface");
                                obj.createPublisher(splitedConfig[1], splitedConfig[5], splitedConfig[7]);
                                break;
                            case 2:
                                //Process \\w + Is subscriber On \\w + URL \\w +
                                ipPM = splitedConfig[7].Split(':')[1].Split('/')[2];
                                obj = (PMInterface)Activator.GetObject(typeof(PMInterface),
                                    "tcp://" + ipPM + ":8086/PMInterface");
                                obj.createSubscriber(splitedConfig[1], splitedConfig[5], splitedConfig[7]);
                                break;
                            case 3:
                                //Process \\w+ Is broker On \\w+ URL \\w+
                                ipPM = splitedConfig[7].Split(':')[1].Split('/')[2];
                                obj = (PMInterface)Activator.GetObject(typeof(PMInterface), 
                                    "tcp://"+ipPM+":8086/PMInterface");
                                obj.createBroker(splitedConfig[1], splitedConfig[5], splitedConfig[7], routingPolicy, ordering);
                                break;
                            case 4:
                                //RoutingPolicy flooding
                                routingPolicy = "flooding";
                                break;
                            case 5:
                                //RoutingPolicy filter
                                routingPolicy = "filter";
                                break;
                            case 6:
                                //Ordering NO
                                ordering = "NO";
                                break;
                            case 7:
                                //Ordering FIFO
                                ordering = "FIFO";
                                break;
                            case 8:
                                //Ordering TOTAL
                                ordering = "TOTAL";
                                break;
                            default:
                                Console.WriteLine("Default case");
                                break;
                        }

                    }
                }
            }


            Console.WriteLine("File loaded");
            Console.WriteLine("You can enter the commands");

            /*
            Subscriber processname Subscribe topicname
            Subscriber processname Unsubscribe topicname
            Publisher processname Publish numberofevents Ontopic topicname Interval xms
            Status
            Crash processname
            Freeze processname
            Unfreeze processname
            Wait xms
            **/



            String[] pattern = new String[MAX_COMMAND_NUM]{ "Subscriber \\w+ Subscribe [\\w/]+",
                                            "Subscriber \\w+ Unsubscribe [\\w/]+",
                                            "Publisher \\w+ Publish \\d+ Ontopic [\\w/]+ Interval \\d+",
                                            "Status",
                                            "Crash \\w+",
                                            "Freeze \\w+",
                                            "Unfreeze \\w+",
                                            "Wait \\d+"
                                          };
            String input = "";
            Match match;

            while (!input.Equals("Exit"))
            {
                input = System.Console.ReadLine();
                for (int commandNumber = 0; commandNumber < MAX_COMMAND_NUM; commandNumber++)
                {
                    match = Regex.Match(input, pattern[commandNumber]);
                    if (match.Success)
                    {

                        string[] words = match.Captures[0].Value.Split(' ');

                        switch (commandNumber + 1)
                        {
                            case 1:
                                Console.WriteLine(words[1]);
                                Console.WriteLine(words[3]);
                                break;
                            case 2:
                                Console.WriteLine(words[1]);
                                Console.WriteLine(words[3]);
                                break;
                            case 3:
                                Console.WriteLine(words[1]);
                                Console.WriteLine(words[3]);
                                Console.WriteLine(words[5]);
                                Console.WriteLine(words[7]);
                                break;
                            case 4:
                                Console.WriteLine(words[0]);
                                break;
                            case 5:
                                Console.WriteLine(words[1]);
                                break;
                            case 6:
                                Console.WriteLine(words[1]);
                                break;
                            case 7:
                                Console.WriteLine(words[1]);
                                break;
                            case 8:
                                Console.WriteLine(words[1]);
                                break;
                            default:
                                Console.WriteLine("Default case");
                                break;
                        }

                    }
                }
            }

        }
    }
}
