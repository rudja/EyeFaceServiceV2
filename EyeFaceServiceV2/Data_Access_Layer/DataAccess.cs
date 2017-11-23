using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using System.Configuration;

namespace EyeFaceServiceV2.Data_Access_Layer
{
    public class DataAccess
    {
        IMongoCollection<People> collection;

        public DataAccess()
        {
            var connectionString = "mongodb://localhost";
            //The connectionString key is in the Web.config file
            //var connectionString = ConfigurationManager.AppSettings["connectionString"];
            MongoClient client = new MongoClient(connectionString);
            var db = client.GetDatabase("EyeFaceDB");
            collection = db.GetCollection<People>("People");
            //Here we can store some data into the database to begin. 
        }

        //public async List<People> GetAllPeople()
        public async Task<List<People>> GetAllPeople()
        {
            List<People> result = new List<People>();
            var filter = new BsonDocument();
            try
            {
                using (var cursor = await collection.FindAsync(filter))
                {
                    while (await cursor.MoveNextAsync())
                    {
                        var batch = cursor.Current;
                        foreach (var document in batch)
                        {
                            // process document
                            //Add the current document to the list
                            result.Add(document);
                        }
                    }
                    return result;
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public async Task<People> GetPerson(int id)
        {
            var filter = Builders<People>.Filter.Eq("person_id", id);
            var result = new People();
            try
            {
                using (var cursor = await collection.FindAsync(filter))
                {
                    while (await cursor.MoveNextAsync())
                    {
                        var batch = cursor.Current;
                        foreach (var document in batch)
                        {
                            // process document
                            result = document;
                        }
                    }
                    return result;
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public async Task<bool> RemovePerson(int id)
        {
            var filter = Builders<People>.Filter.Eq("person_id", id);
            try
            {
                var result = await collection.DeleteManyAsync(filter);
                if (result.DeletedCount == 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                throw e;
            }  
        }

        //We can only update age of the person, because gender, ancestry are not suppose to change.
        public async Task<bool> UpdatePerson(int id, int age)
        {
            var filter = Builders<People>.Filter.Eq("person_id", id);
            var update = Builders<People>.Update
                .Set("age", age);
            try
            {
                var result = await collection.UpdateOneAsync(filter, update);       //UpdateOneAsync because we are supposed to find only on document with this person_id
                if (result.ModifiedCount == 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        //Change the name of a project in all database
        public async Task<bool> UpdateProject(string name)
        {
            var filter = Builders<People>.Filter.Eq("attraction.project_name", name);
            var update = Builders<People>.Update
                .Set("attraction.project_name", name);
            try
            {
                var result = await collection.UpdateManyAsync(filter, update);
                if (result.ModifiedCount == 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                throw e;
            }

        }
    }
}



 