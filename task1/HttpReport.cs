using System.Collections.Generic;

namespace task1
{
    public class HttpReport
    {
        public Dictionary<int, string> Statuses { get; set; }
        public Dictionary<string, int> Reports { get; set; }
        public HttpReport()
        {
            Statuses = new Dictionary<int, string>();
            Reports = new Dictionary<string, int>();
        }
    }
}
