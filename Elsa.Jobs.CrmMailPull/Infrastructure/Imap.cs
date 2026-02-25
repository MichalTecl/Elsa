using Elsa.Jobs.CrmMailPull.Entities;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
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
    }

    public sealed class RemoteFolderInfo
    {
        public string FullName { get; set; }
        public string Name { get; set; }
        public long UidValidity { get; set; }
        public FolderAttributes Attributes { get; set; }
    }
}
