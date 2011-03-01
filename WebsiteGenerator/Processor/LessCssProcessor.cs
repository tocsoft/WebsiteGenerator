using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace WebsiteGenerator.Processor
{
    class LessCssProcessor : IProcessor
    {

        public System.IO.Stream Process(string virtualPath, string rootPath, System.IO.Stream source, Dictionary<string, string> settings)
        {
            var engine = new dotless.Core.LessEngine();
            if (settings != null)
            {
                bool compress = false;
                if (bool.TryParse(settings["compress"], out compress))
                    engine.Compress = compress;
            }

            var src = new StreamReader(source).ReadToEnd();
            source.Dispose();
            string output = engine.TransformToCss(src, Path.Combine(rootPath, virtualPath.TrimStart('\\')));
            
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(output));
            return ms;
        }

    }
}
