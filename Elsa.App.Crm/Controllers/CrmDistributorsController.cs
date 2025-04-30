using Elsa.App.Crm.Model;
using Elsa.App.Crm.Repositories;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Crm;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Commerce;
using Robowire.RoboApi;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Elsa.App.Crm.Repositories.DistributorsRepository;

namespace Elsa.App.Crm.Controllers
{
    [Controller("CrmDistributors")]
    public class CrmDistributorsController : ElsaControllerBase
    {
        private readonly DistributorsRepository _distributorsRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IUserRepository _userRepo;
        private readonly CustomerTagRepository _tagRepo;
        private readonly IDatabase _db;
        private readonly DistributorFiltersRepository _distributorFilters;

        public CrmDistributorsController(IWebSession webSession, ILog log, DistributorsRepository distributorsRepository, ICustomerRepository customerRepository, IUserRepository userRepo, CustomerTagRepository tagRepo, IDatabase db, DistributorFiltersRepository distributorFilters) : base(webSession, log)
        {
            _distributorsRepository = distributorsRepository;
            _customerRepository = customerRepository;
            _userRepo = userRepo;
            _tagRepo = tagRepo;
            _db = db;
            _distributorFilters = distributorFilters;
        }

        public List<DistributorGridRowModel> GetDistributors(DistributorGridFilter filter, int pageSize, int page, string sorterId)
        {
            return _distributorsRepository.GetDistributors(filter, pageSize, page, sorterId);
        }

        public DistributorSorting[] GetSortingTypes() => DistributorSorting.Sortings;

        public DistributorDetailViewModel GetDetail(int customerId) => _distributorsRepository.GetDetail(customerId);

        public List<DistributorAddressViewModel> GetAddresses(int customerId) => _distributorsRepository.GetDistributorAddresses(customerId);

        public IEnumerable<CustomerNoteViewModel> GetNotes(int customerId)
        {
            var usindex = _userRepo.GetUserIndex();

            return _customerRepository.GetCustomerRelatedNotes(customerId)
                .Select(n => new CustomerNoteViewModel 
                {
                    Id = n.Id,
                    Author = usindex.Get(n.AuthorId, null)?.EMail,
                    NoteDt = StringUtil.FormatDateTime(n.CreateDt),
                    Text = n.Body,
                    IsOwn = n.AuthorId == WebSession.User.Id,
                });
        }

        public IEnumerable<CustomerNoteViewModel> AddNote(int customerId, string text)
        {
            _customerRepository.AddCustomerNote(customerId, text);

            return GetNotes(customerId);
        }

        public IEnumerable<CustomerNoteViewModel> DeleteNote(int customerId, int noteId)
        {
            _customerRepository.DeleteCustomerNote(noteId);

            return GetNotes(customerId);
        }

        public void Save(DistributorChangeRequestModel rq)
        {
            EnsureUserRight(CrmUserRights.DistributorsAppEdits);

            using(var tx = _db.OpenTransaction())
            {
                var customer = _customerRepository.GetCustomer(rq.CustomerId).Ensure("Invalid CstomerId");

                if (customer.HasStore != rq.HasStore || customer.HasEshop != rq.HasEshop)
                {
                    customer.HasEshop = rq.HasEshop;
                    customer.HasStore = rq.HasStore;

                    _db.Save(customer);
                }

                if (rq.AddedTags != null)
                    rq.AddedTags.ForEach(t => _tagRepo.Assign(customer.Id, t));

                if (rq.RemovedTags != null)
                    rq.RemovedTags.ForEach(t => _tagRepo.Unassign(customer.Id, t));

                foreach (var storeAddress in rq.ChangedAddresses.Where(a => a.IsDeleted))
                    _distributorsRepository.DeleteStore(customer.Id, storeAddress.AddressName);
                 
                foreach(var address in rq.ChangedAddresses.Where(a => !a.IsDeleted))
                {
                    _distributorsRepository.SaveStore(customer.Id, address.AddressName, s => {
                        s.Name = address.StoreName;
                        s.Address = address.Address;
                        s.City = address.City;
                        s.Www = address.Www;
                        s.PreviewName = address.StoreName;
                        s.Lat = address.Lat;
                        s.Lon = address.Lon;
                    });
                }

                tx.Commit();
            }
        }

        public DistributorOrderInfoPage GetOrders(int distributorId, long? pageKey)
        {
            const int pageSize = 10;

            var rows = _db.Sql()
                .Call("GetDistributorOrdersOverview")
                .WithParam("@customerId", distributorId)
                .WithParam("@pageSize", pageSize)
                .WithParam("@lastSeenId", pageKey)
                .AutoMap<DistributorOrderInfo>();

            return new DistributorOrderInfoPage(rows, pageSize);
        }
        
        public DistributorFilterValidationResult ValidateFilter(DistributorFilterModel filter)
        {
            try
            {
                var result = _distributorFilters.Execute(filter);
                return new DistributorFilterValidationResult { IsValid = true, NumberOfRecords = result.Ids.Count, FilterText = result.FilterText  };
            }
            catch (Exception ex)
            {
                return new DistributorFilterValidationResult { IsValid = false, ErrorMessage = ex.Message }; 
            }
        }

        protected override void OnBeforeCall()
        {
            EnsureUserRight(CrmUserRights.DistributorsApp);
            base.OnBeforeCall();
        }
    }
}
