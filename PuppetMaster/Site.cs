using System.Collections.Generic;
using CommonTypes;
using System;

namespace PuppetMaster
{

    public class Site {
        string sitename;
        Site parent;
        List<Site> sons;
        string brokerOnSiteURL;
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

        public string BrokerOnSiteURL
        {
            get { return brokerOnSiteURL; }
            set { brokerOnSiteURL = value; }
        }

        public string Sitename
        {
            get { return sitename; }
        }

    }
}

