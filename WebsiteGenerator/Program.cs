using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using HttpServer;
using System.Text;
using HttpServer.Headers;

namespace WebsiteGenerator
{
    class Program
    {
        static string root;
        static string rulesEnginePath;
        static DateTime lastUpdated = DateTime.MinValue;
        static Rules _rulesEngine = new Rules();
        static Rules rulesEngine
        {
            get { 
                
                var rules = Path.Combine(root, "rules.meta");
                if (!File.Exists(rules))
                {
                    return Rules.DefaultRules();
                }

                var lastupdate = File.GetLastWriteTime(rules);
                if (lastupdate > lastUpdated)
                {
                    _rulesEngine = Newtonsoft.Json.JsonConvert.DeserializeObject<Rules>(File.ReadAllText(rules));
                    lastUpdated = lastupdate;
                }

                return _rulesEngine;
            }
        }
        static void Main(string[] args)
        {
            var a = args.ToList();
            var exportIndex = a.IndexOf("-e");
            var portIndex = a.IndexOf("-p");


             root = System.Environment.CurrentDirectory;

            if (exportIndex > -1)
            {
                //export all files mode;
                var exportfolder = args[exportIndex + 1];
                Console.WriteLine("Exporting site to {0}", exportfolder);
                foreach(var file in Directory.GetFiles(root, "*.*", SearchOption.AllDirectories))
                {
                    var virtualPath = file.Replace(root, "").TrimStart('\\');
                    
                    var newPath = Path.Combine(exportfolder, virtualPath);
                    var dir = Path.GetDirectoryName(newPath);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    Stream s = ProcessPath(virtualPath);
                    if (s != null)
                    {
                        using (var fs = File.Create(newPath))
                        {
                            CopyStream(s, fs);
                        }
                        s.Dispose();
                    }
                    
                }
            }
            else
            {
                //launch website mode
                int port;
                if(portIndex == -1 || !int.TryParse(args[portIndex + 1], out port))
                {
                    port = 8089;
                }
                HttpListener listener = HttpServer.HttpListener.Create(System.Net.IPAddress.Any, port);
                listener.RequestReceived += new EventHandler<RequestEventArgs>(listener_RequestReceived);
                listener.Start(5);
                Console.WriteLine("Launching website...");
                string url = "http://localhost:" + listener.Port.ToString();
                Console.WriteLine("Open {0} to see a live version or hit 'R' on the keyboard to launch a browser window", url);
                Console.WriteLine("Hit 'Esc' to stop server");
                Console.WriteLine("use \"-e <folder>\" to export.");
                Console.WriteLine("use \"-p <port>\" to change port default(8089)", listener.Port);
                while (true)
                {
                    var key = Console.ReadKey(true).Key;
                    bool stop = false;
                    switch (key)
                    {
                        case ConsoleKey.Escape:
                            stop = true;
                            break;
                        case ConsoleKey.R:
                            Process.Start(url);
                            break;
                        default:
                            break;
                    }
                    if (stop)
                        break;

                }
                listener.Stop();
            }
        }

        static Stream ProcessPath(string virtualPath)
        {
            return ProcessPath(virtualPath, null);
        }
        static Stream ProcessPath(string virtualPath, string defaultFilename)
        {
            MemoryStream msOutput = new MemoryStream();
            string path = Path.Combine(root, virtualPath);
            if (Directory.Exists(path) && !string.IsNullOrEmpty(defaultFilename))
            {
                path = Directory.GetFiles(path, defaultFilename).First();
                virtualPath = path.Replace(root, "");
            }
            if (File.Exists(path))
            {
                var src = File.OpenRead(path);
                try
                {
                    return rulesEngine.Process(virtualPath, root, src, null);
                }
                catch (Exception ex)
                {
                    byte[] buffer = Encoding.UTF8.GetBytes("<html><body><pre>" + ex.Message + "</pre></body></html>");
                    msOutput.Write(buffer, 0, buffer.Length);
                }
            }
            msOutput.Position = 0;
            return msOutput;
        }

        static void listener_RequestReceived(object sender, RequestEventArgs e)
        {
            var virtualPath = e.Request.Uri.AbsolutePath.TrimStart('/').Replace("/","\\");
            var stream = ProcessPath(virtualPath, "index.html");
            if (stream != null)
                CopyStream(stream, e.Response.Body);
            
        }
        public static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[32768];
            while (true)
            {
                int read = input.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                    return;
                output.Write(buffer, 0, read);
            }
        }

    }
}
