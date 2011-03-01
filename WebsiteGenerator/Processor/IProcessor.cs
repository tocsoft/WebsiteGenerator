using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace WebsiteGenerator.Processor
{
    public interface IProcessor
    {
        Stream Process(string virtualPath, string rootPath, Stream source, Dictionary<string, string> settings);
    }
}
