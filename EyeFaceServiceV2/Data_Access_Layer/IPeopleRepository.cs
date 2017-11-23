using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EyeFaceServiceV2
{
    public interface IPeopleRepository
    {
        //Get all the list of people in database
        //IEnumerable<People> GetAllPeople();
        Task<List<People>> GetAllPeople();

        //Get only one person in database 
        //People GetPerson(int id);
        Task<People> GetPerson(int id);

        //Remove a row about a person
        Task<bool> RemovePerson(int id);

        //Update the person's attributes into the database 
        Task<bool> UpdatePerson(int id, People item);

        //Update the name of a project into all database
        Task<bool> UpdateProject(string name);
    }
}
