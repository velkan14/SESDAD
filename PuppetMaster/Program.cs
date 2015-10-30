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
            Dictionary<string, PMPublisher> publisherDict = new Dictionary<string, PMPublisher>();
            Dictionary<string, PMSubscriber> subscriberDict = new Dictionary<string, PMSubscriber>();
            Dictionary<string, PMBroker> brokerDict = new Dictionary<string, PMBroker>();
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
                                string URL = splitedConfig[7];
                                string brokerURL = findSiteByName(sites, site).BrokerOnSiteURL;

                                PMInterface obj = (PMInterface)Activator.GetObject(typeof(PMInterface),
                                    "tcp://" + ipPM + ":8086/PMInterface");
                                obj.createPublisher(processName, URL, brokerURL);
                                
                                publisherDict.Add(processName, (PMPublisher)Activator.GetObject(typeof(PMPublisher),
                                    URL+"PM"));
                                break;
                            case 2:
                                //Process \\w + Is subscriber On \\w + URL \\w +
                                ipPM = splitedConfig[7].Split(':')[1].Split('/')[2];
                                processName = splitedConfig[1];
                                site = splitedConfig[5];
                                URL = splitedConfig[7];
                                brokerURL = findSiteByName(sites, site).BrokerOnSiteURL;
                                obj = (PMInterface)Activator.GetObject(typeof(PMInterface),
                                    "tcp://" + ipPM + ":8086/PMInterface");
                                obj.createSubscriber(processName, URL, brokerURL);
                                subscriberDict.Add(processName, (PMSubscriber)Activator.GetObject(typeof(PMSubscriber),
                                    URL + "PM"));
                                break;
                            case 3:
                                //Process \\w+ Is broker On \\w+ URL \\w+
                                ipPM = splitedConfig[7].Split(':')[1].Split('/')[2];
                                processName = splitedConfig[1];
                                site = splitedConfig[5];
                                URL = splitedConfig[7];
                                obj = (PMInterface)Activator.GetObject(typeof(PMInterface), 
                                    "tcp://"+ipPM+":8086/PMInterface");
                                obj.createBroker(processName, URL, routingPolicy, ordering);
                                //adicionar ao site o url do broker para que os outros processos saibam a quem se ligar
                                Site tmpSite = findSiteByName(sites, site);
                                tmpSite.BrokerOnSiteURL = URL;
                                PMBroker broker = (PMBroker)Activator.GetObject(typeof(PMBroker),
                                    URL+"PM");
                                brokerDict.Add(processName, broker);

                                if (!tmpSite.getDad().Equals("none"))
                                {
                                    PMBroker brokerDad = (PMBroker)Activator.GetObject(typeof(PMBroker),
                                        tmpSite.getDad() + "PM");
                                    brokerDad.addSon(URL);
                                    broker.addDad(tmpSite.getDad());
                                }
                                foreach (string son in tmpSite.getSonsBrokersURLs())
                                {
                                    broker.addSon(son);
                                }
                                
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
                                PMPublisher pub;
                                publisherDict.TryGetValue(words[1], out pub);
                                pub.publish(Int32.Parse(words[3]), words[5], Int32.Parse(words[7]));
                                break;
                            case 4:
                                //Status
                                Console.WriteLine(words[0]);
                                foreach (KeyValuePair<string, PMPublisher> tmp in publisherDict) {
                                    tmp.Value.status();
                                }
                                foreach (KeyValuePair<string, PMSubscriber> tmp in subscriberDict)
                                {
                                    tmp.Value.status();
                                }
                                foreach (KeyValuePair<string, PMBroker> tmp in brokerDict)
                                {
                                    tmp.Value.status();
                                }
                                break;
                            case 5:
                                //Crash \\w+
                                string processName = words[1];
                                if (publisherDict.ContainsKey(processName))
                                {
                                    publisherDict.TryGetValue(words[1], out pub);
                                    pub.crash();
                                } else if (subscriberDict.ContainsKey(processName))
                                {
                                    PMPublisher sub;
                                    publisherDict.TryGetValue(words[1], out sub);
                                    sub.crash();
                                }
                                else if (brokerDict.ContainsKey(processName))
                                {
                                    PMPublisher brk;
                                    publisherDict.TryGetValue(words[1], out brk);
                                    brk.crash();
                                }
                                break;
                            case 6:
                                //Freeze \\w+
                                processName = words[1];
                                if (publisherDict.ContainsKey(processName))
                                {
                                    publisherDict.TryGetValue(words[1], out pub);
                                    pub.freeze();
                                }
                                else if (subscriberDict.ContainsKey(processName))
                                {
                                    PMPublisher sub;
                                    publisherDict.TryGetValue(words[1], out sub);
                                    sub.freeze();
                                }
                                else if (brokerDict.ContainsKey(processName))
                                {
                                    PMPublisher brk;
                                    publisherDict.TryGetValue(words[1], out brk);
                                    brk.freeze();
                                }
                                break;
                            case 7:
                                //Unfreeze \\w+
                                processName = words[1];
                                if (publisherDict.ContainsKey(processName))
                                {
                                    publisherDict.TryGetValue(words[1], out pub);
                                    pub.unfreeze();
                                }
                                else if (subscriberDict.ContainsKey(processName))
                                {
                                    PMPublisher sub;
                                    publisherDict.TryGetValue(words[1], out sub);
                                    sub.unfreeze();
                                }
                                else if (brokerDict.ContainsKey(processName))
                                {
                                    PMPublisher brk;
                                    publisherDict.TryGetValue(words[1], out brk);
                                    brk.unfreeze();
                                }
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
