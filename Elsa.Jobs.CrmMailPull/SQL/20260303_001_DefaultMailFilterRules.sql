INSERT INTO MailPullAddressBlacklist(Pattern) 
 SELECT '*@biorythme.cz'
 WHERE NOT EXISTS(SELECT TOP 1 1 FROM MailPullAddressBlacklist WHERE Pattern = '*@biorythme.cz');

INSERT INTO MailPullAddressBlacklist(Pattern) 
  SELECT u.EMail
    FROM [User] u
   WHERE u.EMail NOT LIKE '%@biorythme.cz'
     AND NOT EXISTS(SELECT TOP 1 1 FROM MailPullAddressBlacklist WHERE Pattern = u.Email);
 
INSERT INTO MailContentBlacklist (SubjectPattern) 
SELECT N'Teď mě tady nezastihnete...'
  WHERE NOT EXISTS(SELECT TOP 1 1 FROM MailContentBlacklist WHERE SubjectPattern = N'Teď mě tady nezastihnete...')