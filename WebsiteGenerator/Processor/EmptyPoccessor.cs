using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebsiteGenerator.Processor
{
    public class EmptyProcessor : IProcessor
    {

        public System.IO.Stream Process(string virtualPath, string rootPath, System.IO.Stream source, Dictionary<string, string> settings)
        {
            source.Dispose();
            return null;
        }

    }
}
