using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EyeFaceApplication
{
    public class Attraction
    {
        public ObjectId Id { get; set; }
        public string project_name { get; set; }
        public DateTime dateUTC { get; set; }
        public double attention_time { get; set; }
        public int satisfied { get; set; }

        public Attraction()
        {
            project_name = "";
            dateUTC = DateTime.UtcNow;
            attention_time = 0;
            satisfied = 0;

        }
    }

    public class People
    {
        public ObjectId Id { get; set; }
        public int person_id { get; set; }
        public string gender { get; set; }
        public int age { get; set; }
        public string ancestry { get; set; }
        public List<Attraction> attractions { get; set; }
    }
}
