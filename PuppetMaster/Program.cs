using System;
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
            List<Site> sites = new List<Site>();
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
                                Site sitio;
                                string sitename = splitedConfig[1];
                                string parentName = splitedConfig[3];
                                sitio = new Site(sitename, findSiteByName(sites, parentName));        
                                sites.Add(sitio); 
                                break;
                            case 1:
                                //Process \\w+ Is publisher On \\w+ URL \\w+
                                string ipPM = splitedConfig[7].Split(':')[1].Split('/')[2];
                                string processName = splitedConfig[1];
                                string site = splitedConfig[5];
                                string publisherUrl = splitedConfig[7];
                                string brokerURL = findSiteByName(sites, site).BrokerOnSiteURL;

                                PMInterface obj = (PMInterface)Activator.GetObject(typeof(PMInterface),
                                    "tcp://" + ipPM + ":8086/PMInterface");
                                obj.createPublisher(processName, publisherUrl, brokerURL);
                                break;
                            case 2:
                                //Process \\w + Is subscriber On \\w + URL \\w +
                                ipPM = splitedConfig[7].Split(':')[1].Split('/')[2];
                                brokerURL = findSiteByName(sites, splitedConfig[5]).BrokerOnSiteURL;
                                obj = (PMInterface)Activator.GetObject(typeof(PMInterface),
                                    "tcp://" + ipPM + ":8086/PMInterface");
                                obj.createSubscriber(splitedConfig[1], splitedConfig[7], brokerURL);
                                break;
                            case 3:
                                //Process \\w+ Is broker On \\w+ URL \\w+
                                ipPM = splitedConfig[7].Split(':')[1].Split('/')[2];
                                obj = (PMInterface)Activator.GetObject(typeof(PMInterface), 
                                    "tcp://"+ipPM+":8086/PMInterface");
                                obj.createBroker(splitedConfig[1], splitedConfig[7], routingPolicy, ordering);
                                //adicionar ao site o url do broker para que os outros processos saibam a quem se ligar
                                Site tmpSite = findSiteByName(sites, splitedConfig[5]);
                                tmpSite.BrokerOnSiteURL = splitedConfig[7];
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
                                //Subscriber \\w+ Subscribe [\\w/]+
                                Console.WriteLine(words[1]);
                                Console.WriteLine(words[3]);
                                break;
                            case 2:
                                //Subscriber \\w+ Unsubscribe [\\w/]+
                                Console.WriteLine(words[1]);
                                Console.WriteLine(words[3]);
                                break;
                            case 3:
                                //Publisher \\w+ Publish \\d+ Ontopic [\\w/]+ Interval \\d+
                                Console.WriteLine(words[1]);
                                Console.WriteLine(words[3]);
                                Console.WriteLine(words[5]);
                                Console.WriteLine(words[7]);
                                break;
                            case 4:
                                //Status
                                Console.WriteLine(words[0]);
                                break;
                            case 5:
                                //Crash \\w+
                                Console.WriteLine(words[1]);
                                break;
                            case 6:
                                //Freeze \\w+
                                Console.WriteLine(words[1]);
                                break;
                            case 7:
                                //Unfreeze \\w+
                                Console.WriteLine(words[1]);
                                break;
                            case 8:
                                //Wait \\d+
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

        //espero que este static nao cause problemas. Yolo!
        public static Site findSiteByName(List<Site> sites, string sitename){
            if (!sitename.Equals("none")) {
                foreach (Site site in sites) {
                    if (site.Sitename.Equals(sitename))
                        return site;
                }
            }
            return null;
        }
    }
}
