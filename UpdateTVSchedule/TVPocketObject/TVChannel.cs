using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateTVSchedule.TVPocketObject
{
    class TVChannel
    {
        public int id { get; set; }
        public string name { get; set; }
        public string urlcrawl { get; set; }
        public string urlapi { get; set; }
        public string urllogo { get; set; }
        public int refgroup { get; set; }
    }
}
