using Elsa.Jobs.CrmMailPull.Entities;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Elsa.Jobs.CrmMailPull.Infrastructure
{
    internal static class Imap
    {
        public static T PerformConnected<T>(IMailPullSource source, Func<IImapClient, T> action)
        {
            using (var client = new ImapClient())
            {
                client.Timeout = (int)TimeSpan.FromSeconds(30).TotalMilliseconds;
                var options = source.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;

                try
                {
                    client.Connect(source.Host, source.Port, options);
                    client.Authenticate(source.Username, source.Password);

                    return action(client);

                }
                finally
                {
                    try
                    {
                        client.Disconnect(false);
                    }
                    catch
                    {
                        ;
                    }
                }
            }
        }

        public static void PerformConnected(IMailPullSource source, Action<IImapClient> action)
        {
            PerformConnected<object>(source, imap => 
            { 
                action(imap); 
                return null; 
            });
        }

        public static List<RemoteFolderInfo> GetAllFolders(this IImapClient imap)
        {
            var result = new List<RemoteFolderInfo>(256);

            void traverse(IMailFolder folder)
            {

                if (result.All(existing => !string.Equals(existing.FullName, folder.FullName, StringComparison.OrdinalIgnoreCase)))
                {
                    var info = new RemoteFolderInfo
                    {
                        Name = folder.Name,
                        FullName = folder.FullName,
                        Attributes = folder.Attributes
                    };

                    // UIDVALIDITY je dostupné až po Open()
                    // Některé složky jsou jen kontejnery (\Noselect) -> nejdou otevřít.
                    if (!folder.Attributes.HasFlag(FolderAttributes.NoSelect))
                    {
                        try
                        {
                            folder.Open(FolderAccess.ReadOnly);
                            info.UidValidity = (long)folder.UidValidity;
                        }
                        catch {; }
                    }

                    result.Add(info);
                }

                // Rekurze do dětí
                IList<IMailFolder> children = null;
                try
                {
                    children = folder.GetSubfolders(false);
                }
                catch {; }

                if (children != null)
                    foreach (var ch in children)
                        traverse(ch);
            }

            var namespaces = imap.PersonalNamespaces?.OfType<FolderNamespace>()?.ToList() ?? new List<FolderNamespace>(0);
            if (namespaces.Count == 0)
                namespaces.Add(new FolderNamespace('.', string.Empty));

            foreach (var ns in namespaces)
            {
                IList<IMailFolder> roots;
                try
                {
                    roots = imap.GetFolders(ns);
                }
                catch
                {
                    continue;
                }

                foreach (var r in roots)
                    traverse(r);
            }

            return result;
        }

        public static List<RemoteMailMessageReferenceInfo> GetMailMessageReferences(
            this IImapClient imap,
            string folderFullName,
            long lastSeenUid,
            int batchSize = 200)
        {
            var folder = imap.GetFolder(folderFullName);
            folder.Open(FolderAccess.ReadOnly);

            var uidValidity = (long)folder.UidValidity;

            // UID range: (lastSeenUid + 1) .. *
            var fromUid = new UniqueId((uint)lastSeenUid + 1);
            var range = new UniqueIdRange(fromUid, UniqueId.MaxValue);

            var uids = folder.Search(SearchQuery.Uids(range));
            if (uids == null || uids.Count == 0)
                return new List<RemoteMailMessageReferenceInfo>(0);

            var results = new List<RemoteMailMessageReferenceInfo>(uids.Count);

            void AddEmails(HashSet<string> set, InternetAddressList list)
            {
                if (set == null || list == null) return;

                foreach (var addr in list)
                {
                    if (addr is MailboxAddress mb)
                    {
                        if (!string.IsNullOrWhiteSpace(mb.Address))
                            set.Add(mb.Address.Trim());
                    }
                    else if (addr is GroupAddress grp && grp.Members != null)
                    {
                        // group: expand members
                        foreach (var member in grp.Members.OfType<MailboxAddress>())
                        {
                            if (!string.IsNullOrWhiteSpace(member.Address))
                                set.Add(member.Address.Trim());
                        }
                    }
                }
            }

            
            var batch = uids.Take(batchSize).ToList();

            var summaries = folder.Fetch(batch, MessageSummaryItems.UniqueId | MessageSummaryItems.InternalDate | MessageSummaryItems.Envelope);

            foreach (var s in summaries)
            {
                var reference = new RemoteMailMessageReferenceInfo
                {
                    FolderFullName = folderFullName,
                    ImapUid = s.UniqueId.Id,
                    InternalDt = s.InternalDate?.UtcDateTime ?? default(DateTime)
                };
                                                        
                AddEmails(reference.ParticipantEmails, s.Envelope?.From);
                AddEmails(reference.ParticipantEmails, s.Envelope?.To);
                AddEmails(reference.ParticipantEmails, s.Envelope?.Cc);
                
                if (reference.ParticipantEmails.Count > 0)
                    results.Add(reference);
            }
            
            return results;
        }
    }

    public sealed class RemoteFolderInfo
    {
        public string FullName { get; set; }
        public string Name { get; set; }
        public long UidValidity { get; set; }
        public FolderAttributes Attributes { get; set; }
    }

    public sealed class RemoteMailMessageReferenceInfo
    {
        public string FolderFullName { get; set; }
        public uint ImapUid { get; set; }
        public DateTime InternalDt { get; set; }

        // Raw emails; you can immediately HMAC them before persisting if you want.
        public HashSet<string> ParticipantEmails { get; set; } = new HashSet<string>();
    }
}
