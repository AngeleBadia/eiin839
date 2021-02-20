using System;
using System.Diagnostics;
using System.IO;

class Program
{
    static void Main()
    {
        //
        // Set up the process with the ProcessStartInfo class.
        // https://www.dotnetperls.com/process
        //
        ProcessStartInfo start = new ProcessStartInfo();

        string[] separator = new string[] { "\\bin" };
        //separate at "\bin\..."
        string path = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName).Split(separator, StringSplitOptions.None)[0];
        //{path}\resources\index.html
        string rootDirectory = path + @"\cgi-bin";

        start.FileName = rootDirectory + @"\ExecTest.exe"; // Specify exe name.
        start.Arguments = "Argument1"; // Specify arguments.
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
                Console.ReadLine();
            }
        }
    }
}