using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Bson;

namespace EyeFaceServiceV2
{
    public class Attraction
    {
        public ObjectId Id { get; set; }
        public string project_name { get; set; }
        public DateTime dateUTC { get; set; }
        public int attention_time { get; set; }
        public int satisfied { get; set; }
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