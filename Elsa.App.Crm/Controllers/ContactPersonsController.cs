using Elsa.App.Crm.Entities;
using Elsa.App.Crm.Model;
using Elsa.Commerce.Core.Crm;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Data;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Crm;
using Robowire.RoboApi;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Controllers
{
    [Controller("contactPersons")]
    public class ContactPersonsController : ElsaControllerBase
    {        
        private readonly IDatabase _database;
        private readonly ICustomerRepository _customerRepository;

        public ContactPersonsController(IWebSession webSession, ILog log, IDatabase database, ICustomerRepository customerRepository) : base(webSession, log)
        {
            _database = database;
            _customerRepository = customerRepository;
        }

        public IEnumerable<ContactPersonModel> Get(int customerId)
        {
            return _database.SelectFrom<ICustomerContactPerson>()
                .Join(b => b.Person)
                .Where(p => p.CustomerId == customerId)
                .Execute()
                .OrderBy(p => string.IsNullOrWhiteSpace(p.Person.ExternalId) ? 0 : 1)
                .ThenByDescending(p => p.Id)
                .Select(p => new ContactPersonModel
                {
                    Id = p.Person.Id,
                    Name = p.Person.Name,
                    Email = p.Person.Email,
                    Phone = p.Person.Phone,
                    Note = string.IsNullOrWhiteSpace(p.Person.ExternalId) ? p.Person.Note : "(zdroj: Flox)",
                    IsSystem = !string.IsNullOrWhiteSpace(p.Person.ExternalId)
                });
        }

        public IEnumerable<ContactPersonModel> Save(int customerId, ContactPersonModel model)
        {
            using (var tx = _database.OpenTransaction())
            {
                IPerson person;

                if (model.Id > 0)
                {
                    person = _database.SelectFrom<IPerson>().Where(p => p.Id == model.Id).Execute().FirstOrDefault() ?? throw new Exception("Invalid person reference");

                    if (!string.IsNullOrEmpty(person.ExternalId))
                        throw new InvalidOperationException("Není možné měnit kontakty definované v ERP");
                }
                else
                {
                    person = _database.New<IPerson>();
                }

                var originalState = person.GetState();

                person.Email = model.Email;
                person.Phone = model.Phone;
                person.Note = model.Note;
                person.Name = model.Name;

                person.GetState().FindChanges(originalState, (key, oldVal, newVal) => { 
                    _customerRepository.LogCustomerChange(customerId, $"Změna kontaktní osoby {person.Name} - {key}", oldVal, newVal, Guid.NewGuid().ToString());
                });

                _database.Save(person);

                var bridge = _database.SelectFrom<ICustomerContactPerson>()
                    .Where(ccp => ccp.CustomerId == customerId && ccp.PersonId == model.Id)
                    .Execute()
                    .FirstOrDefault();

                if (bridge == null)
                {
                    bridge = _database.New<ICustomerContactPerson>();
                    bridge.CustomerId = customerId;
                    bridge.PersonId = person.Id;

                    _database.Save(bridge);
                }

                tx.Commit();
            }

            return Get(customerId);
        }

        public IEnumerable<ContactPersonModel> Delete(int customerId, int personId)
        {
            using (var tx = _database.OpenTransaction())
            {
                var personBridges = _database.SelectFrom<ICustomerContactPerson>()
                    .Join(b => b.Person)
                    .Where(ccp => ccp.PersonId == personId)
                    .Execute()
                    .ToList();

                if (personBridges.Any(p => !string.IsNullOrWhiteSpace(p.Person.ExternalId)))
                    throw new InvalidOperationException("Neni mozne mazat systemove osoby");

                if (personBridges.All(b => b.CustomerId == customerId))
                {
                    _database.DeleteAll(personBridges);
                    _database.Delete(personBridges.First().Person);
                }
                else
                {
                    _database.DeleteAll(personBridges.Where(b => b.CustomerId == customerId));
                }

                _customerRepository.LogCustomerChange(customerId, "Smazání kontaktní osoby {personBridges.First().Person.Name}", null, DateTime.Now, Guid.NewGuid().ToString());

                tx.Commit();
            }

            return Get(customerId);
        }
    }
}
