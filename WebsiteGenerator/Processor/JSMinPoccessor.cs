using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace WebsiteGenerator.Processor
{
    public class JsMinProcessor : IProcessor
    {

        public System.IO.Stream Process(string virtualPath, string rootPath, System.IO.Stream source, Dictionary<string, string> settings)
        {
            var min = new JSMin.JavaScriptMinifier();
            var src = new StreamReader(source).ReadToEnd();
            source.Dispose();

            var output = min.Minify(src);

            return new MemoryStream(Encoding.UTF8.GetBytes(output));
        }

    }
}
