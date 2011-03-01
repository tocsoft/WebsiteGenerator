using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using WebsiteGenerator.Processor;
using System.IO;

namespace WebsiteGenerator
{

    public class Rules : List<Rule>, IProcessor
    {
        public static Rules _defaultRules;
        public static Rules DefaultRules()
        {
            if (_defaultRules == null)
            {
                _defaultRules = new Rules();
                _defaultRules.Add(new Rule()
                {
                    Filter = "*.html",
                    Process = "Spark",
                    Order = 1
                });
                _defaultRules.Add(new Rule()
                {
                    Filter = "*.master",
                    Process = "Skip",
                    Order = 1
                });
                _defaultRules.Add(new Rule()
                {
                    Filter = "*rules.meta",
                    Process = "Skip",
                    Order = 1
                });
                _defaultRules.Add(new Rule()
                {
                    Filter = "*.css",
                    Process = "Less",
                    Order = 1
                });
                _defaultRules.Add(new Rule()
                {
                    Filter = "*.js",
                    Process = "JsMin",
                    Order = 1
                });
            }

            return _defaultRules;
        }
        public Stream Process(string virtualPath, string rootPath, Stream source, Dictionary<string,string> settings)
        {
            var ps = this.Where(x => x.Check(virtualPath)).OrderByDescending(x => x.Order);
            Stream fs = source;

            foreach (var p in ps)
            {
                if (fs == null)
                    return null;
                fs = getProcessor(p).Process(virtualPath, rootPath, fs, p.Settings);
            }

            return fs;
        }


        public IProcessor getProcessor(Rule rule)
        {
                switch (rule.Process)
                {
                    case "Spark":
                        return new Processor.SparkProcessor();
                    case "Skip":
                        return new Processor.EmptyProcessor();
                    case "Less":
                        return new Processor.LessCssProcessor();
                    case "JsMin":
                        return new Processor.JsMinProcessor();                        
                    default:
                        return new PassthroughProcessor();
                }
        }

    }

    public class Rule
    {
        private Regex regex = null;
        public bool Check(string path)
        {
            if (regex == null)
            {
                regex = new Regex(GetPattern(Filter));
            }
            return regex.IsMatch(path);
        }
        public string Filter { get; set; }
        public int Order { get; set; }
        public string Process { get; set; }
        public Dictionary<string, string> Settings { get; set; }

        public static string[] regexEscape = new string[] { "\\", ".", "^", "$", "|", "/", "{", "}", "[", "]", "(", ")", "+" };
        private static string GetPattern(string Namespace)
        {
            string _pattern = Namespace;
            foreach (string c in regexEscape)
                _pattern = _pattern.Replace(c, "\\" + c);

            _pattern = "^" + _pattern.Replace("?", ".?").Replace("*", ".*?") + "$";
            return _pattern;
        }
    }

}