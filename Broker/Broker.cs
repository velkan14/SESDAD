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

        List<BrokerToBrokerInterface> dad = new List<BrokerToBrokerInterface>();
        List<BrokerToBrokerInterface> sons = new List<BrokerToBrokerInterface>();
        Dictionary<string, List<SubscriberInterface>> subscribersByTopic = new Dictionary<string, List<SubscriberInterface>>();
        List<SubAux> subLastMsgReceived = new List<SubAux>();

        Dictionary<string, List<BrokerToBrokerInterface>> brokersByTopic = new Dictionary<string, List<BrokerToBrokerInterface>>();
        Dictionary<BrokerToBrokerInterface, List<string>> topicsProvidedByBroker = new Dictionary<BrokerToBrokerInterface, List<string>>();
        Dictionary<Tuple<string, string>, List<int>> filteringTable = new Dictionary<Tuple<string, string>, List<int>>();
        //estrutura para auxiliar a implementar comunicacao FIFO entre brokers. Ideia geniallll
        //<<Broker,Pub>,Lista das mensagens pendentes>    
        Dictionary<Tuple<string, string>, Tuple<int, List<Event>>> fifoBrokersMsgs = new Dictionary<Tuple<string, string>, Tuple<int, List<Event>>>();
        Dictionary<string,int> lastMsgNumberByPub = new Dictionary<string, int>();
        Dictionary<string, List<Event>> msgQueueByPub = new Dictionary<string, List<Event>>();

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

                    if (routing.Equals("flooding"))
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
                    else if (routing.Equals("filter"))
                    {
                        string keyTopic;
                        foreach (KeyValuePair<string, List<BrokerToBrokerInterface>> entry in brokersByTopic)
                        {
                            
                            keyTopic = entry.Key;
                            if (keyTopic.Equals(evt.Topic) || isSubtopicOf(evt.Topic, keyTopic))
                            {
                                
                                List<BrokerToBrokerInterface> brokersOftopic = entry.Value;
                                //brokers vizinhos que para um dado topico nao o recebem porque é filtrado
                                // List<BrokerToBrokerInterface> neglectedBrokers = sons;
                                //neglectedBrokers.AddRange(dad);
                                List<BrokerToBrokerInterface> neglectedBrokers = new List<BrokerToBrokerInterface>();
                                foreach (BrokerToBrokerInterface s in sons)
                                {
                                    foreach(BrokerToBrokerInterface s2 in entry.Value)
                                    {
                                        if (s.getURL() != s2.getURL())
                                            neglectedBrokers.Add(s);
                                    }
                                    if (entry.Value.Count == 0)
                                        neglectedBrokers = sons;
                                }
                                foreach(BrokerToBrokerInterface d in dad)
                                {
                                    foreach (BrokerToBrokerInterface s2 in entry.Value)
                                    {
                                        if (d.getURL() != s2.getURL())
                                            neglectedBrokers.Add(d);
                                    }
                                    if (entry.Value.Count == 0)
                                        neglectedBrokers.AddRange(dad);
                                }

                                Console.WriteLine("TOPICO: " + evt.Topic);
                                foreach (BrokerToBrokerInterface broker in brokersOftopic)
                                {
                                    if (!broker.getURL().Equals(url))
                                    {
                                        Console.WriteLine("broker do topico: " + broker.getURL());
                                        //se para um dado tuplo <broker, pub> houver uma entrada na tabela filteredTable temos de modificar o 
                                        //numero das mensagens antes das enviarmos (aplicam-se algumas regras nesta modificacao)
                                        Tuple<string, string> tp = new Tuple<string, string>(broker.getURL(), evt.PublisherName);
                                        List<int> auxLst = new List<int>();        
                                        if (filteringTable.TryGetValue(tp, out auxLst))
                                        {      
                                            int counter = 0;
                                            List<int> ll = auxLst;
                                            foreach (int i in ll)
                                            {
                                                if (i > evt.MsgNumber) {
                                                    
                                                    break;
                                                }
                                                counter++;
                                                
                                            }
                                            
                                            evt.MsgNumber -= counter;
                                        }
                                        
                                        broker.forwardEvent(this.url, evt);
                                        notifyPM(evt);
                                    }
                                }
                                //se ha pelo menos um broker que foi filtrado temos de assinalar isto na tabela
                                if (neglectedBrokers.Count>0)
                                {
                                    Console.WriteLine("a entrar na filtering table");

                                    foreach (BrokerToBrokerInterface bb in neglectedBrokers)
                                    {
                                        Console.WriteLine("broker na filtering table: " + bb.getURL());

                                        Tuple<string, string> tp = new Tuple<string, string>(bb.getURL(), evt.PublisherName);
                                        List<int> auxLst = new List<int>();
                                        if (!filteringTable.TryGetValue(tp, out auxLst))
                                        {
                                            filteringTable.Add(tp, new List<int> { evt.MsgNumber });
                                            Console.WriteLine("adicionada entrada na filtering table");
                                            Console.WriteLine("valor na filtering table: " + filteringTable[tp].First());
                                        }
                                        else
                                        {
                                            auxLst.Add(evt.MsgNumber);
                                            auxLst.OrderBy(o => o).ToList();
                                        }

                                    }

                                }



                            }
                        }
                        
                    }

                    string topic;
                    string pubURL = evt.PublisherName;
                    int msgNumber = evt.MsgNumber;
                    string msgTopic = evt.Topic;

                    //a ideia subjacente é a de que quando um evento chega a um sub ele pode estar interessado nela por se tratar dum topico ou subtopico que queira
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
                        HashSet<string> subsWhoGotMessage = new HashSet<string>();
                        Dictionary<string, List<SubscriberInterface>> subset = setOfTopics(msgTopic);
                        foreach (KeyValuePair<string, List<SubscriberInterface>> entry in subset)
                        {                           
                            topic = entry.Key;
                            foreach (SubscriberInterface sub in entry.Value)
                            {
                                SubAux subAux = subLastMsgReceived.Find(o => o.Sub.getURL() == sub.getURL());
                                int nextLastMsgNumber = subAux.lastMsgNumber(pubURL) + 1;
                                Console.WriteLine("******************************");
                                Console.WriteLine("SUBSCRIBER: " + sub.getURL());
                                Console.WriteLine("PUBLISHER: " + pubURL);
                                Console.WriteLine("MSG TOPIC = " + msgTopic);
                                Console.WriteLine("Msg Number = " + msgNumber);
                                Console.WriteLine("nextLastMsgNumber = " + nextLastMsgNumber);
                                if (topic.Equals(evt.Topic) || isSubtopicOf(evt.Topic, topic))
                                {
                                    subAux.addPub(pubURL, msgTopic);
                                    if (msgNumber == nextLastMsgNumber)
                                    {
                                        Console.WriteLine("entregar!!!!!!");
                                        sub.deliverToSub(evt);
                                        subAux.updateLastMsgNumber(pubURL);
                                        flushMsgQueue(subAux, sub, pubURL, nextLastMsgNumber);
                                        subsWhoGotMessage.Add(sub.getURL());
                                    }
                                    else
                                    {
                                        Console.WriteLine("arquivar!!!!");
                                        subAux.addToQueue(evt);
                                    }
                                }

                            }
                        }
                        foreach (KeyValuePair<string, List<SubscriberInterface>> entry in setNonTopics(msgTopic))
                        {
                            topic = entry.Key;
                            foreach (SubscriberInterface sub in entry.Value)
                            {
                                if (!subsWhoGotMessage.Contains(sub.getURL()))
                                {
                                    SubAux subAux = subLastMsgReceived.Find(o => o.Sub.getURL() == sub.getURL());
                                    if (subAux.assertPub(pubURL))
                                    {
                                        int nextLastMsgNumber = subAux.lastMsgNumber(pubURL) + 1;
                                        if (msgNumber == nextLastMsgNumber)
                                        {
                                            Console.WriteLine("Entrei mas nao sou topico!");
                                            Console.WriteLine("******************************");
                                            Console.WriteLine("SUBSCRIBER: " + sub.getURL());
                                            Console.WriteLine("PUBLISHER: " + pubURL);
                                            Console.WriteLine("MSG TOPIC = " + msgTopic);
                                            Console.WriteLine("nextLastMsgNumber = " + nextLastMsgNumber);
                                            subAux.updateLastMsgNumber(pubURL);
                                            flushMsgQueue(subAux, sub, pubURL, nextLastMsgNumber);
                                        }
                                        else
                                        {
                                            subAux.addfilteredSeqNumber(pubURL, msgNumber);
                                        }
                                    }
                                }
                            }
                        }

                    }
                    else
                    {
                        foreach (KeyValuePair<string, List<SubscriberInterface>> entry in subscribersByTopic) {
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

                    /*foreach (KeyValuePair<string, List<SubscriberInterface>> entry in subscribersByTopic)
                    {
                        
                        topic = entry.Key;
                        Console.WriteLine("topico da tabela subscribersByTopic: " + topic);
                        Console.WriteLine("TOPICO da mensagem recebida: " + evt.Topic);
                        
                        if (ordering == "FIFO")
                        {
                            foreach (SubscriberInterface sub in entry.Value)
                            {
                                
                                SubAux subAux = subLastMsgReceived.Find(o => o.Sub.getURL() == sub.getURL());                             
                                int nextLastMsgNumber = subAux.lastMsgNumber(pubURL) + 1;
                                Console.WriteLine("******************************");
                                Console.WriteLine("SUBSCRIBER: " + sub.getURL());
                                //adicionar quem é que está a enviar os topicos (mesmo que sejam topicos que o sub nao quer
                                subAux.addTopicProvider(evt.Topic, evt.PublisherName);
                                Console.WriteLine("Msg Number = " + msgNumber);
                                Console.WriteLine("nextLastMsgNumber = " + nextLastMsgNumber);
                                if (topic.Equals(evt.Topic) || isSubtopicOf(evt.Topic, topic))
                                {
                                    
                                    if (msgNumber == nextLastMsgNumber)
                                    {
                                        Console.WriteLine("entrei!!!!!! Msg number = " + msgNumber);
                                        sub.deliverToSub(evt);
                                        subAux.updateLastMsgNumber(pubURL);
                                        flushMsgQueue(subAux, sub, pubURL, nextLastMsgNumber);
                                        
                                    }
                                    else
                                    {
                                        Console.WriteLine("Nao entrei!!!!!! Msg number = " + msgNumber);
                                        subAux.addToQueue(evt);
                                    }
                                }
                                else
                                {
                                    //se a mensagem que chega for de um publisher que nao é um fornecedor para aquele topico
                                    //entao nao nos interessa pois so ia estragar os sequence numbers
                                    if (!(subAux.getTopicProviders(topic).Contains(pubURL))) { continue; }
                                    if (msgNumber == nextLastMsgNumber)
                                    {
                                        Console.WriteLine("Entrei mas nao sou topico! Msg number = " + msgNumber);
                                        subAux.updateLastMsgNumber(pubURL);
                                        flushMsgQueue(subAux, sub, pubURL, nextLastMsgNumber);
                                    }
                                    else
                                    {
                                        subAux.addfilteredSeqNumber(pubURL, msgNumber);
                                    }
                                }
                            }
                        }
                        else {
                            foreach (SubscriberInterface sub in entry.Value)
                            {
                                if (topic.Equals(evt.Topic) || isSubtopicOf(evt.Topic, topic))
                                {
                                    sub.deliverToSub(evt);
                                }
                            }

                        }
                    }*/
                }
            }
        }

        //subconjunto da tabela de topicos -> subs 
        //escolhemos os subtopicos do topico do evento (e o proprio)
        public Dictionary<string, List<SubscriberInterface>> setOfTopics(string topic)
        {
            Dictionary<string, List<SubscriberInterface>> result = new Dictionary<string, List<SubscriberInterface>>();
            string topicKey;
            foreach (KeyValuePair<string, List<SubscriberInterface>> entry in subscribersByTopic)
            {
                topicKey = entry.Key;
                List<SubscriberInterface> aux = new List<SubscriberInterface>();
                if (topicKey.Equals(topic) || isSubtopicOf(topic, topicKey))
                {
                    result.Add(topicKey, entry.Value);
                }               
            }
            return result;
        }


        public Dictionary<string, List<SubscriberInterface>> setNonTopics(string topic)
        {
            Dictionary<string, List<SubscriberInterface>> result = new Dictionary<string, List<SubscriberInterface>>();
            string topicKey;
            foreach (KeyValuePair<string, List<SubscriberInterface>> entry in subscribersByTopic)
            {
                topicKey = entry.Key;
                if (!(topicKey.Equals(topic) || isSubtopicOf(topic, topicKey)))
                {
                    result.Add(topicKey, entry.Value);
                }
            }
            return result;
        }


        //quando recebemos um evento temos de verificar se nao ha eventos anteriormente recebidos que tiveram de ser guardados 
        //porque nao tinha sido recebido um evento anterior. Se houver, temos de entrega-los ao sub
        public void flushMsgQueue(SubAux subAux, SubscriberInterface sub, string pubURL, int nextLastMsgNumber)
        {
            if (subAux.msgQueue(pubURL) != null)
            {
                List<Event> eventsToRemove = new List<Event>();

                foreach (Event pendingEvent in subAux.msgQueue(pubURL))
                {
                    if (pendingEvent.MsgNumber == nextLastMsgNumber)
                    {
                        sub.deliverToSub(pendingEvent);
                        subAux.updateLastMsgNumber(pubURL);
                        eventsToRemove.Add(pendingEvent);
                    }
                }
                subAux.updateMsgQueue(pubURL, eventsToRemove);
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

                if (ordering == "FIFO")
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
                        SubAux subAux = subLastMsgReceived.Find(o => o.Sub.getURL() == sub.getURL());
                        subAux.updatePubs(topic);
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
                    if (!lastMsgNumberByPub.TryGetValue(pubURL, out aux))
                    {
                        lastMsgNumberByPub.Add(pubURL, initLastMsgNumber);
                        msgQueueByPub.Add(pubURL, new List<Event>());
                    }

                    if (lastMsgNumberByPub[pubURL] + 1 == msgNumber)
                    {
                        forwardEvent(this.url, newEvent);
                        lastMsgNumberByPub[pubURL]++;
                        Console.WriteLine("Event Forwarded by Publisher: " + newEvent.PublisherName + ", topic: " + newEvent.Topic);
                        flushPubMsgQueue(pubURL);
                    }
                    else
                    {
                        msgQueueByPub[pubURL].Add(newEvent);
                        msgQueueByPub[pubURL].OrderBy(o => o.MsgNumber).ToList();
                    }
                }
                else
                {
                    forwardEvent(this.url, newEvent);
                    Console.WriteLine("Event Forwarded by Publisher: " + newEvent.PublisherName + ", topic: " + newEvent.Topic);
                }
            }       
        }


        public void flushPubMsgQueue(string pubURL)
        {
            foreach(Event evt in msgQueueByPub[pubURL])
            {
                List<Event> eventsToRemove = new List<Event>();
                int nextLastMsgNumber = lastMsgNumberByPub[pubURL]++;
                if (evt.MsgNumber == nextLastMsgNumber)
                {
                    forwardEvent(this.url, evt);
                    lastMsgNumberByPub[pubURL]++;
                    eventsToRemove.Add(evt);
                    Console.WriteLine("Event Forwarded by Publisher: " + evt.PublisherName + ", topic: " + evt.Topic);
                }
                foreach (Event e in eventsToRemove)
                {
                    msgQueueByPub[pubURL].Remove(e);
                }
                eventsToRemove.Clear();
            }
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
                        entry.Key.forwardInterest(this.url, topic);
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
    }
}
