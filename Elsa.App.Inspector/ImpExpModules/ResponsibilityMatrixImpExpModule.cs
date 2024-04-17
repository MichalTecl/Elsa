using Elsa.App.ImportExport;
using Elsa.App.Inspector.Repo;
using Elsa.Commerce.Core;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Inspector.ImpExpModules
{
    public class ResponsibilityMatrixImpExpModule : XlsImportExportModuleBase<ResponsibilityMatrixRowModel>
    {
        private readonly IInspectionsRepository _inspectionsRepository;
        private readonly IUserRepository _users;

        public ResponsibilityMatrixImpExpModule(IInspectionsRepository inspectionsRepository, IUserRepository users, IDatabase db) : base(db)
        {
            _inspectionsRepository = inspectionsRepository;
            _users = users;
        }

        public override string Title => "Odpovědné osoby Inspektor";

        public override string Description => "Import a export nastavení odpovědných uživatelů pro jednotlivé typy inspekcí";

        protected override List<ResponsibilityMatrixRowModel> ExportData(out string exportFileName, IDatabase db)
        {
            exportFileName = "Uživatelé_Inspektor.xlsx"; 

            var alUsers = _users.GetAllUsers();
            var allInspecitons = _inspectionsRepository.GetIssueTypes().OrderBy(i => i.Name);
            var respMatrix = _inspectionsRepository.GetResponsibilityMatrix().ToList();

            var result = new List<ResponsibilityMatrixRowModel>();

            foreach(var inspectionType in allInspecitons)
            {
                var responsibles = respMatrix.Where(r => r.InspectionTypeId == inspectionType.Id).OrderBy(r => r.DaysAfterDetect).ToList();
                
                if (responsibles.Count == 0)
                { 
                    result.Add(new ResponsibilityMatrixRowModel
                    {
                        IssueTypeName = inspectionType.Name
                    });

                    continue;
                }

                foreach(var responsible in responsibles)
                {
                    result.Add(new ResponsibilityMatrixRowModel
                    {
                        IssueTypeName = inspectionType.Name,
                        Days = responsible.DaysAfterDetect,
                        MailAlias = responsible.EMailOverride,
                        UserName = alUsers.FirstOrDefault(u => u.Id == responsible.ResponsibleUserId)?.EMail 
                    });
                }
            }

            return result;
        }

        protected override string ImportDataInTransaction(List<ResponsibilityMatrixRowModel> data, IDatabase db, ITransaction tx)
        {
            int changes = 0;

            // load all users, all inspections, all responsibility matrix
            var allUsers = _users.GetAllUsers();
            var allInspections = _inspectionsRepository.GetIssueTypes().ToList();
            var respMatrix = _inspectionsRepository.GetResponsibilityMatrix().ToList();
            

            // iterate through all inspection types and update responsibility matrix
            foreach(var inspectionType in allInspections)
            {
                var existingRows = respMatrix.Where(r => r.InspectionTypeId == inspectionType.Id).ToList();
                var rows = data.Where(r => r.IssueTypeName == inspectionType.Name).ToList();

                if (rows.Count == 0)
                {
                    // inspection type is not included inthe import, so we are not touching this type at all
                    continue;
                }

                // find rows that are in the database but not in the import
                var rowsToRemove = existingRows.Where(er => !rows.Any(r => r.UserName == allUsers.FirstOrDefault(u => u.Id == er.ResponsibleUserId)?.EMail)).ToList();
                // remove rows
                foreach (var rowToRemove in rowsToRemove)
                    changes += _inspectionsRepository.RemoveResponsibleUser(inspectionType.Id, rowToRemove.ResponsibleUserId);

                // find rows where user is empty meaning that we are removing all users from the inspection type
                var rowToRemoveAll = rows.FirstOrDefault(r => string.IsNullOrWhiteSpace(r.UserName));
                if (rowToRemoveAll != null)
                {
                    changes += _inspectionsRepository.RemoveResponsibleUser(inspectionType.Id, null);
                    continue;
                }
                                
                // save new rows
                foreach(var newRow in rows)
                {
                    var user = allUsers.FirstOrDefault(u => u.EMail == newRow.UserName);
                    if (user == null)
                    {
                        throw new ArgumentException($"Uzivatel \"{newRow.UserName}\" neexistuje");
                    }

                    changes += _inspectionsRepository.SetResponsibleUser(inspectionType.Id, user.Id, newRow.MailAlias, newRow.Days);
                }                
            }

            return $"Hotovo. {changes} záznamů bylo změněno";
        }
    }
}
