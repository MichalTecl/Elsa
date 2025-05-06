using Elsa.App.Crm.Entities;
using Elsa.App.Crm.Model;
using Elsa.App.Crm.Repositories;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Newtonsoft.Json;
using Robowire.RoboApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Controllers
{
    [Controller("CrmRobots")]
    public class CrmRobotsController : ElsaControllerBase
    {
        private readonly DistributorFiltersRepository _filters;
        private readonly CustomerTagRepository _tags;

        public CrmRobotsController(IWebSession webSession, ILog log, DistributorFiltersRepository filters, CustomerTagRepository tags) : base(webSession, log)
        {
            _filters = filters;
            _tags = tags;
        }

        public List<CrmRobotModel> GetRobots()
        {
            var tagIndex = _tags.GetTagTypes(false, true).ToDictionary(t => t.Id, t => t.Name);
            var allMapped = _filters.GetAllRobots(false)
                .Select(r => MapRobot(r, tagIndex))
                .OrderBy(r => r.IsActive ? 0 : 1)
                .ThenBy(r => r.SequenceOrder)
                .ToList();

            CrmRobotModel previous = null;
            foreach(var b in allMapped)
            {
                if (previous != null)
                {
                    previous.CanMoveDown = b.IsActive;
                    b.CanMoveUp = b.IsActive;
                }

                if (!b.IsActive)
                    break;

                previous = b;
            }

            return allMapped;
        }

        public List<CrmRobotModel> MoveRobotSequence(int robotId, int direction)
        {
            var allRobots = _filters.GetAllRobots(false).OrderBy(r => r.SequenceOrder).ToList();

            var robotA = allRobots.FirstOrDefault(r => r.Id == robotId).Ensure();
            var inx = allRobots.IndexOf(robotA) + direction;

            if (inx < 0 || inx >= allRobots.Count)
                throw new ArgumentException("Invalid operation direction");

            var robotB = allRobots[inx];

            _filters.SaveRobot(robotA.Id, r => r.SequenceOrder = robotB.SequenceOrder);
            _filters.SaveRobot(robotB.Id, r => r.SequenceOrder = robotA.SequenceOrder);

            return GetRobots();
        }

        public List<CrmRobotModel> ChangeRobotActive(int robotId, bool activate)
        {
            _filters.SaveRobot(robotId, r => { 
                if (activate)
                {
                    if (r.ActiveFrom >= DateTime.Now)
                        r.ActiveFrom = DateTime.Now;

                    if (r.ActiveTo != null && r.ActiveTo < DateTime.Now)
                        r.ActiveTo = null;
                }
                else
                {
                    r.ActiveTo = DateTime.Now;
                }
            });

            return GetRobots();
        }

        public List<CrmRobotModel> SaveRobot(CrmRobotModel model)
        {
            var tagIdIndex = _tags.GetTagTypes(false, true).ToDictionary(t => t.Name, t => t.Id);

            int? getTagId(string tagName)
            {
                if (string.IsNullOrEmpty(tagName))
                    return null;

                if (!tagIdIndex.TryGetValue(tagName, out var tagId))
                    throw new ArgumentException($"Neznámý typ štítku \"{tagName}\"");

                return tagId;
            }

            _filters.SaveRobot(model.Id, r => {
                r.Name = StringUtil.TrimAndValidateNonEmpty(model.Name, () => "Robot musí mít název!");
                r.Description = model.Description;
                r.NotifyMailList = string.Join(";", model.MailRecipients ?? Enumerable.Empty<string>());
                r.JsonData = JsonConvert.SerializeObject(model);
                r.FilterMatchRemovesTagTypeId = getTagId(model.MatchRemovesTagTypeName);
                r.FilterMatchSetsTagTypeId = getTagId(model.MatchSetsTagTypeName);
                r.FilterUnmatchRemovesTagTypeId = getTagId(model.UnmatchRemovesTagTypeName);
                r.FilterUnmatchSetsTagTypeId = getTagId(model.UnmatchSetsTagTypeName);
            });

            return GetRobots();
        }

        private CrmRobotModel MapRobot(ICrmRobot robot, Dictionary<int, string> tagTypeNameIndex)
        {
            string toTagName(int? tagTypeId)
            {
                if (tagTypeId == null)
                    return null;

                return tagTypeNameIndex[tagTypeId.Value];
            }

            return new CrmRobotModel
            {
                Id = robot.Id,
                SequenceOrder = robot.SequenceOrder,
                Name = robot.Name,
                Description = robot.Description,
                Author = robot.Author.EMail,
                IsActive = robot.ActiveFrom <= DateTime.Now && (robot.ActiveTo == null || robot.ActiveTo > DateTime.Now),
                MatchSetsTagTypeName = toTagName(robot.FilterMatchSetsTagTypeId),
                MatchRemovesTagTypeName = toTagName(robot.FilterMatchRemovesTagTypeId),
                UnmatchSetsTagTypeName = toTagName(robot.FilterUnmatchSetsTagTypeId),
                UnmatchRemovesTagTypeName = toTagName(robot.FilterUnmatchRemovesTagTypeId),
                MailRecipients = robot.GetRecipients().ToList()
            };            
        }
    }
}
