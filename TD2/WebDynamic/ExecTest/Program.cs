using System;

namespace ExeTest
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2) {
                Console.WriteLine("Arguments missing");
            }
            else
                Console.WriteLine("<HTML><BODY> Hello " + args[0] + " from " + args[1] + "</BODY></HTML>");
        }
        
    }
}
