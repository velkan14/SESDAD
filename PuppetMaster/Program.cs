using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PuppetMaster
{
    class Program
    {
        private const int MAX_COMMAND_NUM= 8;

        static void Main(string[] args)
        {
            PM pm = new PM();

            /**
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

            while (input != "Exit" || input != "exit")
            {
                input = System.Console.ReadLine();
                for (int commandNumber = 0; commandNumber < MAX_COMMAND_NUM; commandNumber++)
                {
                    match = Regex.Match(input, pattern[commandNumber]);
                    if (match.Success)
                    {
                   
                        string[] words = match.Captures[0].Value.Split(' ');

                        switch (commandNumber+1)
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
