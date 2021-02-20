using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web;


using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Diagnostics;

namespace BasicServerHTTPlistener
{
    internal class Program
    {
        private static void Main(string[] args)
        {

            //if HttpListener is not supported by the Framework
            if (!HttpListener.IsSupported)
            {
                Console.WriteLine("A more recent Windows version is required to use the HttpListener class.");
                return;
            }
 
 
            // Create a listener.
            HttpListener listener = new HttpListener();

            // Add the prefixes.
            if (args.Length != 0)
            {
                foreach (string s in args)
                {
                    listener.Prefixes.Add(s);
                    // don't forget to authorize access to the TCP/IP addresses localhost:xxxx and localhost:yyyy 
                    // with netsh http add urlacl url=http://localhost:xxxx/ user="Tout le monde"
                    // and netsh http add urlacl url=http://localhost:yyyy/ user="Tout le monde"
                    // user="Tout le monde" is language dependent, use user=Everyone in english 

                }
            }
            else
            {
                Console.WriteLine("Syntax error: the call must contain at least one web server url as argument");
            }
            listener.Start();

            // get args 
            foreach (string s in args)
            {
                Console.WriteLine("Listening for connections on " + s);
            }

            // Trap Ctrl-C on console to exit 
            Console.CancelKeyPress += delegate {
                // call methods to close socket and exit
                listener.Stop();
                listener.Close();
                Environment.Exit(0);
            };


            while (true)
            {
                // Note: The GetContext method blocks while waiting for a request.
                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;

                string documentContents;
                using (Stream receiveStream = request.InputStream)
                {
                    using (StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8))
                    {
                        documentContents = readStream.ReadToEnd();
                    }
                }

                // get url 
                Console.WriteLine($"Received request for {request.Url}");

                //get url protocol
                Console.WriteLine($"Protocol: {request.Url.Scheme}");
                //get user in url
                Console.WriteLine($"User: {request.Url.UserInfo}");
                //get host in url
                Console.WriteLine($"Host: {request.Url.Host}");
                //get port in url
                Console.WriteLine($"Port: {request.Url.Port}");
                //get path in url 
                Console.WriteLine($"Path: {request.Url.LocalPath}");

                // parse path in url & store each segment
                /*string[] segments = new string[] {};
                int i = 0;
                foreach (string str in request.Url.Segments)
                {
                    Console.WriteLine($"Path in URL: {str}");
                    segments[i++] = str;
                }*/

                //get params in url. After ? and between &

                Console.WriteLine($"Query: {request.Url.Query}");

                //request.Url.Query

                //parse params in url
                /*Console.WriteLine("param1 = " + HttpUtility.ParseQueryString(request.Url.Query).Get("param1"));
                Console.WriteLine("param2 = " + HttpUtility.ParseQueryString(request.Url.Query).Get("param2"));
                Console.WriteLine("param3 = " + HttpUtility.ParseQueryString(request.Url.Query).Get("param3"));
                Console.WriteLine("param4 = " + HttpUtility.ParseQueryString(request.Url.Query).Get("param4"));
                */
                //
                
                Console.WriteLine($"doc contents: {documentContents}");

                // Obtain a response object.
                HttpListenerResponse response = context.Response;


                //ex:
                //Mymethods.hello_you("You", "Dublin");
                //Mymethods.duo_state("Mary", "Joe", "old");

                // Construct a response.
                string responseString = Parser.parse_url_and_get_result(request.Url);//"<HTML><BODY> Hello world!</BODY></HTML>";//TODO //Mymethods.hello_you("You", "Dublin");//"<HTML><BODY> Hello world!</BODY></HTML>";//TODO
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                // Get a response stream and write the response to it.
                response.ContentLength64 = buffer.Length;
                System.IO.Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                // You must close the output stream.
                output.Close();
            }
            // Httplistener neither stop ... But Ctrl-C do that ...
            // listener.Stop();
        }
    }


    class Parser
    {

        public static string parse_url_and_get_result(Uri toParse)
        {
            string path = toParse.LocalPath;
            string query = toParse.Query;

            string parsedPath = path.Replace('/', '\\');
            string parsedQuery = "";


            if(toParse.Segments.Length < 1)
            {
                return "No segments";
            }
            // parse path in url & store each segment
            string[] segTmp = new string[toParse.Segments.Length];
            int i = 0;
            bool isCgibin = false;

            Console.WriteLine("Parse segments: ");
            foreach (string str in toParse.Segments)
            {
                segTmp[i++] = str;
                Console.WriteLine("seg[" + i + "]: " + str);
                if (str.Equals("cgi-bin/")) {
                    isCgibin = true;
                }
            }


            string[] queryKeys = HttpUtility.ParseQueryString(toParse.Query).AllKeys;
            foreach (string str in queryKeys)
            {
                parsedQuery += $"{HttpUtility.ParseQueryString(toParse.Query).Get(str)} ";
            }
            Console.WriteLine("Parsed query: " + parsedQuery);

            string[] segments = path.Split('/');
            int size = segments.Length;
 
            if (!(size == 1 && segments[0].Equals(path)))
            {
                string execOrMethodName = segments[size - 1];
                Console.WriteLine("Exec or method: " + execOrMethodName);
                //string[] methSegments = execOrMethodName.Split('.'); --> extension
                if (isCgibin)//"<cgi-bin> //.<ext>"
                {
                    parsedPath += ".exe";
                    Console.WriteLine("Calling cgi: '" + parsedPath + "'");
                    return new Mymethods().cgi_bin(parsedPath, parsedQuery);
                }
                else if(size > 1 && segments[size-2].Equals("mymethods"))//Mymethods
                {
                    Console.WriteLine("Calling mymethods: '" + execOrMethodName + "'");
                    Type type = typeof(Mymethods);
                    MethodInfo method = type.GetMethod(execOrMethodName);
                    if (method == null)
                    {
                        Console.WriteLine("Bad method invokation");
                        return "ERROR";
                    }

                    Mymethods c = new Mymethods();
                    string[] args;
                    if(query != null)
                    {
                        args = new string[] { query };
                    }
                    else
                    {
                        args = null;
                    }
                    string result = (string)method.Invoke(c, args);
                    Console.WriteLine(result);
                    return result;
                }
            }
            return "ERROR";
        }
    }

    class Mymethods
    {
        /** Some methods*/
        //method1
        public string hello_you(string query)
        {

            string name = HttpUtility.ParseQueryString(query).Get("name");
            if (name == null)
                return "Missing parameter 'name'";

            string location = HttpUtility.ParseQueryString(query).Get("location");
            if (location == null)
                return "Missing parameter 'location'";

            return hello_you_parsed(name, location);
        }

        //not necessary -> could have put it directly in above function 
        public string hello_you_parsed(string name, string location)
        {
            return "<HTML><BODY> Hello " + name + " from " + location + "</BODY></HTML>";
        }

        //method2
        public string duo_state(string query)
        {
            string person1 = HttpUtility.ParseQueryString(query).Get("person1");
            if (person1 == null)
                return "Missing parameter 'person1'";

            string person2 = HttpUtility.ParseQueryString(query).Get("person2");
            if (person2 == null)
                return "Missing parameter 'person2'";

            string state = HttpUtility.ParseQueryString(query).Get("state");
            if (state == null)
                return "Missing parameter 'state'";

            return duo_state_parsed(person1, person2, state);
        }

        //not necessary -> could have put it directly in above function 
        public string duo_state_parsed(string person1, string person2, string state)
        {
            return "<HTML><BODY> " + person1 + " and " + person2 + " are " + state + "</BODY></HTML>";
        }

        //execute .exe file
        public string cgi_bin(string execPath, string arguments)
        {
            ProcessStartInfo start = new ProcessStartInfo();


            string[] separator = new string[] { "\\bin" };
            //separate at "\bin\..."
            string path = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName).Split(separator, StringSplitOptions.None)[0];
            //{path}\resources\index.html
            string filePath = path + execPath;

            Console.WriteLine("FilePath: " + filePath);
            //tester si existe
            if (!File.Exists(filePath))
            {
                Console.WriteLine("Cgi not found");
                return "ERROR: cgi file not found";
            }

            start.FileName = filePath;
            start.Arguments = arguments; // Specify arguments.
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            //
            // Start the process.
            //
            using (Process process = Process.Start(start))
            {
                //
                // Read in all the text from the process with the StreamReader.
                //
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    Console.WriteLine(result);
                    //Console.ReadLine();
                    return result;
                }
            }
        }
        
        //same with shell script
        public string cgi_bin_sh(string exec_name, string[] args)
        {
            return "todo";
        }


        /**
         * Question 7 :A partir des questions précédentes, 
         * vous pouvez vous convaincre de la simplicité de ce concept en mettant en place 
         * un programme client qui non seulement enverra des paramètres dans une URL 
         * (comme paramètres d'appel d'une fonction) mais récupérera les données de retour 
         * dans les données renvoyées par le serveur.
         * Vous pouvez par exemple invoquer une méthode incr <param1_val> 
         * (qui incrémente la valeur de val) depuis un client Web qui n’est plus un navigateur WEB.
         * La méthode incr  correspondra à l’invocation d’une méthode de même nom sur le serveur
         * (exemple : http://localhost:8080/webservice/incr?val=5). 
         * Il vous appartiendra alors de définir le format du contenu du message de réponse 
         * qui ne soit plus de l’HTML mais un format lisible et convenu avec le client 
         * (ex. un texte particulier tel que «incr OK val=6», ou du json 
         * (c.f https://fr.wikipedia.org/wiki/JavaScript_Object_Notation)).
         */

        //TODO

    }
}