using EyeFaceServiceV2.Data_Access_Layer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace EyeFaceServiceV2.Controllers
{
    //[Route("api/[controller]")]
    public class PersonController : ApiController
    {
        private DataAccess people = new DataAccess();

        // GET: api/person
        public Task<List<People>> Get()
        {
            return people.GetAllPeople();
        }

        // GET: api/person/(int)
        public Task<People> Get(int id)
        {
            Task<People> person = people.GetPerson(id);
            if (person == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }
            return person;
        }

        // DELETE: api/person/(int)
        public async void Delete(int id)
        {
            await people.RemovePerson(id);
            if (!(await people.RemovePerson(id)))
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }
        }

        // PUT: api/person/(int,int)
        public async void Put(int id, int age)
        {
            if (!(await people.UpdatePerson(id, age)))
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }
        }

        // PUT: api/person/(string)
        public async void Put(string name)
        {
            if (!(await people.UpdateProject(name)))
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }
        }

    }
}
