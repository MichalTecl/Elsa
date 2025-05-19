insert into MeetingStatus (Title, ColorHex, IconClass, ProjectId, ActionExpected)
select *
  from (
	select N'Plán' Title, N'#1E90FF' ColorHex, 'far fa-calendar-alt' IconClass, 1 ProjectId, 1 ActionExpected union
	select N'Proběhlo',   N'#32CD32', 'far fa-calendar-check', 1, 0 union 
	select N'Zrušeno',    N'#FF6347', 'far fa-calendar-times', 1, 0
  ) x
  where not exists (select top 1 1 from MeetingStatus ems where ems.Title = x.Title);

DECLARE @defaultStatusId INT;
SELECT TOP 1 @defaultStatusId = Id FROM MeetingStatus WHERE Title = N'Plán';

insert into MeetingCategory (Title,  IconClass, InitialStatusId, ProjectId, ExpectedDurationMinutes)
select *
  from (
	select N'Telefonát' Title, 'fas fa-phone' IconClass, @defaultStatusId InitialStatusId, 1  ProjectId, 15 ExpectedDurationMinutes union
	select N'Schůzka' Title,  'fas fa-coffee' IconClass, @defaultStatusId InitialStatusId, 1  ProjectId, 60 ExpectedDurationMinutes union
    select N'E-Mail' Title,  'fas fa-envelope' IconClass, @defaultStatusId InitialStatusId, 1  ProjectId, 1 ExpectedDurationMinutes
  ) x
  where not exists (select top 1 1 from MeetingCategory ems where ems.Title = x.Title);


insert into MeetingStatusAction (Title, CurrentStatusTypeId, NextStatusTypeId, SortOrder)
 select trg.Title, src.Id, trg.Id, trg.Id
   from MeetingStatus src
   cross join MeetingStatus trg 
  where ((src.Title = N'Plán' AND trg.Title = N'Proběhlo')
     or (src.Title = N'Plán' AND trg.Title = N'Zrušeno'))
	 and not exists(select top 1 1 from MeetingStatusAction msa where msa.CurrentStatusTypeId = src.Id and msa.NextStatusTypeId = trg.Id)
	 