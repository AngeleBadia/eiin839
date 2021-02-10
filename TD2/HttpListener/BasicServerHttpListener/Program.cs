using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;

namespace BasicServerHTTPlistener
{
    internal class Header {
        WebHeaderCollection collection;

        public Header(HttpListenerRequest request) {
            this.collection = (WebHeaderCollection)request.Headers;
        }

        public string getOneHeader(HttpRequestHeader header) {
            return collection.Get(header.ToString());
        }
        
        public WebHeaderCollection getHeaders() {
            return this.collection;
        }

        public void printAllHeaders() {
            Console.WriteLine($"{collection}");
        }

        public void printOneHeader(HttpRequestHeader header) {
            Console.WriteLine($"{header}: {collection.Get(header.ToString())}");
        }
    }

    internal class Program
    {
        private static string getRootDirectory()
        {
            string[] separator = new string[]
                    {
                        "\\bin"
                    };
            //separate at "\bin\..."
            string path = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName).Split(separator, StringSplitOptions.None)[0];
            //{path}\resources\index.html
            return path + @"\resources";
        }

        static readonly string HTTP_ROOT = getRootDirectory();

        private static void Main(string[] args)
        {
            
            if (!HttpListener.IsSupported)
            {
                Console.WriteLine("A more recent Windows version is required to use the HttpListener class.");
                return;
            }

            // Create a listener.
            HttpListener listener = new HttpListener();

            // Trap Ctrl-C and exit 
            Console.CancelKeyPress += delegate
            {
                listener.Stop();
                System.Environment.Exit(0);
            };

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
            foreach (string s in args)
            {
                Console.WriteLine("Listening for connections on " + s);
            }

            while (true)
            {
                // Note: The GetContext method blocks while waiting for a request.
                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;
                Header header = new Header(request);

                string documentContents;
                using (Stream receiveStream = request.InputStream)
                {
                    using (StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8))
                    {
                        documentContents = readStream.ReadToEnd();
                    }
                }

                Console.WriteLine($"Received request for {request.Url}");
                Console.WriteLine(documentContents);
                header.printAllHeaders();// Console.WriteLine($"{request.Headers}");

                // Obtain a response object.
                HttpListenerResponse response = context.Response;

                string method = request.HttpMethod;
                string resource = request.RawUrl;//get request url
                string responseString = "";
                string responseBody = "";

                //only treat GET methods
                if(method.Equals("GET"))
                {
                    string filePath = HTTP_ROOT;//get root directory

                    if (resource.Equals("/"))
                        filePath += @"\index.html";// default
                    else
                    {
                        //change all "/" to "\"
                        resource.Replace('/', '\\');
                        filePath += resource;
                    }
                    if (!File.Exists(filePath)) {
                        responseString = "HTTP/1.0 404 NOT FOUND\n\n";
                        responseBody = "ERROR 404 - Not found";
                    }
                    else {
                        responseString = "HTTP/1.0 200 OK\n\n";
                        //suivi de deux sauts de ligne avant le contenu HTML(soit une ligne blanche).
                        // Construct a response.
                        responseBody = File.ReadAllText(filePath);//"<HTML><BODY> Hello world!</BODY></HTML>";
                        responseString += responseBody;
                    }
                }
                else {// != GET method
                    responseString = "Method not supported";
                    responseBody = "ERROR - Not supported";
                }

                byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                // Get a response stream and write the response to it.
                response.ContentLength64 = buffer.Length;
                Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                // You must close the output stream.
                output.Close();

                Console.WriteLine($"Response:\n{responseString}\n");
            }
            // Httplistener neither stop ...
            // listener.Stop();
        }
    }
}