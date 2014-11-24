using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateTVSchedule.TVPocketObject
{
    class TVProgram
    {
        public int refchannel { get; set; }
        public string name { get; set; }
        public string dateStart { get; set; }
        public string timeStart { get; set; }
        public int duration { get; set; }
        public string posterurl { get; set; }
    }
}
