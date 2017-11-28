using System;
using System.Collections.Generic;

namespace Models
{
    public class SearchResult
    {
        public IEnumerable<Person> data{ get; set; } 
        public string log{ get; set; }
        public string requestId{ get; set; }
        public DateTime timestamp { get; set; }
        public long duration { get; set; }

        public int? page { get; set; }

        public bool hasPrev { get; set; }
        public bool hasNext { get; set; }
    }


}
