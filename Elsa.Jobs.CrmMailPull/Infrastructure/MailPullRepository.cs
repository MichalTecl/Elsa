using Elsa.Common.Caching;
using Elsa.Jobs.CrmMailPull.Entities;
using MimeKit;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Elsa.Jobs.CrmMailPull.Infrastructure
{
    public class MailPullRepository
    {
        private readonly ICache _cache;
        private readonly IDatabase _db;

        public MailPullRepository(ICache cache, IDatabase database)
        {
            _cache = cache;
            _db = database;
        }

        public List<IMailboxFolder> GetActiveMailFolders()
        {
            return _db.SelectFrom<IMailboxFolder>()
                .Join(f => f.MailPullSource)
                .Where(f => f.IsEnabled && f.MailPullSource.IsEnabled)
                .Execute()
                .ToList();
        }

        public List<IMailPullSource> GetActiveSources()
        {
            return _db.SelectFrom<IMailPullSource>()
                .Join(s => s.Folders)
                .Where(s => s.IsEnabled)
                .Execute()
                .ToList();
        }

        public IMailPullSource GetActiveSource(int sourceId)
        {
            return _db.SelectFrom<IMailPullSource>()
                .Join(s => s.Folders)
                .Where(s => s.IsEnabled)
                .Where(s => s.Id == sourceId)
                .Take(1)
                .Execute()
                .FirstOrDefault();
        }

        public int SaveMimeMessage(MimeMessageWrapper wrap)
        {
            var entity = _db.SelectFrom<IMailMessageFullContent>()
                .Where(m => m.MessageUid == wrap.MessageUid)
                .Take(1)
                .Execute()
                .FirstOrDefault();

            if (entity == null)
            {
                entity = _db.New<IMailMessageFullContent>();

                entity.MessageUid = wrap.MessageUid;
                entity.Subject = wrap.Subject;
                entity.ConversationUid = wrap.ConversationUid;
                entity.Content = wrap.BodyPlainText;
                entity.Sender = wrap.Message.Sender.GetAddress(false);

                _db.Save(entity);
            }

            return entity.Id;
        }

        public List<LastSeenMailInfo> GetLastSeenMailInfo()
        {
            return _db
                .Sql()
                .Execute(@"select f.MailPullSourceId SourceId, 
                           f.Id FolderId,
                           ISNULL((select MAX(r.ImapUid) from MailMessageReference r where r.MailboxFolderId = f.Id), 0) LastSeenImapUid
                      from MailboxFolder f
                      join MailPullSource src ON (f.MailPullSourceId = src.Id)
                     where src.IsEnabled = 1
                       and f.IsEnabled = 1
                    order by f.MailPullSourceId")
                .AutoMap<LastSeenMailInfo>();
        }

        public List<MailMessageRef> GetMessagesMissingFullContent()
        {
            return _db.Sql()
                .Execute(@"SELECT mr.Id MailMessageReferenceId, mr.MailboxFolderId FolderId, mr.ImapUid
                          FROM MailMessageReference mr  
                          WHERE mr.ConversationId IS NULL    
                            AND mr.FullContentId IS NULL
                            AND mr.Id IN
                            (
                               SELECT mrpa.MailMessageReferenceId
                                 FROM MailMessageReferenceParticipant mrpa 
                                 JOIN MessageParticipantAddress mpa ON (mrpa.ParticipantAddressId = mpa.Id)
                                 JOIN vwAllDistributorEmails ade ON (mpa.Email = ade.Email)
                            )")
                .AutoMap<MailMessageRef>();
        }

        internal void AssignMessageFullContent(int mailMessageReferenceId, int fullContentId)
        {
            var mref = _db.SelectFrom<IMailMessageReference>()
                .Where(m => m.Id == mailMessageReferenceId)
                .Take(1)
                .Execute()
                .FirstOrDefault() ?? throw new ArgumentException($"Invalid {nameof(mailMessageReferenceId)}");

            mref.FullContentId = fullContentId;

            _db.Save(mref);
        }

        public MessageFilter GetFilter()
        {
            return _cache.ReadThrough("mailMessageFilter", TimeSpan.FromHours(1), () => {

                var adrBlsts = _db.SelectFrom<IMailPullAddressBlacklist>()
                .Execute()
                .Where(bl => !string.IsNullOrWhiteSpace(bl.Pattern))
                .ToList();

                var contentBlsts = _db.SelectFrom<IMailContentBlacklist>()
                .Execute()
                .Where(bl => !(string.IsNullOrWhiteSpace(bl.SubjectPattern) && string.IsNullOrWhiteSpace(bl.BodyPattern)))
                .ToList(); 

                return new MessageFilter(adrBlsts, contentBlsts); 
            });
        }
    }

    public sealed class LastSeenMailInfo
    {
        public int SourceId { get; set; }
        public int FolderId { get; set; }
        public long LastSeenImapUid { get; set; }
    }

    public sealed class MailMessageRef
    {
        public int MailMessageReferenceId { get; set; }
        public int FolderId { get; set; }
        public long ImapUid { get; set; }
    }
}
