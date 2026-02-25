using Elsa.Common.Logging;
using Elsa.Jobs.CrmMailPull.Entities;
using Elsa.Jobs.CrmMailPull.Infrastructure;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Elsa.Jobs.CrmMailPull.Steps
{
    public class LoadMailReferences
    {
        private readonly ILog _log;
        private readonly MailPullRepository _repository;
        private readonly IDatabase _db;

        public LoadMailReferences(ILog log, MailPullRepository repository, IDatabase db)
        {
            _log = log;
            _repository = repository;
            _db = db;
        }

        public void Load()
        {
            _log.Info("Querying last seen ImapUids for all enabled folders... ");
            var lastSeens = _repository.GetLastSeenMailInfo().GroupBy(i => i.SourceId).ToList();

            _log.Info($"Loading folders");
            var allFolders = _repository.GetActiveMailFolders().ToDictionary(f => f.Id, f => f);

            foreach (var sourceFolderLastSeens in lastSeens)
            {
                var sourceId = sourceFolderLastSeens.Key;
                var folderLocalStatuses = sourceFolderLastSeens.ToList();

                if (folderLocalStatuses.Count == 0)
                    continue;

                var source = allFolders[folderLocalStatuses[0].FolderId].MailPullSource;

                _log.Info($"Starting incremental pull of mails from source {source.Host}/{source.Username}");

                Imap.PerformConnected(source, imap =>
                {

                    foreach (var folderLocalStatus in folderLocalStatuses)
                    {
                        var folder = allFolders[folderLocalStatus.FolderId];

                        _log.Info($"Loading mails from folder {folder.FolderFullName}. Local LastSeenUid = {folderLocalStatus.LastSeenImapUid}; UidValidity = {folder.UidValidity}");

                        var currentLastSeen = folderLocalStatus.LastSeenImapUid;

                        const int batchSize = 200;
                        while (true)
                        {
                            try
                            {
                                _log.Info($"Loading a batch of mails where ImapUid > {currentLastSeen}");
                                var batch = imap.GetMailMessageReferences(folder.FolderFullName, currentLastSeen, batchSize);

                                _log.Info($"Received a batch of {batch.Count}");

                                SaveBatch(folder.Id, batch);

                                if (batch.Count < batchSize)
                                {
                                    _log.Info("Done processing the folder");
                                    break;
                                }

                                currentLastSeen = batch.Max(b => b.ImapUid);
                            }
                            catch (Exception ex)
                            {
                                _log.Error("Failed to load a batch - skipping the folder processing", ex);
                                break;
                            }
                        }
                    }

                });
            }

        }

        private void SaveBatch(int folderId, List<RemoteMailMessageReferenceInfo> batch)
        {
            // TODO - bulk save

            _log.Info($"Saving batch of {batch.Count} mails");
            using (var tx = _db.OpenTransaction())
            {
                foreach (var message in batch)
                {
                    var msg = _db.New<IMailMessageReference>(r =>
                    {
                        r.MailboxFolderId = folderId;
                        r.ImapUid = message.ImapUid;
                        r.InternalDt = message.InternalDt;
                    });

                    _db.Save(msg);

                    foreach(var p in message.ParticipantEmails)
                    {
                        var addr = _db.SelectFrom<IMessageParticipantAddress>().Where(a => a.Email == p).Take(1).Execute().FirstOrDefault();
                        if (addr == null)
                        {
                            addr = _db.New<IMessageParticipantAddress>(a => a.Email = p);
                            _db.Save(addr);
                        }

                        var pct = _db.New<IMailMessageReferenceParticipant>(i =>
                        {
                            i.MailMessageReferenceId = msg.Id;
                            i.ParticipantAddressId = addr.Id;
                        });
                        
                        _db.Save(pct);
                    }                    
                }

                tx.Commit();

                _log.Info($"Batch saved");
            }
        }
    }
}
