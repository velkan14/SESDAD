using System.Collections.Generic;
using CommonTypes;
using System;
using CommonTypesPM;

namespace PuppetMaster
{

    public class Site {
        string sitename;
        Site parent;
        List<Site> sons;
        string[] brokerReplicas = new string[3];
        string[] replicasName = new string[3];
        string brokerOnSiteURL = "NULL";
        int leaderCounter = 0;
        //um broker por site. Por enquanto...

        public Site(string sitename, Site parent)
        {
            this.sitename = sitename;
            this.parent = parent;
            sons = new List<Site>();
        }

        public void addSon(Site newSon)
        {
            sons.Add(newSon);
        }

        public List<string> getSonsBrokersURLs()
        {
            List<string> res = new List<string>();
            foreach (Site son in sons)
            {
                res.Add(son.BrokerOnSiteURL);
            }
            return res;

        }

        public string getDad()
        {
            if (parent == null) return "none";
            return parent.BrokerOnSiteURL;

        }

        public string BrokerOnSiteURL
        {
            get { return brokerOnSiteURL; }
            set { brokerOnSiteURL = value; }
        }

        public int LeaderCounter
        {
            get { return leaderCounter; }
            set { leaderCounter = value; }
        }

        public string Sitename
        {
            get { return sitename; }
        }
        public string[] Replicas
        {
            get { return brokerReplicas; }
        }

        public string[] ReplicasName
        {
            get { return replicasName; }
        }

        internal void addReplica(string uRL, string processName)
        {
            brokerReplicas[leaderCounter] = uRL;
            replicasName[leaderCounter++] = processName;
        }
    }
}

