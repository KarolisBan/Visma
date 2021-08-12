using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Visma.Entities
{
    public class Book
    {
        public double ISBN { get; set; }
        public string Name { get; set; }
        public string Author { get; set; }
        public string Category { get; set; }
        public string Language { get; set; }
        public DateTime? Publication { get; set; }
        public bool LogTaken { get; set; }
        public string LogTakenBy { get; set; }
        public DateTime? LogTakeOutDate { get; set; }
        public DateTime? LogPlannedReturnDate { get; set; }

        public Book()
        {
            Publication = null;
            LogTaken = false;
            LogTakenBy = string.Empty;
            LogTakeOutDate = null;
            LogPlannedReturnDate = null;
        }

    }
}
