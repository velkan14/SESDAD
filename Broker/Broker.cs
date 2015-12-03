using CommonTypes;
using CommonTypesPM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Broker
{
    class Broker :  BrokerToBrokerInterface, PMBroker, BrokerSubscribeInterface, BrokerPublishInterface
    {
        string url;
        int initLastMsgNumber = -1; //porque comecamos a numerar as mensagens em 0...
        private int numberFreezes = 0;
        private bool freezeFlag = false;
        AutoResetEvent freezeEvent = new AutoResetEvent(false);
        int brokerID=1;
        int LiderID=1;

        List<BrokerToBrokerInterface> dad = new List<BrokerToBrokerInterface>();
        List<BrokerToBrokerInterface> sons = new List<BrokerToBrokerInterface>();
        List<BrokerToBrokerInterface> replicas = new List<BrokerToBrokerInterface>();
        Dictionary<string, List<SubscriberInterface>> subscribersByTopic = new Dictionary<string, List<SubscriberInterface>>();
        List<SubAux> subLastMsgReceived = new List<SubAux>();

        Dictionary<string, List<BrokerToBrokerInterface>> brokersByTopic = new Dictionary<string, List<BrokerToBrokerInterface>>();
        Dictionary<BrokerToBrokerInterface, List<string>> topicsProvidedByBroker = new Dictionary<BrokerToBrokerInterface, List<string>>();
        Dictionary<Tuple<string, string>, List<int>> filteringTable = new Dictionary<Tuple<string, string>, List<int>>();
        //estrutura para auxiliar a implementar comunicacao FIFO entre brokers.
        //<<Broker,Pub>,Lista das mensagens pendentes>    
        Dictionary<Tuple<string, string>, Tuple<int, List<Event>>> fifoBrokersMsgs = new Dictionary<Tuple<string, string>, Tuple<int, List<Event>>>();
        Dictionary<string,int> lastMsgNumberByPub = new Dictionary<string, int>();
        Dictionary<string, List<Event>> msgQueueByPub = new Dictionary<string, List<Event>>();
        Dictionary<string, HashSet<string>> topicsByPub = new Dictionary<string, HashSet<string>>();

        //to store the messages until they can by TO-delivered 
        Dictionary<Tuple<string, int>, Event> holdBackQueueByTopic = new Dictionary<Tuple<string, int>, Event>();
        //just for the sequencer
        Dictionary<string, int> seqNumberByTopic = new Dictionary<string, int>();
        BrokerToBrokerInterface root;
        string rootURL = null;
        Dictionary<int, string> globalInfo = new Dictionary<int, string>();
        Dictionary<string, List<int>> neglectedTotalTopicsBySub = new Dictionary<string, List<int>>();
        int seqNb = -1;
        Dictionary<Tuple<string, int>, int> seqNbByMessage = new Dictionary<Tuple<string, int>, int>();

        List<Event> events = new List<Event>();
        string routing, ordering, loggingLevel;
        string processName;
        NotificationReceiver pm;

        public Broker(NotificationReceiver pm, string processName, string routing, string ordering, string loggingLevel)
        {
            this.pm = pm;
            this.processName = processName;
            this.routing = routing;
            this.ordering = ordering;
            this.loggingLevel = loggingLevel;
        }

        public void setUrl(string url)
        {
            this.url = url;
        }

        public string getURL() { return url; }

        public bool isSubtopicOf(string subtopic, string topic)
        {
            string[] arrayTopicsSub = subtopic.Split('/');
            string[] arrayTopics = topic.Split('/');
            
            if (arrayTopicsSub.Length > arrayTopics.Length)
            {
                for (int i = 0; i < arrayTopics.Length; i++)
                {
                    if (!arrayTopicsSub[i].Equals(arrayTopics[i])) return false;
                }
                return true;
            }
            return false;
            
        }

        public bool assertPub(string pubURL, string topic)
        {
            HashSet<string> auxHst;
            if (topicsByPub.TryGetValue(pubURL, out auxHst))
            {
                return auxHst.Contains(topic);
            }
            return false;
        }
        //os topicos que um certo pub publica. É actualizada sempre que se recebe uma mensagem
        public void addToPubsByTopic(string pubURL, string msgTopic)
        {
            HashSet<string> auxHst;
            if (topicsByPub.TryGetValue(pubURL, out auxHst))
            {
                auxHst.Add(msgTopic);
            }
            else
            {
                topicsByPub.Add(pubURL, new HashSet<string>() { msgTopic });
            }
        }
        //verifica se o sub está subscrito a outros topicos para alem de msgTopic publicados por pubURL
        public bool assertSubscription(string pubURL, string msgTopic, string subURL)
        {
            foreach(KeyValuePair<string, List<SubscriberInterface>> entry in subscribersByTopic)
            {
                string topicKey = entry.Key;
                if (!(topicKey.Equals(msgTopic) || isSubtopicOf(msgTopic, topicKey)))
                {
                    if (assertPub(pubURL, msgTopic))
                    {
                        foreach (SubscriberInterface s in entry.Value)
                        {
                            if (s.getURL() == subURL)
                                return true;
                        }
                    }
                }
            }
            return false;
        }
        //ve se o sub esta subscrito a outros topicos para alem de msgTopic
        public bool assertTopicsSubbed(string subURL, string msgTopic)
        {
            foreach (KeyValuePair<string, List<SubscriberInterface>> entry in subscribersByTopic)
            {
                string topicKey = entry.Key;
                if (!(topicKey.Equals(msgTopic) || isSubtopicOf(msgTopic, topicKey)))
                {
                    foreach (SubscriberInterface s in entry.Value)
                    {
                        if (s.getURL() == subURL)
                            return true;
                    }    
                }
            }
            return false;
        }

        public void deliverToSub(string url, Event evt)
        {
            string topic;
            string pubURL = evt.PublisherName;
            int msgNumber = evt.MsgNumber;
            string msgTopic = evt.Topic;

            //a ideia subjacente é a de que quando um evento chega a um sub ele pode estar interessado nele por se tratar dum topico ou subtopico que queira
            //Mas tambem ha a possibilidade de apesar de nao estar interessado directamente nesse topico/subtopico (porque nao subscreveu)
            //precisa de ser avisado relativamente ao numero de sequencia da mensagem. Isto acontece quando essa mensagem vem de um publisher
            //que publica mensagens dos topicos que o sub quer
            //no entanto é preciso ter cuidado com certas subtilezas. A ordem das operações é
            //1. entregar a mensagem (ou arquivar) a todos os subs que a queiram (estão subscritos ao topico/subtopico)
            //2. registar aqueles que recebem (os que arquivam nao contam neste caso) a mensagem.
            //3. enviar a mensagem (so o numero de msg) a todos os subs que nao receberam a mensagem e que tenham interesse nessa mensagem
            //mensagens sao postas numa fila caso nao se tenha recebido a anterior
            if (ordering == "FIFO")
            {
                Console.WriteLine("******************************");
                Console.WriteLine("FIFOing IT OUT!");
                addToPubsByTopic(pubURL, msgTopic);
                HashSet<string> subsWhoGotMessage = new HashSet<string>();
                List<SubscriberInterface> subset = new List<SubscriberInterface>();
                subset = setOfTopics(msgTopic);
                foreach (SubscriberInterface y in subset)
                {
                    Console.WriteLine("subset member: " + y);
                }
                foreach (SubscriberInterface sub in subset)
                {
                    SubAux subAux = subLastMsgReceived.Find(o => o.Sub.getURL() == sub.getURL());
                    int nextLastMsgNumber = subAux.lastMsgNumber(pubURL) + 1;

                    Console.WriteLine("SUBSCRIBER: " + sub.getURL());
                    Console.WriteLine("PUBLISHER: " + pubURL);
                    Console.WriteLine("MSG TOPIC = " + msgTopic);
                    Console.WriteLine("Msg Number = " + evt.MsgNumber);
                    Console.WriteLine("nextLastMsgNumber = " + nextLastMsgNumber);
                    if (msgNumber == nextLastMsgNumber)
                    {
                        Console.WriteLine("entregar!!!!!!");
                        if (assertPrimary())
                        {
                            sub.deliverToSub(evt);
                        }
                        subAux.updateLastMsgNumber(pubURL);
                        flushMsgQueue(subAux, sub, pubURL);
                        //para nao entregarmos a mesma mensagem duas vezes caso o sub esteja subscrito ao topico e a um
                        //hipertopico
                        subsWhoGotMessage.Add(sub.getURL());
                    }
                    else
                    {
                        Console.WriteLine("arquivar!!!!");
                        subAux.addToQueue(evt);
                    }
                }

                subset = setNonTopics(msgTopic);
                foreach (SubscriberInterface ss in subset)
                {
                    //se ja recebeu nao interessa mais. Evitar que seja entregue a mesma msg duas vezes ao mesmo sub
                    if (!subsWhoGotMessage.Contains(ss.getURL()))
                    {
                        SubAux subAux = subLastMsgReceived.Find(o => o.Sub.getURL() == ss.getURL());
                        //verificar se o topico interessa ao sub
                        if (assertSubscription(pubURL, msgTopic, ss.getURL()))
                        {
                            int nextLastMsgNumber = subAux.lastMsgNumber(pubURL) + 1;
                            if (msgNumber == nextLastMsgNumber)
                            {
                                Console.WriteLine("Entrei mas nao sou topico!");
                                Console.WriteLine("******************************");
                                Console.WriteLine("SUBSCRIBER: " + ss.getURL());
                                Console.WriteLine("PUBLISHER: " + pubURL);
                                Console.WriteLine("MSG TOPIC = " + msgTopic);
                                Console.WriteLine("nextLastMsgNumber = " + nextLastMsgNumber);
                                subAux.updateLastMsgNumber(pubURL);
                                flushMsgQueue(subAux, ss, pubURL);
                            }
                            else
                            {
                                subAux.addfilteredSeqNumber(pubURL, msgNumber);
                            }
                        }
                    }

                }
                Console.WriteLine("ENDING FIFO");
            }
            else if (ordering == "TOTAL" && routing!="flooding")
            {
                Console.WriteLine("******************************");
                Console.WriteLine("TOTALing IT OUT!");
                HashSet<string> subsWhoGotMessage = new HashSet<string>();
                List<SubscriberInterface> subset = new List<SubscriberInterface>();
                subset = setOfTopics(msgTopic);
                foreach (SubscriberInterface sub in subset)
                {
                    SubAux subAux = subLastMsgReceived.Find(o => o.Sub.getURL() == sub.getURL());
                    int nextLastMsgNumber = subAux.LastGlobalMsgNumber + 1;

                    Console.WriteLine("SUBSCRIBER: " + sub.getURL());
                    Console.WriteLine("PUBLISHER: " + pubURL);
                    Console.WriteLine("MSG TOPIC = " + msgTopic);
                    Console.WriteLine("Msg Number = " + evt.MsgNumber);
                    Console.WriteLine("nextLastMsgNumber = " + nextLastMsgNumber);
                    if (msgNumber == nextLastMsgNumber)
                    {
                        Console.WriteLine("entregar!!!!!!");
                        if (assertPrimary())
                        {
                            sub.deliverToSub(evt);
                        }                       
                        subAux.updateLastGlobalMsgNumber();
                        flushQueueTotal(sub, subAux);
                        //para nao entregarmos a mesma mensagem duas vezes caso o sub esteja subscrito ao topico e a um
                        //hipertopico
                        subsWhoGotMessage.Add(sub.getURL());
                    }
                    else
                    {
                        //verificar se as mensagens em falta nao sao mensagens que simplesmente nao queremos. Se forem, entao podemos entregar a mensagem
                        if(!assertOldTotalMsg(sub.getURL(), evt.MsgNumber))
                        {
                            Console.WriteLine("arquivar!!!!");
                            subAux.addToQueueTotal(evt);                      
                        }
                        else
                        {
                            Console.WriteLine("afinal entregar!!!!!!");
                            if (assertPrimary())
                            {
                                sub.deliverToSub(evt);
                            }
                            subAux.updateLastGlobalMsgNumber(evt.MsgNumber);
                            flushQueueTotal(sub, subAux);
                        }
                        
                    }
                }
            }
            else if(ordering == "TOTAL" && routing == "flooding"){
                addToPubsByTopic(pubURL, msgTopic);
                HashSet<string> subsWhoGotMessage = new HashSet<string>();
                List<SubscriberInterface> subset = new List<SubscriberInterface>();
                subset = setOfTopics(msgTopic);
                foreach (SubscriberInterface sub in subset)
                {
                    SubAux subAux = subLastMsgReceived.Find(o => o.Sub.getURL() == sub.getURL());
                    int nextLastMsgNumber = subAux.LastGlobalMsgNumber + 1;
                    Console.WriteLine("SUBSCRIBER: " + sub.getURL());
                    Console.WriteLine("PUBLISHER: " + pubURL);
                    Console.WriteLine("MSG TOPIC = " + msgTopic);
                    Console.WriteLine("Msg Number = " + evt.MsgNumber);
                    Console.WriteLine("nextLastMsgNumber = " + nextLastMsgNumber);
                    if (msgNumber == nextLastMsgNumber)
                    {
                        Console.WriteLine("entregar!!!!!!");
                        if (assertPrimary())
                        {
                            sub.deliverToSub(evt);
                        }
                        
                        subAux.updateLastGlobalMsgNumber();
                        flushQueueTotal(sub, subAux);
                        subsWhoGotMessage.Add(sub.getURL());
                    }
                    else
                    {
                        Console.WriteLine("arquivar!!!!");
                        subAux.addToQueueTotal(evt);
                        subsWhoGotMessage.Add(sub.getURL());
                    }
                }

                subset = setNonTopics(msgTopic);
                foreach (SubscriberInterface ss in subset)
                {
                    //se ja recebeu nao interessa mais. Evitar que seja entregue a mesma msg duas vezes ao mesmo sub
                    if (!subsWhoGotMessage.Contains(ss.getURL()))
                    {                       
                        SubAux subAux = subLastMsgReceived.Find(o => o.Sub.getURL() == ss.getURL());
                        int nextLastMsgNumber = subAux.LastGlobalMsgNumber + 1;
                        Console.WriteLine("SUBSCRIBER: " + ss.getURL());
                        Console.WriteLine("PUBLISHER: " + pubURL);
                        Console.WriteLine("MSG TOPIC = " + msgTopic);
                        Console.WriteLine("Msg Number = " + evt.MsgNumber);
                        Console.WriteLine("nextLastMsgNumber = " + nextLastMsgNumber);
                        if (msgNumber == nextLastMsgNumber)
                        {
                            subAux.updateLastGlobalMsgNumber();
                            flushQueueTotal(ss, subAux);
                        }
                        else
                        {
                            subAux.addToQueueTotal(evt);
                            subAux.addEventNotToDeliver(evt.MsgNumber);
                        }                                                            
                    }
                }

            }
            else
            {
                if (assertPrimary())
                {
                    foreach (KeyValuePair<string, List<SubscriberInterface>> entry in subscribersByTopic)
                    {
                        topic = entry.Key;
                        foreach (SubscriberInterface sub in entry.Value)
                        {
                            if (topic.Equals(evt.Topic) || isSubtopicOf(evt.Topic, topic))
                            {
                                sub.deliverToSub(evt);                                                 
                            }
                        }

                    }
                }                
            }

        }

        public void forwardToBroker(string url, Event evt)
        {
            if (routing.Equals("flooding"))
            {
                if (assertPrimary())
                {
                    foreach (BrokerToBrokerInterface d in dad)
                    {
                        d.forwardEvent(this.url, evt);
                    }
                    foreach (BrokerToBrokerInterface son in sons)
                    {
                        son.forwardEvent(this.url, evt);
                        notifyPM(evt);
                    }
                }             
            }
            else if (routing.Equals("filter") && ordering != "TOTAL")
            {
                Console.WriteLine("FILTERING!!!!!!!!!!!!!!!!!!!!");
                Console.WriteLine("MSG TOPIC: " + evt.Topic);
                Console.WriteLine("MSG NUMBER: " + evt.MsgNumber);
                Console.WriteLine("Broker who sended this: " + url);
                Console.WriteLine("Publisher: " + evt.PublisherName);
                int msgNumberCopy = evt.MsgNumber;
                Console.WriteLine("MSG NUMBER COPY: " + msgNumberCopy);
                string keyTopic;
                HashSet<string> brokersURLWhoGotMsg = new HashSet<string>();
                HashSet<string> brokersWhoWereNeglected = new HashSet<string>();
                foreach (KeyValuePair<string, List<BrokerToBrokerInterface>> entry in brokersByTopic)
                {
                    keyTopic = entry.Key;
                    if (keyTopic.Equals(evt.Topic) || isSubtopicOf(evt.Topic, keyTopic))
                    {

                        List<BrokerToBrokerInterface> brokersOftopic = new List<BrokerToBrokerInterface>();
                        if (entry.Value != null)
                            brokersOftopic = entry.Value;
                        //brokers vizinhos que para um dado topico nao o recebem porque é filtrado
                        List<BrokerToBrokerInterface> neglectedBrokers = new List<BrokerToBrokerInterface>();
                        List<BrokerToBrokerInterface> auxLst2 = sons;
                        auxLst2.AddRange(dad);
                        neglectedBrokers = auxLst2.Except(brokersOftopic, new SameBrokerComparer()).ToList();

                        foreach (BrokerToBrokerInterface sss in neglectedBrokers)
                        {
                            Console.WriteLine("neglectedBroker: " + sss.getURL());
                        }

                        if (neglectedBrokers.Count == 0)
                            Console.WriteLine("neglectedBrokers vazios");

                        Console.WriteLine("TOPICO: " + evt.Topic);

                        foreach (BrokerToBrokerInterface broker in entry.Value)
                        {
                            if (!broker.getURL().Equals(url))
                            {
                                if (!brokersURLWhoGotMsg.Contains(broker.getURL()) && !brokersWhoWereNeglected.Contains(broker.getURL()))
                                {
                                    Event modMsg = new Event(evt.PublisherName, evt.Topic, evt.Content, evt.MsgNumber);
                                    //se para um dado tuplo <broker, pub> houver uma entrada na tabela filteredTable temos de modificar o 
                                    //numero das mensagens antes das enviarmos (aplicam-se algumas regras nesta modificacao)
                                    Console.WriteLine("enviar a mensagem para o broker: " + broker.getURL());
                                    Tuple<string, string> tp = new Tuple<string, string>(broker.getURL(), evt.PublisherName);
                                    List<int> auxLst = new List<int>();
                                    if (filteringTable.TryGetValue(tp, out auxLst))
                                    {
                                        int counter = 0;
                                        List<int> ll = auxLst;
                                        foreach (int i in ll)
                                        {
                                            Console.WriteLine("valor na filtering table: " + i);
                                            if (i > evt.MsgNumber)
                                            {
                                                break;
                                            }
                                            counter++;

                                        }
                                        Console.WriteLine("valor do counter: " + counter);
                                        modMsg.MsgNumber -= counter;
                                        Console.WriteLine("Modifiquei a msg" + evt.Topic);
                                        Console.WriteLine("Novo msg number é: " + msgNumberCopy);
                                    }
                                    if (assertPrimary())
                                    {
                                        broker.forwardEvent(this.url, modMsg);
                                    }                                   
                                    brokersURLWhoGotMsg.Add(broker.getURL());
                                    notifyPM(evt);
                                }
                            }
                        }
                        //se ha pelo menos um broker que foi filtrado temos de assinalar isto na tabela
                        if (neglectedBrokers.Any())
                        {
                            foreach (BrokerToBrokerInterface bb in neglectedBrokers)
                            {
                                if (!bb.getURL().Equals(url))
                                {
                                    if (!brokersWhoWereNeglected.Contains(bb.getURL()) && !brokersURLWhoGotMsg.Contains(bb.getURL()))
                                    {
                                        Console.WriteLine("broker na filtering table: " + bb.getURL());

                                        Tuple<string, string> tp = new Tuple<string, string>(bb.getURL(), evt.PublisherName);
                                        List<int> auxLst = new List<int>();
                                        if (!filteringTable.TryGetValue(tp, out auxLst))
                                        {
                                            filteringTable.Add(tp, new List<int> { evt.MsgNumber });
                                            Console.WriteLine("adicionada entrada na filtering table");
                                            foreach (int ii in filteringTable[tp])
                                            {
                                                Console.WriteLine("valor na filtering table: " + ii);
                                            }
                                        }
                                        else
                                        {
                                            auxLst.Add(evt.MsgNumber);
                                            Console.WriteLine("adicionado valor na filtering table: " + evt.MsgNumber);
                                            filteringTable[tp] = auxLst.OrderBy(o => o).ToList();
                                        }
                                        brokersWhoWereNeglected.Add(bb.getURL());
                                    }
                                }
                            }
                        }
                    }
                }

            }
            else if (routing.Equals("filter") && ordering == "TOTAL")
            {
                HashSet<string> brokersURLWhoGotMsg = new HashSet<string>();
                foreach (KeyValuePair<string, List<BrokerToBrokerInterface>> entry in brokersByTopic)
                {
                    string keyTopic = entry.Key;
                    if (keyTopic.Equals(evt.Topic) || isSubtopicOf(evt.Topic, keyTopic))
                    {
                        foreach (BrokerToBrokerInterface broker in entry.Value)
                        {
                            if (!broker.getURL().Equals(url))
                            {
                                if (!brokersURLWhoGotMsg.Contains(broker.getURL())){
                                    if (assertPrimary())
                                    {
                                        broker.forwardEvent(this.url, evt);
                                    }                                   
                                    brokersURLWhoGotMsg.Add(broker.getURL());
                                    notifyPM(evt);
                                }
                            }
                        }
                    }
                }

            }

        }

        public void forwardEvent(string url, Event evt)
        {
            if (freezeFlag)
            {
                lock(this) numberFreezes++;
                freezeEvent.WaitOne();
            }
            lock (this) {
                bool exists = false;
                foreach (Event e in events) if (evt == e) exists = true;
                if (!exists)
                {
                    events.Add(evt);
                    deliverToSub(url, evt);
                    forwardToBroker(url, evt);                                   
                }
            }
        }

        //subconjunto da tabela de topicos -> subs 
        //escolhemos os topicos para os quais o nosso topico é subtopico (e o proprio topico)
        public List<SubscriberInterface> setOfTopics(string topic)
        {
            List<SubscriberInterface> result = new List<SubscriberInterface>();
            SameSubscriberComparer compareSubs = new SameSubscriberComparer();
            HashSet<SubscriberInterface> auxResult = new HashSet<SubscriberInterface>(compareSubs);
            string topicKey;
            foreach (KeyValuePair<string, List<SubscriberInterface>> entry in subscribersByTopic)
            {
                topicKey = entry.Key;
                if (topicKey.Equals(topic) || isSubtopicOf(topic, topicKey))
                {
                    foreach (SubscriberInterface s in entry.Value)
                    {
                        auxResult.Add(s);
                    }
                }
            }
            result = auxResult.ToList();
            return result;
        }

        //escolhemos os topicos para os quais o nosso topico nao é subtopico (nem o proprio topico)
        public List<SubscriberInterface> setNonTopics(string topic)
        {
            List<SubscriberInterface> result = new List<SubscriberInterface>();
            SameSubscriberComparer compareSubs = new SameSubscriberComparer();
            HashSet<SubscriberInterface> auxResult = new HashSet<SubscriberInterface>(compareSubs);
            string topicKey;
            foreach (KeyValuePair<string, List<SubscriberInterface>> entry in subscribersByTopic)
            {
                topicKey = entry.Key;
                if (!(topicKey.Equals(topic) || isSubtopicOf(topic, topicKey)))
                {
                    foreach (SubscriberInterface s in entry.Value) {
                        auxResult.Add(s);
                    }
                }
            }
            result = auxResult.ToList();
            return result;
        }


        //quando recebemos um evento temos de verificar se nao ha eventos anteriormente recebidos que tiveram de ser guardados 
        //porque nao tinha sido recebido um evento anterior. Se houver, temos de entrega-los ao sub
        public void flushMsgQueue(SubAux subAux, SubscriberInterface sub, string pubURL)
        { 
            if (subAux.msgQueue(pubURL) != null)
            {
               
                List<Event> eventsToRemove = new List<Event>();

                foreach (Event pendingEvent in subAux.msgQueue(pubURL))
                {
                    if (pendingEvent.MsgNumber == subAux.lastMsgNumber(pubURL)+1)
                    {
                        if (assertPrimary())
                        {
                            sub.deliverToSub(pendingEvent);
                        }                       
                        subAux.updateLastMsgNumber(pubURL);
                        eventsToRemove.Add(pendingEvent);
                    }
                }
                subAux.updateMsgQueue(pubURL, eventsToRemove);
                eventsToRemove.Clear();
            }
        }

        public void flushMsgQueueTopic(SubAux subAux, SubscriberInterface sub, string topic)
        {
            
            if (subAux.msgQueueTopic(topic) != null)
            {
                Console.WriteLine("entrei na msgQUEUE");
                List<Event> eventsToRemove = new List<Event>();

                foreach (Event pendingEvent in subAux.msgQueueTopic(topic))
                {
                    Console.WriteLine("msg number na msgQueue: " + pendingEvent.MsgNumber);
                    Console.WriteLine("next msg number a entregar: " + subAux.lastMsgNumber(topic));
                    if (pendingEvent.MsgNumber == subAux.lastMsgNumber(topic)+1)
                    {
                        if (assertPrimary())
                        {
                            sub.deliverToSub(pendingEvent);
                        }                        
                        subAux.updateLastMsgNumber(topic);
                        eventsToRemove.Add(pendingEvent);
                    }
                }
                subAux.updateMsgQueueTopic(topic, eventsToRemove);
                eventsToRemove.Clear();
            }
        }

        public void addDad(string url)
        {
            lock (this)
            {
                BrokerToBrokerInterface brokerDad = (BrokerToBrokerInterface)Activator.GetObject(typeof(BrokerToBrokerInterface), url + "B");
                dad.Add(brokerDad);
                if (routing.Equals("filter")) topicsProvidedByBroker.Add(brokerDad, new List<string>());
            }
        }

        public void addSon(string url)
        {
            lock (this)
            {
                BrokerToBrokerInterface brokerSon = (BrokerToBrokerInterface)Activator.GetObject(typeof(BrokerToBrokerInterface), url + "B");
                sons.Add(brokerSon);
                if (routing.Equals("filter")) topicsProvidedByBroker.Add(brokerSon, new List<string>());
            }
        }

        public void crash()
        {
            System.Environment.Exit(1);
        }

        public void freeze()
        {
            lock (this)
            {
                freezeFlag = true;
            }
        }

        public void status()
        {
            Console.WriteLine("Dad:");
            foreach(BrokerToBrokerInterface bb in dad) Console.WriteLine(bb.getURL());
            Console.WriteLine("Sons:");
            foreach (BrokerToBrokerInterface bb in sons) Console.WriteLine(bb.getURL());
        }

        public void unfreeze()
        {
            lock (this)
            {
                freezeFlag = false;
            }
            for (int i = 0; i < numberFreezes; i++)
                freezeEvent.Set();
            numberFreezes = 0;
        }

        public void subscribe(string topic, string subscriberURL)
        {
            if (freezeFlag)
            {
                numberFreezes++;
                freezeEvent.WaitOne();
            }
            lock (this)
            {
                bool subExists = false;
                SubscriberInterface newSubscriber = (SubscriberInterface)Activator.GetObject(
                           typeof(SubscriberInterface), subscriberURL);

                if (ordering == "FIFO" || ordering == "TOTAL")
                {
                    foreach (SubAux sub in subLastMsgReceived)
                    {
                        if (sub.Sub.getURL().Equals(subscriberURL))
                        {
                            subExists = true;
                            break;
                        }
                    }
                    if (!subExists)
                    {
                        subLastMsgReceived.Add(new SubAux(newSubscriber));
                    }
                        
                    subExists = false;
                }
                if (subscribersByTopic.ContainsKey(topic))
                    subscribersByTopic[topic].Add(newSubscriber);
                else
                    subscribersByTopic.Add(topic, new List<SubscriberInterface> { newSubscriber });

                Console.WriteLine("Subscriber: " + subscriberURL + " topic: " + topic);
                
                if (routing.Equals("filter"))
                {
                    foreach (KeyValuePair<BrokerToBrokerInterface, List<string>> entry in topicsProvidedByBroker)
                    {
                        if (!entry.Value.Contains(topic))
                        {
                            foreach (string topicValue in entry.Value)
                            {
                                if (isSubtopicOf(topic, topicValue))
                                    return;
                            }
                            entry.Key.forwardInterest(this.url, topic);
                            Console.WriteLine("Forward interest to " + entry.Key.getURL() + " on topic " + topic);
                            entry.Value.Add(topic);
                        }
                    }
                }

            }
        }

        public void unsubscribe(string topic, string subscriberURL)
        {
            if (freezeFlag)
            {
                numberFreezes++;
                freezeEvent.WaitOne();
            }
            lock (this)
            {
                SubscriberInterface subscriber = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), subscriberURL);

                int index = 0;
                bool topicRemoved = false;
                List<SubscriberInterface> auxLst = new List<SubscriberInterface>();
                foreach (SubscriberInterface sub in subscribersByTopic[topic])
                {
                    if (sub.getURL().Equals(subscriberURL))
                    {
                        SubAux subAux = subLastMsgReceived.Find(o => o.Sub.getURL() == sub.getURL());
                        if (subscribersByTopic[topic].Count == 1)
                        {
                            subscribersByTopic.Remove(topic);
                            topicRemoved = true;
                        }
                        else
                        {
                            subscribersByTopic[topic].RemoveAt(index);                           
                            return;
                        }
                        
                        break;
                    }
                    index++;
                }
                if (routing.Equals("filter") && topicRemoved)
                {
                    if (!brokersByTopic.ContainsKey(topic))
                    {
                        List<string> wantedTopics = mergeInterestedSubsAndBrokers(topic);
                        
                        foreach (KeyValuePair<BrokerToBrokerInterface, List<string>> entry in topicsProvidedByBroker)
                        {
                            if (entry.Value.Contains(topic))
                            {
                                entry.Key.forwardDisinterest(this.url, topic);
                                Console.WriteLine("Forward disinterest to " + entry.Key.getURL() + " on topic " + topic);
                                entry.Value.Remove(topic);

                                foreach (string newTopic in wantedTopics)
                                {
                                    entry.Key.forwardInterest(this.url, newTopic);
                                    Console.WriteLine("Forward interest to " + entry.Key.getURL() + " on topic " + newTopic);
                                    entry.Value.Add(topic);
                                }
                            }
                        }
                    }
                    else return;
                }
            }
            Console.WriteLine("Unsubscriber: " + subscriberURL + " topic: " + topic);
        }


        public void publishEvent(Event newEvent)
        {
            lock (this) {
                if (ordering == "FIFO")
                {
                    List<BrokerToBrokerInterface> auxLst = new List<BrokerToBrokerInterface>();
                    if(!brokersByTopic.TryGetValue(newEvent.Topic, out auxLst))
                    {
                        brokersByTopic.Add(newEvent.Topic, new List<BrokerToBrokerInterface>());
                    }
                    string pubURL = newEvent.PublisherName;
                    int msgNumber = newEvent.MsgNumber;
                    int aux;
                    addToPubsByTopic(pubURL, newEvent.Topic);
                    if (!lastMsgNumberByPub.TryGetValue(pubURL, out aux))
                    {
                        lastMsgNumberByPub.Add(pubURL, initLastMsgNumber);
                        msgQueueByPub.Add(pubURL, new List<Event>());
                    }

                    if (lastMsgNumberByPub[pubURL] + 1 == msgNumber)
                    {
                        if (assertPrimary())
                        {
                            forwardEvent(this.url, newEvent);
                        }                      
                        lastMsgNumberByPub[pubURL]++;
                        flushPubMsgQueue(pubURL);                      
                        Console.WriteLine("Event Forwarded by Publisher: " + newEvent.PublisherName + ", topic: " + newEvent.Topic);                    
                    }
                    else
                    {
                        msgQueueByPub[pubURL].Add(newEvent);
                        msgQueueByPub[pubURL] = msgQueueByPub[pubURL].OrderBy(o => o.MsgNumber).ToList();
                    }
                }
                else if (ordering == "TOTAL")
                {
                    List<BrokerToBrokerInterface> auxLst = new List<BrokerToBrokerInterface>();
                    if (!brokersByTopic.TryGetValue(newEvent.Topic, out auxLst))
                    {
                        brokersByTopic.Add(newEvent.Topic, new List<BrokerToBrokerInterface>());
                    }
                    //Vamos usar o root como sequencer de todos os topicos. 
                    if (root == null)
                        rootURL = findRootNode();
                    root = (BrokerToBrokerInterface)Activator.GetObject(typeof(BrokerToBrokerInterface), rootURL);
                    Console.WriteLine("root url: " + rootURL);
                    //enviamos ao sequencer a mensagem sem o conteudo. Para ser mais eficaz
                    Event evt = new Event(newEvent.PublisherName, newEvent.Topic, "", newEvent.MsgNumber);
                    if (assertPrimary())
                    {
                        root.reqSequence(this.url, evt);
                    }
                    else
                    {
                        
                    }                    
                    holdBackQueueByTopic.Add(Tuple.Create(newEvent.PublisherName, newEvent.MsgNumber), newEvent);
                }
                else
                {
                    if (assertPrimary())
                    {
                        forwardEvent(this.url, newEvent);
                    }                    
                    Console.WriteLine("Event Forwarded by Publisher: " + newEvent.PublisherName + ", topic: " + newEvent.Topic);
                }
            }       
        }
        //root é o sequencer. 
        //url é para onde temos de mandar a respota
        public void reqSequence(string url, Event evt)
        {
            lock (this)
            {
                Console.WriteLine("Sequencer received request from: " + url);
                seqNb = seqNb + 1;
                if (routing == "filter")
                {
                    propagate(seqNb, evt.Topic);
                    //Thread.Sleep(1000);
                    auxRcvGlobalInfo(seqNb, evt.Topic);
                }
                seqNbByMessage.Add(Tuple.Create(evt.Topic, evt.MsgNumber), seqNb);
                BrokerToBrokerInterface brk = (BrokerToBrokerInterface)Activator.GetObject(typeof(BrokerToBrokerInterface), url+"B");
                brk.rcvSeqNumber(seqNb, evt);
            }
        }

        public void reqSequenceReplica(string url, Event evt)
        {
            lock (this)
            {
                Console.WriteLine("Sequencer received request from: " + url);
                seqNb = seqNb + 1;
                if (routing == "filter")
                {
                    propagate(seqNb, evt.Topic);
                    //Thread.Sleep(1000);
                    auxRcvGlobalInfo(seqNb, evt.Topic);
                }
                BrokerToBrokerInterface brk = (BrokerToBrokerInterface)Activator.GetObject(typeof(BrokerToBrokerInterface), url + "B");
                brk.rcvSeqNumber(seqNb, evt);
            }
        }

        public void rcvSeqNumber(int seqNumber, Event evt)
        {
            lock (this)
            {
                Event aux;
                if(holdBackQueueByTopic.TryGetValue(Tuple.Create(evt.PublisherName, evt.MsgNumber), out aux))
                {
                    Event seqedEvent = new Event(aux.PublisherName, aux.Topic, aux.Content, seqNumber);
                    Console.WriteLine("Msg original broker: " + this.url);
                    Console.WriteLine("Msg topic: " + aux.Topic + " from: " + aux.PublisherName);
                    Console.WriteLine("new msgNumber given by sequencer: " + seqNumber);
                    Console.WriteLine("old msgNumber: " + evt.MsgNumber);
                    if (assertPrimary())
                    {
                        forwardEvent(this.url, seqedEvent);
                    }                    
                }
                else
                {
                    Console.WriteLine("Unrequested seqNumber");
                }                
            }
        }

        //pode ser uma procura na arvore que se faz uma vez. Vai-se perguntando ao pai ate se encontrar um no sem pai. Devolve-se essa info
        //aos outros nos. Ou root podia fazer flooding do seu url. Ou hardcoded como agora 
        public string findRootNode()
        {
            return "tcp://localhost:3333/brokerB";
        }

        //root vai propagar a info a todos os filhos. Isto é necessário para garantir total order com overlapping groups
        public void propagate(int seqNb, string topic)
        {
            lock (this) { 
                foreach (BrokerToBrokerInterface bb in sons)
                {
                    bb.rcvGlobalInfo(seqNb, topic);
                }
            }
        }

        //cada broker vai receber do sequencer informação sobre todas as mensagens no sistema. Mais concretamente vao receber o topico e o numero 
        //de sequencia global atribuido pelo sequencer. Cada broker propaga esta info aos seus filhos. Cada broker verifica para cada um dos seus subs
        //se o topico em questao lhe interessa. Se interessar, nao faz nada (porque eventualmente estes subs hao de receber esse evento). Se nao interessar entao
        //regista numa tabela (neglectedTotalTopicsBySub) que aquela mensagem é doutro topico qq. Assim quando um sub receber uma mensagem que lhe interesse
        //ve nessa tabela se a mensagem anterior (eventualmente mais do que a anterior) era dum topico que nao lhe interessava.
        public void rcvGlobalInfo(int seqNb, string msgTopic)
        {
            lock (this)
            {
                propagate(seqNb, msgTopic);
                auxRcvGlobalInfo(seqNb, msgTopic);
            }  
        }

        public void auxRcvGlobalInfo(int seqNb, string msgTopic)
        {
            Console.WriteLine("received global info!");
            Console.WriteLine("seqNB: " + seqNb + "msg topic: " + msgTopic);
            HashSet<string> alreadyGotInfo = new HashSet<string>();

            List<SubscriberInterface> subs = setOfTopics(msgTopic);
            //para todos os subs que recebem mensagens deste topico nao precisamos de adicionar entrada na tabela
            foreach(SubscriberInterface s in subs)
            {
                alreadyGotInfo.Add(s.getURL());
                Console.WriteLine("sub que recebe o topico: " + s.getURL());
            }
            subs = setNonTopics(msgTopic);
            foreach(SubscriberInterface ss in subs)
            {
                Console.WriteLine("sub que nao recebe o topico: " + ss.getURL());
                if (!alreadyGotInfo.Contains(ss.getURL()))
                {
                    string subURL = ss.getURL();
                    if (alreadyGotInfo.Contains(subURL)) { continue; }
                    List<int> auxLst;
                    if (!neglectedTotalTopicsBySub.TryGetValue(subURL, out auxLst))
                    {
                        neglectedTotalTopicsBySub.Add(subURL, new List<int>() { seqNb });
                    }
                    else
                    {
                        auxLst.Add(seqNb);
                    }
                    alreadyGotInfo.Add(subURL);
                    Console.WriteLine("adicionada entrada na neglectedTOtal: " + seqNb);

                }

            }                
            

        }

        public void flushQueueTotal(SubscriberInterface sub, SubAux subAux)
        {
            List<Event> lst = subAux.getQueueTotal();
            List<Event> eventsToRemove = new List<Event>();
            foreach(Event pendingEvent in lst)
            {
                Console.WriteLine("SUBSCRIBER: " + sub.getURL());
                Console.WriteLine("MSG TOPIC = " + pendingEvent.Topic);
                Console.WriteLine("Msg Number = " + pendingEvent.MsgNumber);
                if((pendingEvent.MsgNumber == subAux.LastGlobalMsgNumber + 1) && !subAux.checkEventNotToDeliver(pendingEvent))
                {                  
                    Console.WriteLine("entregar a msg que estava na queue");
                    if (assertPrimary())
                    {
                        sub.deliverToSub(pendingEvent);
                    }
                    subAux.updateLastGlobalMsgNumber();
                    eventsToRemove.Add(pendingEvent);
                }
                else if ((pendingEvent.MsgNumber == subAux.LastGlobalMsgNumber + 1) && subAux.checkEventNotToDeliver(pendingEvent))
                {
                    subAux.updateLastGlobalMsgNumber();
                    eventsToRemove.Add(pendingEvent);
                }
                else
                {
                    break;
                }
            }
            subAux.updateMsgQueueTotal(eventsToRemove);
            eventsToRemove.Clear();
        }

        public bool assertOldTotalMsg(string subURL, int msgNB)
        {
            SubAux subAux = subLastMsgReceived.Find(o => o.Sub.getURL() == subURL);
            List<int> auxLst;
            if(neglectedTotalTopicsBySub.TryGetValue(subURL, out auxLst))
            {
                int j = -1;
                int next = -1;
                List<int> toRemove = new List<int>();
                int size = auxLst.Count;
                for(int i = 0; i < size; i++)
                {
                    Console.WriteLine("valor da entrada na neglectedTOTAL: " + auxLst[i]);
                    if (size > 1)
                    {
                        if(i!=size-1)
                            next = auxLst[i+1];
                    }
                    if (auxLst[i] < msgNB)
                    {
                        j = auxLst[i];
                        toRemove.Add(auxLst[i]);
                    }
                    else
                    {
                        break;
                    }

                    if (auxLst[i]+1 != next && next!=-1)
                    {
                        break;
                    }
                    
                }
                if (j + 1 == msgNB)
                {
                    List<int> aux = neglectedTotalTopicsBySub[subURL].Except(toRemove).ToList();
                    neglectedTotalTopicsBySub[subURL] = aux;
                    Console.WriteLine("entrada removida da neglectedTOtal: " + j);
                    return true;
                }
            }
            return false;
        }

        public void flushPubMsgQueue(string pubURL)
        {
            List<Event> eventsToRemove = new List<Event>();
            foreach (Event evt in msgQueueByPub[pubURL])
            {              
                if (evt.MsgNumber == (lastMsgNumberByPub[pubURL] + 1))
                {
                    if (assertPrimary())
                    {
                        forwardEvent(this.url, evt);
                    }                   
                    lastMsgNumberByPub[pubURL]++;
                    eventsToRemove.Add(evt);
                    Console.WriteLine("Event Forwarded by Publisher: " + evt.PublisherName + ", topic: " + evt.Topic);
                }
                else
                {
                    break;
                }
            }
            foreach (Event e in eventsToRemove)
            {
                msgQueueByPub[pubURL].Remove(e);
            }
            eventsToRemove.Clear();       
        }


        public void forwardInterest(string url, string topic)
        {
            Console.WriteLine("Received forwardInterest(" + url + ", " + topic + ")");
            BrokerToBrokerInterface interestedBroker = (BrokerToBrokerInterface)Activator.GetObject(typeof(BrokerToBrokerInterface), url + "B");

            List<BrokerToBrokerInterface> brokers;
            if(brokersByTopic.TryGetValue(topic, out brokers))
            {
                if (brokers != null) { 
                    foreach (BrokerToBrokerInterface broker in brokers)
                    {
                        if (!broker.getURL().Equals(url))
                        {
                            brokersByTopic[topic].Add(interestedBroker);

                            forwardInterestAux(url, topic);
                        }
                    }
                }

            }
            else
            {
                brokersByTopic.Add(topic, new List<BrokerToBrokerInterface> { interestedBroker });

                forwardInterestAux(url, topic);
            }
            
        }

        //para cada keyvaluepair<broker, topics que recebe desse broker> de topicsProvidedByBroker,
        //se ainda nao recebemos o topico "topic" de um broker vizinho,
        //enviamos lhe forwardInterest
        public void forwardInterestAux(string url, string topic)
        {
            foreach (KeyValuePair<BrokerToBrokerInterface, List<string>> entry in topicsProvidedByBroker)
            {
                if (!entry.Key.getURL().Equals(url))
                {
                    if (!entry.Value.Contains(topic))
                    {
                        if (assertPrimary())
                        {
                            entry.Key.forwardInterest(this.url, topic);
                        }                       
                        Console.WriteLine("Forwarded interest to " + entry.Key.getURL() + " on topic " + topic);
                        entry.Value.Add(topic);
                    }
                }
            }
        }

        public void notifyPM(Event evt)
        {
            string notification = "BroEvent " + processName + ", " + evt.PublisherName + ", " + evt.Topic + ", " + evt.MsgNumber.ToString();
            if (loggingLevel.Equals("full")) pm.notify(notification);
            Console.WriteLine(notification);
        }
        
        public void forwardDisinterest(string url, string topic)
        {
            Console.WriteLine("Received forwardDisinterest(" + url + ", " + topic + ")");
            int index = 0;
            bool topicRemoved = false;
            foreach (BrokerToBrokerInterface broker in brokersByTopic[topic])
            {
                if (broker.getURL().Equals(url))
                {
                    if (brokersByTopic[topic].Count == 1)
                    {
                        brokersByTopic.Remove(topic);
                        topicRemoved = true;
                    }
                    else
                    {
                        brokersByTopic[topic].RemoveAt(index);
                        return;
                    }
                    break;
                }
                index++;
            }

            if (topicRemoved)
            {
                if (!subscribersByTopic.ContainsKey(topic))
                {
                    List<string> wantedTopics = mergeInterestedSubsAndBrokers(topic);
                    foreach (KeyValuePair<BrokerToBrokerInterface, List<string>> entry in topicsProvidedByBroker)
                    {
                        if (entry.Value.Contains(topic))
                        {
                            if (assertPrimary())
                            {
                                entry.Key.forwardDisinterest(this.url, topic);
                            }                           
                            Console.WriteLine("Forward disinterest to " + entry.Key.getURL() + " on topic " + topic);
                            entry.Value.Remove(topic);

                            foreach (string newTopic in wantedTopics)
                            {
                                if (assertPrimary())
                                {
                                    entry.Key.forwardInterest(this.url, newTopic);
                                }                               
                                Console.WriteLine("Forward interest to " + entry.Key.getURL() + " on topic " + newTopic);
                                entry.Value.Add(topic);
                            }
                        }
                    }
                }
                else return;

            }
            
        }

      
        public List<string> subscribersByTopicSubtopicsOf(string topic)
        {
            List<string> subtopics = new List<string>();
            foreach (KeyValuePair<string, List<SubscriberInterface>> entry in subscribersByTopic)
            {
                if (isSubtopicOf(entry.Key, topic))
                {
                    subtopics.Add(entry.Key);
                }
            }
            
            return reorganizeSubtopics(subtopics);

        }

        public List<string> brokersByTopicSubtopicsOf(string topic)
        {
            List<string> subtopics = new List<string>();
            foreach (KeyValuePair<string, List<BrokerToBrokerInterface>> entry in brokersByTopic)
            {
                if (isSubtopicOf(entry.Key, topic))
                {
                    subtopics.Add(entry.Key);
                }
            }
            
            return reorganizeSubtopics(subtopics);

        }

        public List<string> mergeInterestedSubsAndBrokers(string topic)
        {
            var stillInterestedSubscribers = subscribersByTopicSubtopicsOf(topic);
            var stillInterestedBrokers = brokersByTopicSubtopicsOf(topic);

            stillInterestedSubscribers.Union(stillInterestedBrokers);
            
            return reorganizeSubtopics(stillInterestedSubscribers);
        }

        public List<string> reorganizeSubtopics(List<string> subtopics)
        {
            List<string> stillInterestedTopics = new List<string>();
            bool isSubtopic = false;
            foreach (string a in subtopics)
            {
                isSubtopic = false;
                if (stillInterestedTopics.Count > 0)
                {
                    int index = 0;
                    foreach (string b in stillInterestedTopics)
                    {
                        if (isSubtopicOf(a, b))
                        {
                            isSubtopic = true;
                            break;
                        }
                        else if (isSubtopicOf(b, a))
                        {
                            stillInterestedTopics.RemoveAt(index);
                            break;
                        }
                        index++;
                    }
                }
                
                if (!isSubtopic) stillInterestedTopics.Add(a);
            }
            return stillInterestedTopics;
        }

        public void addReplica(string urlReplica)
        {
            lock (this)
            {
                BrokerToBrokerInterface brokerReplica = (BrokerToBrokerInterface)Activator.GetObject(typeof(BrokerToBrokerInterface), urlReplica + "B");
                replicas.Add(brokerReplica);
            }
        }

        public bool assertPrimary()
        {
            if (brokerID == LiderID)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}