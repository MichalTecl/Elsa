﻿using Elsa.App.Crm.Entities;
using Elsa.Common.Caching;
using Elsa.Common.Data;
using Elsa.Common.Interfaces;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Repositories
{
    public class CustomerMeetingsRepository
    {
        private readonly IDatabase _database;
        private readonly ISession _session;
        private readonly ICache _cache;

        private readonly AutoRepo<IMeetingCategory> _meetingCategoryRepo;
        private readonly AutoRepo<IMeetingStatusType> _meetingStautsTypeRepo;
        private readonly AutoRepo<IMeetingStatusAction> _meetingStatusActionRepo;
        
        private readonly DistributorsRepository _distributorsRepo;

        public CustomerMeetingsRepository(IDatabase database, ISession session, ICache cache, DistributorsRepository distributorsRepo)
        {
            _database = database;
            _session = session;
            _cache = cache;

            _meetingCategoryRepo = new AutoRepo<IMeetingCategory>(session, database, cache, selectQueryModifier: (db, q) => q.OrderBy(i => i.Title));
            _meetingStautsTypeRepo = new AutoRepo<IMeetingStatusType>(session, database, cache, selectQueryModifier: (db, q) => q.OrderBy(i => i.Title));
            _meetingStatusActionRepo = new AutoRepo<IMeetingStatusAction>(session, database, cache, selectQueryModifier: (db, q) => q.OrderBy(i => i.SortOrder));
            _distributorsRepo = distributorsRepo;
        }

        public List<IMeetingCategory> GetAllMeetingCategories()
        {
            return _meetingCategoryRepo.GetAll();
        }

        public List<IMeetingStatusType> GetMeetingStatusTypes()
        {
            return _meetingStautsTypeRepo.GetAll();
        }

        public List<IMeetingStatusAction> GetMeetingStatusActions(int? currentStatusId)
        {
            return _meetingStatusActionRepo.GetAll().Where(ms => currentStatusId == null || currentStatusId == ms.CurrentStatusTypeId).ToList();
        }

        public List<IMeeting> GetMeetings(int customerId)
        {
            return _database.SelectFrom<IMeeting>()
                 .Join(m => m.Customer)
                 .Join(m => m.Participants)
                 .Join(m => m.MeetingStatuses)
                .Where(m => m.CustomerId == customerId)
                .Where(m => m.Customer.ProjectId == _session.Project.Id)
                .Execute()
                .ToList();
        }
    }
}
