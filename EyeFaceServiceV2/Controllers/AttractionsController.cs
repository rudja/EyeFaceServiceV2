using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using EyeFaceDataAccess;

namespace EyeFaceServiceV2.Controllers
{
    public class AttractionsController : ApiController
    {
        private EyeFaceDBEntities db = new EyeFaceDBEntities();

        // GET: api/Attractions
        public IQueryable<Attraction> GetAttraction()
        {
            return db.Attraction;
        }

        // GET: api/Attractions/5
        [ResponseType(typeof(Attraction))]
        public async Task<IHttpActionResult> GetAttraction(int id)
        {
            Attraction attraction = await db.Attraction.FindAsync(id);
            if (attraction == null)
            {
                return NotFound();
            }

            return Ok(attraction);
        }

        // PUT: api/Attractions/5
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutAttraction(int id, Attraction attraction)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != attraction.Id)
            {
                return BadRequest();
            }

            db.Entry(attraction).State = EntityState.Modified;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AttractionExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/Attractions
        [ResponseType(typeof(Attraction))]
        public async Task<IHttpActionResult> PostAttraction(Attraction attraction)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.Attraction.Add(attraction);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (AttractionExists(attraction.Id))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtRoute("DefaultApi", new { id = attraction.Id }, attraction);
        }

        // DELETE: api/Attractions/5
        [ResponseType(typeof(Attraction))]
        public async Task<IHttpActionResult> DeleteAttraction(int id)
        {
            Attraction attraction = await db.Attraction.FindAsync(id);
            if (attraction == null)
            {
                return NotFound();
            }

            db.Attraction.Remove(attraction);
            await db.SaveChangesAsync();

            return Ok(attraction);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool AttractionExists(int id)
        {
            return db.Attraction.Count(e => e.Id == id) > 0;
        }
    }
}