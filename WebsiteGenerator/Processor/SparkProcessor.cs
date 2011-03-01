using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spark;
using Spark.FileSystem;
using System.IO;

namespace WebsiteGenerator.Processor
{

    public class SparkProcessor : IProcessor
    {
        public class srcViewFile : Spark.FileSystem.IViewFile
        {
            Stream ms;
            public srcViewFile(Stream src)
            {
                ms = src;
            }
            public long LastModified
            {
                get { return DateTime.Now.Ticks; }
            }

            public Stream OpenViewStream()
            {
                return ms;
            }

        }
        public class StringViewFolderFile : Spark.FileSystem.IViewFolder
        {

            private string _path;
            private Stream _source;
            public StringViewFolderFile(string path, Stream source)
            {
                _source = source;
                _path = path;
            }

            public IViewFile GetViewSource(string path)
            {
                if (HasView(path))
                    return new srcViewFile(_source);
                else
                    return null;
            }

            public bool HasView(string path)
            {
                return path == _path;
            }

            public IList<string> ListViews(string path)
            {
                return new List<string>() { _path };
            }

        }

        public class MasterFileLocator : ITemplateLocator
        {
            string _virtualPath;
            string _rootPath;
            public MasterFileLocator(string virtualPath, string rootPath)
            {
                _virtualPath = virtualPath;
                _rootPath = rootPath;
            }
            public LocateResult LocateMasterFile(IViewFolder viewFolder, string masterName)
            {
                var currentFolder = Path.GetDirectoryName(Path.Combine(_rootPath, _virtualPath.TrimStart('\\')));
                var masterPath = Path.Combine(currentFolder, masterName);
                if (!File.Exists(masterPath))
                {
                    masterPath = Path.Combine(_rootPath, masterName);
                }

                return new LocateResult()
                {
                    Path = masterPath.Replace(_rootPath, "").TrimStart('\\')
                };
            }
        }
        public Stream Process(string virtualPath, string rootPath, Stream source, Dictionary<string, string> settings)
        {
            IViewFolder currentFolder =
                new Spark.FileSystem.CombinedViewFolder(
                    new StringViewFolderFile(virtualPath, source),
                    new Spark.FileSystem.FileSystemViewFolder(rootPath)
                );

            /*
            foreach(var dir in Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories))
            {
                currentFolder = new Spark.FileSystem.CombinedViewFolder(
                    currentFolder, 
                    new Spark.FileSystem.FileSystemViewFolder(dir)
                    );
            }
            */
            
            SparkViewEngine _engine = new SparkViewEngine(new Spark.SparkSettings())
            {
                DefaultPageBaseType = typeof(ViewTemplate).FullName,
                ViewFolder = currentFolder,
                TemplateLocator = new MasterFileLocator(virtualPath, rootPath),                
            };
            
            var descriptor = new SparkViewDescriptor()
                .AddTemplate(virtualPath.TrimStart('\\'));

            var view = (ViewTemplate)_engine.CreateInstance(descriptor);

            view.RootPath = rootPath;
            view.VirtualPath = virtualPath;
            source.Dispose();
            var tw = new StringWriter();
            view.RenderView(tw);
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(tw.ToString().Replace("@/", view.AppRelativeFolder)));

            return ms;
        }
        
    }


    public abstract class ViewTemplate : Spark.AbstractSparkView
    {
        public string RootPath { get; set; }
        public string VirtualPath { get; set; }
        public string AppRelativeFolder
        {
            get
            {
                return EvaluateRelativePath(Path.GetDirectoryName(Path.Combine(RootPath, VirtualPath)), RootPath).Replace(Path.DirectorySeparatorChar, '/') + "/";
            }
        }
        internal static string EvaluateRelativePath(string mainDirPath, string absoluteFilePath)
        {
            string[]
            firstPathParts = mainDirPath.Trim(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar);
            string[]
            secondPathParts = absoluteFilePath.Trim(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar);

            int sameCounter = 0;
            for (int i = 0; i < Math.Min(firstPathParts.Length,
            secondPathParts.Length); i++)
            {
                if (
                !firstPathParts[i].ToLower().Equals(secondPathParts[i].ToLower()))
                {
                    break;
                }
                sameCounter++;
            }

            if (sameCounter == 0)
            {
                return absoluteFilePath;
            }

            string newPath = String.Empty;
            for (int i = sameCounter; i < firstPathParts.Length; i++)
            {
                if (i > sameCounter)
                {
                    newPath += Path.DirectorySeparatorChar;
                }
                newPath += "..";
            }
            if (newPath.Length == 0)
            {
                newPath = ".";
            }
            for (int i = sameCounter; i < secondPathParts.Length; i++)
            {
                newPath += Path.DirectorySeparatorChar;
                newPath += secondPathParts[i];
            }
            return newPath;
        }


    }
}
