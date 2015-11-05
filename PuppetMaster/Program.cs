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
using System.IO;
using System.Threading;

namespace PuppetMaster
{
    class Program
    {
        private const int PUPPETMASTER_IP = 8086;
        private const int MAX_CONFIG_NUM = 11;
        private const int MAX_COMMAND_NUM = 8;

        public delegate void AsyncVoid();
        public delegate void AsyncString(string s);
        public delegate void AsyncIntStringInt(int i1, string s2, int i3);

        static void Main(string[] args)
        {
            string routingPolicy = "flooding";
            string ordering = "FIFO";
            string loggingLevel = "light";
            List<Site> sites = new List<Site>();
            Dictionary<string, PMPublisher> publisherDict = new Dictionary<string, PMPublisher>();
            Dictionary<string, PMSubscriber> subscriberDict = new Dictionary<string, PMSubscriber>();
            Dictionary<string, PMBroker> brokerDict = new Dictionary<string, PMBroker>();

            

            TcpChannel channel = new TcpChannel(PUPPETMASTER_IP);
            ChannelServices.RegisterChannel(channel, false);
            PM pm = new PM();
            RemotingServices.Marshal(pm, "PMInterface",typeof(PMInterface));

            NotificationReceiverImpl notifier = new NotificationReceiverImpl(pm);
            RemotingServices.Marshal(notifier, "PMNotifier", typeof(NotificationReceiver));
            /**
            * Site sitename Parent sitename|none
            * Process processname Is publisher |subscriber |broker On sitename URL process-url
            * RoutingPolicy flooding|filter
            * Ordering NO|FIFO|TOTAL
            **/

            //empty log file
            File.WriteAllText(@"..\..\..\log.txt", String.Empty);

            String[] configFormat = new String[MAX_CONFIG_NUM]{ "Site \\w+ Parent \\w+",
                                            "Process \\w+ Is publisher On \\w+ URL \\w+",
                                            "Process \\w+ Is subscriber On \\w+ URL \\w+",
                                            "Process \\w+ Is broker On \\w+ URL \\w+",
                                            "RoutingPolicy flooding",
                                            "RoutingPolicy filter",
                                            "Ordering NO",
                                            "Ordering FIFO",
                                            "Ordering TOTAL",
                                            "LoggingLevel full",
                                            "LoggingLevel light"
                                          };

            Console.WriteLine("Loading config file");
            try {
                //read configuration file
                string[] configFile = System.IO.File.ReadAllLines(@"..\..\..\config.txt");

                Match config;
                for (int fileLine = 0; fileLine < configFile.Length; fileLine++)
                {
                    for (int configFormatLine = 0; configFormatLine < MAX_CONFIG_NUM; configFormatLine++)
                    {
                        config = Regex.Match(configFile[fileLine], configFormat[configFormatLine]);
                        if (config.Success)
                        {

                            string[] splitedConfig = configFile[fileLine].Split(' ');

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
                                    obj.createPublisher(processName, URL, brokerURL, "tcp://" + ipPM + ":8086/PMNotifier");

                                    publisherDict.Add(processName, (PMPublisher)Activator.GetObject(typeof(PMPublisher),
                                        URL + "PM"));
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
                                    obj.createSubscriber(processName, URL, brokerURL, "tcp://" + ipPM + ":8086/PMNotifier");
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
                                        "tcp://" + ipPM + ":8086/PMInterface");
                                    obj.createBroker(processName, URL, routingPolicy, ordering);
                                    //adicionar ao site o url do broker para que os outros processos saibam a quem se ligar
                                    Site tmpSite = findSiteByName(sites, site);
                                    tmpSite.BrokerOnSiteURL = URL;
                                    PMBroker broker = (PMBroker)Activator.GetObject(typeof(PMBroker),
                                        URL + "PM");
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
                                case 9:
                                    //LoggingLevel full
                                    loggingLevel = "full";
                                    break;
                                case 10:
                                    //LoggingLevel light
                                    loggingLevel = "light";
                                    break;
                                default:
                                    Console.WriteLine("Default case");
                                    break;
                            }

                        }
                    }
                }
            } catch(FileNotFoundException e){
                Console.WriteLine("Config file NOT loaded.");
            }

            Console.WriteLine("Loading script file");
            try {
                string[] scriptFile = System.IO.File.ReadAllLines(@"..\..\..\script.txt");
                for (int fileLine = 0; fileLine < scriptFile.Length; fileLine++)
                {
                    readInput(pm, scriptFile[fileLine], publisherDict, brokerDict, subscriberDict);
                }
            }
            catch(FileNotFoundException e) {
                Console.WriteLine("Script file NOT loaded.");
            }

            Console.WriteLine("Files loaded. You now enter commands");
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
            String input = "";

            while (!input.Equals("Exit"))
            {
                input = System.Console.ReadLine();
                readInput(pm, input, publisherDict, brokerDict, subscriberDict);
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

        public static void readInput(PM pm, string input, Dictionary<string, PMPublisher> publisherDict, Dictionary<string, PMBroker> brokerDict, Dictionary<string, PMSubscriber> subscriberDict)
        {
            String[] pattern = new String[MAX_COMMAND_NUM]{ "Subscriber \\w+ Subscribe [\\w/]+",
                                            "Subscriber \\w+ Unsubscribe [\\w/]+",
                                            "Publisher \\w+ Publish \\d+ Ontopic [\\w/]+ Interval \\d+",
                                            "Status",
                                            "Crash \\w+",
                                            "Freeze \\w+",
                                            "Unfreeze \\w+",
                                            "Wait \\d+"
                                          };
            Match match;
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
                            PMSubscriber sub;
                            subscriberDict.TryGetValue(words[1], out sub);
                            AsyncString a = new AsyncString(sub.subscribe);
                            a.BeginInvoke(words[3], null, null);
                            //sub.subscribe(words[3]);
                            pm.log(input);
                            break;
                        case 2:
                            //Subscriber \\w+ Unsubscribe [\\w/]+
                            subscriberDict.TryGetValue(words[1], out sub);
                            a = new AsyncString(sub.unsubscribe);
                            a.BeginInvoke(words[3], null, null);
                            //sub.unsubscribe(words[3]);
                            pm.log(input);
                            break;
                        case 3:
                            //Publisher \\w+ Publish \\d+ Ontopic [\\w/]+ Interval \\d+
                            PMPublisher pub;
                            publisherDict.TryGetValue(words[1], out pub);
                            AsyncIntStringInt b = new AsyncIntStringInt(pub.publish);
                            b.BeginInvoke(Int32.Parse(words[3]), words[5], Int32.Parse(words[7]), null, null);
                            //pub.publish(Int32.Parse(words[3]), words[5], Int32.Parse(words[7]));
                            pm.log(input);
                            break;
                        case 4:
                            //Status
                            Console.WriteLine(words[0]);
                            
                            foreach (KeyValuePair<string, PMPublisher> tmp in publisherDict)
                            {
                                AsyncVoid c = new AsyncVoid(tmp.Value.status);
                                c.BeginInvoke(null, null);
                                //tmp.Value.status();
                            }
                            foreach (KeyValuePair<string, PMSubscriber> tmp in subscriberDict)
                            {
                                AsyncVoid c = new AsyncVoid(tmp.Value.status);
                                c.BeginInvoke(null, null);
                                //tmp.Value.status();
                            }
                            foreach (KeyValuePair<string, PMBroker> tmp in brokerDict)
                            {
                                AsyncVoid c = new AsyncVoid(tmp.Value.status);
                                c.BeginInvoke(null, null);
                                //tmp.Value.status();
                            }
                            
                            pm.log(input);
                            break;
                        case 5:
                            //Crash \\w+
                            string processName = words[1];
                            if (publisherDict.ContainsKey(processName))
                            {
                                if (publisherDict.TryGetValue(words[1], out pub))
                                {
                                    //pub.crash();
                                    AsyncVoid c = new AsyncVoid(pub.crash);
                                    c.BeginInvoke(null, null);
                                    publisherDict.Remove(words[1]);
                                }
                            }
                            else if (subscriberDict.ContainsKey(processName))
                            {
                                if (subscriberDict.TryGetValue(words[1], out sub))
                                {
                                    //sub.crash();
                                    AsyncVoid c = new AsyncVoid(sub.crash);
                                    c.BeginInvoke(null, null);
                                    subscriberDict.Remove(words[1]);
                                }
                            }
                            else if (brokerDict.ContainsKey(processName))
                            {
                                PMBroker brk;
                                if (brokerDict.TryGetValue(words[1], out brk))
                                {
                                    //brk.crash();
                                    AsyncVoid c = new AsyncVoid(brk.crash);
                                    c.BeginInvoke(null, null);
                                    brokerDict.Remove(words[1]);
                                }
                            }
                            pm.log(input);
                            break;
                        case 6:
                            //Freeze \\w+
                            processName = words[1];
                            if (publisherDict.ContainsKey(processName))
                            {
                                if (publisherDict.TryGetValue(words[1], out pub))
                                {
                                    //pub.freeze();
                                    AsyncVoid c = new AsyncVoid(pub.freeze);
                                    c.BeginInvoke(null, null);
                                }
                            }
                            else if (subscriberDict.ContainsKey(processName))
                            {

                                if (subscriberDict.TryGetValue(words[1], out sub))
                                {
                                    //sub.freeze();
                                    AsyncVoid c = new AsyncVoid(sub.freeze);
                                    c.BeginInvoke(null, null);
                                }
                            }
                            else if (brokerDict.ContainsKey(processName))
                            {
                                PMBroker brk;
                                if (brokerDict.TryGetValue(words[1], out brk))
                                {
                                    //brk.freeze();
                                    AsyncVoid c = new AsyncVoid(brk.freeze);
                                    c.BeginInvoke(null, null);
                                }
                            }
                            pm.log(input);
                            break;
                        case 7:
                            //Unfreeze \\w+
                            processName = words[1];
                            if (publisherDict.ContainsKey(processName))
                            {
                                if (publisherDict.TryGetValue(words[1], out pub))
                                {
                                    //pub.freeze();
                                    AsyncVoid c = new AsyncVoid(pub.unfreeze);
                                    c.BeginInvoke(null, null);
                                }
                            }
                            else if (subscriberDict.ContainsKey(processName))
                            {

                                if (subscriberDict.TryGetValue(words[1], out sub))
                                {
                                    //sub.freeze();
                                    AsyncVoid c = new AsyncVoid(sub.unfreeze);
                                    c.BeginInvoke(null, null);
                                }
                            }
                            else if (brokerDict.ContainsKey(processName))
                            {
                                PMBroker brk;
                                if (brokerDict.TryGetValue(words[1], out brk))
                                {
                                    //brk.freeze();
                                    AsyncVoid c = new AsyncVoid(brk.unfreeze);
                                    c.BeginInvoke(null, null);
                                }
                            }
                            pm.log(input);
                            break;
                        case 8:
                            //Wait \\d+
                            //Console.WriteLine(words[1]);
                            Thread.Sleep(Int32.Parse(words[1]));
                            pm.log(input);
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
