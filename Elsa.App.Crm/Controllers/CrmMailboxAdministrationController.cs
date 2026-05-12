using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Jobs.CrmMailPull.Entities;
using Elsa.Jobs.CrmMailPull.Infrastructure;
using Robowire.RoboApi;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Elsa.App.Crm.Controllers
{
    [Controller("CrmMailboxAdministration")]
    public class CrmMailboxAdministrationController : ElsaControllerBase
    {
        private readonly IDatabase _db;

        public CrmMailboxAdministrationController(IWebSession webSession, ILog log, IDatabase db)
            : base(webSession, log)
        {
            _db = db;
        }

        protected override void OnBeforeCall()
        {
            base.OnBeforeCall();
            EnsureUserRight(CrmUserRights.EmailSummarySettingsAdmin);
        }

        public List<MailPullSourceVm> GetSources()
        {
            return LoadSources();
        }

        public SaveSourceResult SaveSource(SaveSourceRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var isNew = !request.Id.HasValue || request.Id.Value < 1;

            IMailPullSource source = isNew
                ? _db.New<IMailPullSource>()
                : _db.SelectFrom<IMailPullSource>()
                    .Where(s => s.Id == request.Id.Value)
                    .Take(1)
                    .Execute()
                    .FirstOrDefault()
                    ?? throw new InvalidOperationException("Zdroj se nepodařilo najít");

            source.Host = (request.Host ?? string.Empty).Trim();
            source.Username = (request.Username ?? string.Empty).Trim();
            source.Port = request.Port;
            source.UseSsl = request.UseSsl;
            source.Password = source.Password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(source.Host))
                throw new InvalidOperationException("Host nesmí být prázdný");

            if (string.IsNullOrWhiteSpace(source.Username))
                throw new InvalidOperationException("Uživatelské jméno nesmí být prázdné");

            if (source.Port < 1 || source.Port > 65535)
                throw new InvalidOperationException("Port musí být v rozsahu 1 až 65535");

            if (!string.IsNullOrWhiteSpace(request.Password))
                source.Password = request.Password;

            var hasPassword = !string.IsNullOrWhiteSpace(source.Password);
            source.IsEnabled = hasPassword && request.IsEnabled;

            _db.Save(source);

            bool connectionTestOk = false;
            string connectionTestMessage;

            if (!hasPassword)
            {
                connectionTestMessage = "Uloženo bez hesla. Připojení nebylo testováno a zdroj zůstal vypnutý.";
            }
            else
            {
                try
                {
                    Imap.TestConnectionAndGetFolders(source);

                    connectionTestOk = true;
                    connectionTestMessage = "Připojení se podařilo.";
                }
                catch (Exception ex)
                {
                    source.IsEnabled = false;
                    _db.Save(source);
                    connectionTestMessage = "Uloženo, ale test připojení selhal: " + ex.Message;
                }
            }

            return new SaveSourceResult
            {
                ConnectionTestOk = connectionTestOk,
                ConnectionTestWasSkipped = !hasPassword,
                ConnectionTestMessage = connectionTestMessage,
                Sources = LoadSources()
            };
        }

        public List<MailPullSourceVm> SetFolderEnabled(int folderId, bool isEnabled)
        {
            var folder = _db.SelectFrom<IMailboxFolder>()
                .Where(f => f.Id == folderId)
                .Take(1)
                .Execute()
                .FirstOrDefault()
                ?? throw new InvalidOperationException("Složku se nepodařilo najít");

            folder.IsEnabled = isEnabled;
            _db.Save(folder);

            return LoadSources();
        }

        private List<MailPullSourceVm> LoadSources()
        {
            return _db.SelectFrom<IMailPullSource>()
                .Join(s => s.Folders)
                .OrderBy(s => s.Host)
                .OrderBy(s => s.Username)
                .Execute()
                .Select(s => new MailPullSourceVm
                {
                    Id = s.Id,
                    Host = s.Host,
                    Port = s.Port,
                    UseSsl = s.UseSsl,
                    Username = s.Username,
                    IsEnabled = s.IsEnabled,
                    HasPassword = !string.IsNullOrWhiteSpace(s.Password),
                    Folders = (s.Folders ?? Enumerable.Empty<IMailboxFolder>())
                        .OrderBy(f => f.FolderFullName)
                        .Select(f => new MailboxFolderVm
                        {
                            Id = f.Id,
                            MailPullSourceId = f.MailPullSourceId,
                            FolderFullName = f.FolderFullName,
                            Name = f.Name,
                            UidValidity = f.UidValidity,
                            IsEnabled = f.IsEnabled
                        })
                        .ToList()
                })
                .ToList();
        }

        public class SaveSourceRequest
        {
            public int? Id { get; set; }
            public string Host { get; set; }
            public int Port { get; set; }
            public bool UseSsl { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public bool IsEnabled { get; set; }
        }

        public class SaveSourceResult
        {
            public bool ConnectionTestOk { get; set; }
            public bool ConnectionTestWasSkipped { get; set; }
            public string ConnectionTestMessage { get; set; }
            public List<MailPullSourceVm> Sources { get; set; }
        }

        public class MailPullSourceVm
        {
            public int Id { get; set; }
            public string Host { get; set; }
            public int Port { get; set; }
            public bool UseSsl { get; set; }
            public string Username { get; set; }
            public bool IsEnabled { get; set; }
            public bool HasPassword { get; set; }
            public List<MailboxFolderVm> Folders { get; set; }
        }

        public class MailboxFolderVm
        {
            public int Id { get; set; }
            public int MailPullSourceId { get; set; }
            public string FolderFullName { get; set; }
            public string Name { get; set; }
            public long UidValidity { get; set; }
            public bool IsEnabled { get; set; }
        }
    }
}
