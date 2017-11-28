using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Models
{
    public class ParsedRequest
    {
        public string name { get; set; }
        public int? age { get; set; }
        public string phone { get; set; }
        public int page { get; set; }

        public string GetError()
        {
            if (name == null && !age.HasValue && phone == null)
                return "Non-empty query is required";
            return null;
        }

        public static bool Parse(string query, int page, out ParsedRequest parsedRequest, out string error)
        {
            error = null;
            parsedRequest = new ParsedRequest { page = page };

            // Parsing logic goes here : we split by "spacey" symbols
            string pattern = "\\s+";
            string replacement = " ";
            Regex rgx = new Regex(pattern);
            query = rgx.Replace(query, replacement);

            var parts = query.Split(' ').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            bool inName = false;
            var ageCandidates = new List<int?>();
            var phoneCandidates = new List<string>();
            var nameCandidates = new List<StringBuilder>();
            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                if (part.All(c => (c >= '0' && c <= '9') || c == '-'))
                {
                    inName = false;
                    if (part.All(c => c >= '0' && c <= '9') && part.Length < 4)
                    {
                        ageCandidates.Add(int.Parse(part));
                    }
                    else
                    {
                        phoneCandidates.Add(part);
                    }
                }
                else
                {
                    if (!inName)
                    {
                        nameCandidates.Add(new StringBuilder());
                    }
                    var last = nameCandidates.Last();
                    if (last.Length > 0) last.Append(" ");
                    last.Append(part);
                    inName = true;
                }
            }

            parsedRequest.age = ageCandidates.LastOrDefault();
            parsedRequest.name = nameCandidates.Any() ? nameCandidates.OrderByDescending(a => a.Length).First().ToString() : null;
            parsedRequest.phone = phoneCandidates.Any() ? phoneCandidates.OrderByDescending(a => a.Length).First().ToString() : null;

            return error == null;
        }
    }
}