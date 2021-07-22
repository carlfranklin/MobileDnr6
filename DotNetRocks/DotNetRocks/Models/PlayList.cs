using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetRocks.Models
{
    public class PlayList
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public DateTime DateCreated { get; set; }
        public List<Show> Shows { get; set; } = new List<Show>();
    }
}
