using Elsa.App.Crm.Model;
using Elsa.App.Crm.Repositories;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Crm;
using Elsa.Commerce.Core.Crm.Model;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Commerce;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using Robowire.RoboApi;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
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

        public List<DistributorGridRowModel> GetDistributors(DistributorGridFilter filter, int pageSize, int page)
        {
            return _distributorsRepository.GetDistributors(filter, pageSize, page);
        }
                
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
                var changeTrackingGroupingKey = Guid.NewGuid().ToString();

                var customer = _customerRepository.GetCustomer(rq.CustomerId).Ensure("Invalid CstomerId");

                if (customer.HasStore != rq.HasStore || customer.HasEshop != rq.HasEshop)
                {
                    if (customer.HasEshop != rq.HasEshop)
                    {                        
                        _customerRepository.LogCustomerChange(customer.Id, "E-Shop", customer.HasEshop, rq.HasEshop, changeTrackingGroupingKey);
                        customer.HasEshop = rq.HasEshop;
                    }

                    if (customer.HasStore != rq.HasStore)
                    {                        
                        _customerRepository.LogCustomerChange(customer.Id, "Kamenná prodejna", customer.HasStore, rq.HasStore, changeTrackingGroupingKey);
                        customer.HasStore = rq.HasStore;
                    }

                    _db.Save(customer);
                }

                if (rq.AddedTags != null)
                    rq.AddedTags.ForEach(t => _tagRepo.Assign(new[] { customer.Id }, t, null, changeTrackingGroupingKey));
                

                if (rq.RemovedTags != null)
                    rq.RemovedTags.ForEach(t => _tagRepo.Unassign(new[] { customer.Id }, t, changeTrackingGroupingKey));

                foreach (var storeAddress in rq.ChangedAddresses.Where(a => a.IsDeleted))
                    _distributorsRepository.DeleteStore(customer.Id, storeAddress.AddressName, changeTrackingGroupingKey);
                 
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
                    }, changeTrackingGroupingKey);
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

        public int DoBulkTagging(BulkTaggingRequest rq)
        {
            if ((rq.Filter == null) == (rq.CustomerIds == null || rq.CustomerIds.Count == 0))
                throw new ArgumentException("Request must define CustomerIds or Filter");

            var tag = _tagRepo.GetTagTypes(null).FirstOrDefault(t => t.Id == rq.TagTypeId);
            if (tag == null)
                throw new ArgumentException("Štítek neexistuje, nebo nelze použít.");

            if (rq.Set && tag.RequiresNote == true && string.IsNullOrEmpty(rq.Note))
                throw new InvalidOperationException($"Štítek {tag.Name} vyžaduje textovou poznámku");

            int[] customerIds = (rq.Filter == null) 
                ? rq.CustomerIds.ToArray() 
                : _distributorsRepository.GetDistributors(rq.Filter, null, null, true).Select(d => d.Id).ToArray();

            if (rq.Set)
                return _tagRepo.Assign(customerIds.ToArray(), tag.Id, rq.Note).Count;
            else
                return _tagRepo.Unassign(customerIds.ToArray(), tag.Id).Count;
        }

        public int CountFilterResults(DistributorGridFilter filter)
        {
            return _distributorsRepository.GetDistributors(filter, null, null, true).Count;
        }

        public IReadOnlyCollection<CustomerHistoryEntryModel> GetCustomerHistory(int customerId)
        {
            return _distributorsRepository.GetCustomerHistory(customerId, false, true);
        }

        public IEnumerable<CustomerChanges> GetChangeLog(int customerId)
        {
            return _customerRepository.GetCustomerChanges(customerId);
        }
    }
}
