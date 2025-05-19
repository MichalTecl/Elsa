using Elsa.Common.DbUtils;
using Elsa.Integration.Crm.Raynet;
using Elsa.Integration.Crm.Raynet.Model;
using Elsa.Test.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace RaynetTests
{
    class Program
    {
        static void Main(string[] args)
        {
            var protocol = new RnProtocol(ConfigFactory.Get<RaynetClientConfig>(), ConsoleLogger.Instance);
            IRaynetClient rn = new RnActions(protocol);

            var contacts = GetAllContacts(rn).ToDictionary(c => c.Id, c => c);
            
            var activities = GetAllActivities(rn)
                .Where(a =>
                      (a.Company?.Id ?? 2) != 2 // neni Biorythme nebo soukrome
                   && (a.Status != "CANCELLED")
                 )
                .ToList();

            var script = new ScriptGenerator();
                                   

            foreach(var a in activities)
            {
                if(!contacts.TryGetValue(a.Company.Id, out var company))
                {
                    break;
                }

                a.Description = (a.Description ?? string.Empty).Replace("<p>", "").Replace("</p>", "").Replace("<br>", Environment.NewLine).Trim();

                script.WriteRow("Meeting",
                   new
                   {
                       ActivityId = a.Id,
                       CompanyIco = company.RegNumber,
                       Title = string.IsNullOrWhiteSpace(a.Title) ? a.Id.ToString() : a.Title.Trim(),
                       Description = WebUtility.HtmlDecode(a.Description ?? string.Empty) + $"\n|ELSA: přenos z Raynet; ID={a.Id}",
                       a.Status,
                       ScheduledFrom = a.ScheduledFrom ?? a.ScheduledTill ?? a.Completed,
                       ScheduledTill = a.ScheduledTill ?? a.ScheduledFrom ?? a.Completed,
                       Category = a.Category?.Value,                       
                   });
            }

            File.WriteAllText("C:\\Elsa\\Temp\\rndatascript.sql", script.GetScript(), Encoding.UTF8);


            /*


 INSERT INTO Meeting (CustomerId, Title, Text, StatusId, MeetingCategoryId, StartDt, EndDt, AuthorId)

 SELECT x.Id     CustomerId,
        x.Title,
        x.Description,
        CASE x.Status 
         WHEN 'COMPLETED' THEN (SELECT TOP 1 s.Id FROM MeetingStatus s WHERE s.Title= N'Proběhlo')
         ELSE (SELECT TOP 1 s.Id FROM MeetingStatus s WHERE s.Title = N'Plán')
       END  [MeetingStatusId],
       CASE x.Category
         WHEN N'Telefonická schůzka' THEN (SELECT TOP 1 c.Id FROM MeetingCategory c WHERE c.Title = N'Telefonát')
         WHEN N'Osobní schůzka' THEN (SELECT TOP 1 c.Id FROM MeetingCategory c WHERE c.Title = N'Schůzka')
         ELSE (SELECT TOP 1 c.Id FROM MeetingCategory c WHERE c.Title = N'E-Mail')
       END [MeetingCategoryId],

       x.ScheduledFrom StartDt,
       x.ScheduledTill EndDt,
       2

 FROM (
     select m.*, c.* 
       from @Meeting m
       left join Customer c ON (c.CompanyRegistrationId = m.CompanyIco)
       where c.id is not null
         and not exists(SELECT TOP 1 1 FROM Meeting em WHERE em.Text LIKE '%|ELSA: přenos z Raynet; ID=' + LTRIM(STR(m.ActivityId)))
 ) x



             */
        }

        private static List<Contact> GetAllContacts(IRaynetClient rn) => ReadAllPages(offs => rn.GetContacts(offs));
        private static List<Activity> GetAllActivities(IRaynetClient rn) => ReadAllPages<Activity>(offs => rn.GetActivities(offs));

        private static List<T> ReadAllPages<T>(Func<int, RnResponse<List<T>>> pageGetter)
        {
            var result = new List<T>();

            int offset = 0;
            while (true)
            {
                var data = pageGetter(offset).Data;

                if (data.Count == 0)
                    break;

                result.AddRange(data);
                offset += data.Count;
            }

            return result;
        }
    }
}
