using Elsa.Common.Logging;
using Elsa.Jobs.CrmMailPull.Infrastructure;
using MimeKit;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Elsa.Jobs.CrmMailPull.Steps
{
    public class LoadMessagesContent
    {
        private readonly ILog _log;
        private readonly MailPullRepository _repository;
        private readonly IDatabase _db;

        public LoadMessagesContent(ILog log, MailPullRepository repository, IDatabase db)
        {
            _log = log;
            _repository = repository;
            _db = db;
        }

        public void Load()
        {
            // collecting messages missing full content
            var messages = _repository.GetMessagesMissingFullContent();

            if (messages.Count == 0)
            {
                _log.Info("No messages to load content");
                return;
            }

            _log.Info("Loading folder index...");

            var folderIndex = _repository.GetActiveMailFolders().ToDictionary(f => f.Id, f => f);

            _log.Info("Grouping messages by source");

            var groupedPerSource = new Dictionary<int, List<MailMessageRef>>();
            foreach (var message in messages)
            {
                if (!folderIndex.TryGetValue(message.FolderId, out var folder))
                    continue;

                if (!groupedPerSource.TryGetValue(folder.MailPullSourceId, out var group))
                {
                    group = new List<MailMessageRef>();
                    groupedPerSource[folder.MailPullSourceId] = group;
                }

                group.Add(message);
            }

            _log.Info($"{groupedPerSource.Count} sources will be used");

            foreach (var sourceId in groupedPerSource.Keys)
            {
                try
                {
                    var source = _repository.GetActiveSource(sourceId);
                    if (source == null)
                    {
                        _log.Info($"Enabled Source not found by Id={sourceId}");
                        continue;
                    }

                    _log.Info($"Connecting {source.Host}/{source.Username} to load full messages");
                    Imap.PerformConnected(source, imap =>
                        {
                            foreach(var message in groupedPerSource[sourceId])
                            {
                                var folder = folderIndex[message.FolderId];

                                MimeMessage fullContent;
                                try
                                {
                                    _log.Info($"Loading full message content by folderFullName={folder.FolderFullName}, ImapUid={(uint)message.ImapUid}");
                                    fullContent = imap.GetFullMessageByUid(folder.FolderFullName, (uint)message.ImapUid);
                                    _log.Info($"Received {fullContent.Subject}");
                                }
                                catch (Exception ex)
                                {
                                    _log.Error($"Message load failed", ex);
                                    continue;
                                }

                                try
                                {
                                    var fullContentId = _repository.SaveMimeMessage(fullContent);
                                    _repository.AssignMessageFullContent(message.MailMessageReferenceId, fullContentId);
                                    _log.Info("Saved");
                                }
                                catch (Exception e)
                                {
                                    _log.Error("Failed to save full message content", e);
                                }
                            }
                        }
                    );

                }
                catch (Exception ex)
                {
                    _log.Error($"Processing the source failed - skipping this source", ex);
                }
            }

        }


    }
}
