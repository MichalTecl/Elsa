using Elsa.Common;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Jobs.CrmMailPull.Entities;
using Elsa.Jobs.CrmMailPull.Infrastructure;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Elsa.Jobs.CrmMailPull.Steps
{
    public class ExploreFolders : IStep
    {
        private static readonly FolderAttributes[] _skippedAttributes = new FolderAttributes[] { FolderAttributes.Junk, FolderAttributes.Drafts, FolderAttributes.Trash };

        private readonly ILog _log;
        private readonly MailPullRepository _repository;
        private readonly IDatabase _db;

        public ExploreFolders(ILog log, MailPullRepository repository, IDatabase db)
        {
            _log = log;
            _repository = repository;
            _db = db;
        }

        public void Run(TimeoutCheck timeout)
        {
            foreach(var source in _repository.GetActiveSources())
            {
                timeout.Check();

                SyncFolders(source, timeout);
            }

            _log.Info("Folders sync completed");
        }

        private void SyncFolders(IMailPullSource source, TimeoutCheck timeout)
        {           
            _log.Info($"Starting folders sync. Source={source.Host}/{source.Username}");
            
            try
            {
                var serverFolders = Imap.PerformConnected(source, client => client.GetAllFolders());

                var localFolders = source.Folders?.ToDictionary(f => f.FolderFullName.ToLowerInvariant(), f => f);

                _log.Info($"Received {serverFolders.Count} folder(s) from IMAP server; locally there is {localFolders.Count} folder(s)");

                foreach(var serverFolder in serverFolders)
                {
                    timeout.Check();

                    _log.Info($"Syncing server folder = {serverFolder.FullName}");

                    foreach(var skipFlag in _skippedAttributes)
                        if (serverFolder.Attributes.HasFlag(skipFlag))
                        {
                            _log.Info($"Skipping folder {serverFolder.FullName} because of '{skipFlag}' attribute");
                            continue;
                        }

                    bool dirty = false;

                    if(!localFolders.TryGetValue(serverFolder.FullName.ToLowerInvariant(), out var localFolder))
                    {
                        _log.Info("Local folder does not exist yet");
                        dirty = true;
                        localFolder = _db.New<IMailboxFolder>();
                        localFolder.FolderFullName = serverFolder.FullName;
                        localFolder.UidValidity = serverFolder.UidValidity;
                        localFolder.IsEnabled = true;
                        localFolder.MailPullSourceId = source.Id;
                    }
                    else
                    {
                        localFolders.Remove(serverFolder.FullName.ToLowerInvariant());
                    }

                    if (localFolder.UidValidity != serverFolder.UidValidity)
                    {
                        _log.Error($"The folder {source.Host}/{source.Username}/{serverFolder.FullName} has changed its UIDVALIDITY. It needs to be fully resynced.");
                        dirty = dirty || localFolder.IsEnabled;

                        localFolder.IsEnabled = false;
                    }

                    if (localFolder.Name != serverFolder.Name)
                    {
                        dirty = true;

                        _log.Info($"Folder Name changed from '{localFolder.Name}' to '{serverFolder.Name}'");
                        localFolder.Name = serverFolder.Name;
                    }

                    if (dirty) 
                    {
                        _log.Info($"Saving folder {source.Host}/{source.Username}/{serverFolder.FullName}");
                        _db.Save(localFolder);
                    }
                    else
                    {
                        _log.Info("No changes detected");
                    }
                }            
                
            }
            catch (Exception ex)
            {
                _log.Error($"Cannot sync folders. Source={source.Host}/{source.Username}", ex);
            }
        }
    }
}
