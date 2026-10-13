# Zpomalení Elsy 13. 10. 2026

## Výsledek dosavadního šetření

Příčina není z aplikačního logu prokázaná. Log prokazuje opakované timeouty
vykonávání SQL příkazů, nikoli vyčerpání connection poolu. Nejprve je potřeba
zachytit čekání a blokující sessions na SQL Serveru během problému. IIS a SQL
Server podle potvrzení provozovatele běží na stejném stroji, takže je nutné
současně ověřit paměť, CPU a disk celého stroje.

## Konkrétní důkazy v dodaném logu

Zdroj: `C:\Users\mtecl\Downloads\Pomala elsa\20261013.log`.

- Celkem 31 chybových záznamů `Execution Timeout Expired`, všechny před restartem
  ve 12:30:48. První je v 09:39:56 při `PackingController.PackOrder(208870)`.
- Od 11:55 se problém opakuje. Selhávají čtení objednávek, historie běhů úloh,
  procedura `GetThresholdsState` pro varování skladů a report šarží.
- Stacky končí ve vykonávání `ExecuteReader` / načítání metadat výsledku,
  `Error Number:-2`. Nejde o timeout v `SqlConnection.Open` s hláškou
  o nedostupném připojení z poolu.
- Ve 12:28:23 a 12:29:06 trvá report šarží přibližně 30 sekund a selže.
  Po restartu např. ve 12:36:21 trvá 171 ms, ve 12:37:57 62 ms.
  Jde o různá volání, nikoli srovnávací měření stejného vstupu.
- Jediný zaznamenaný deadlock je ve 12:42:00, tedy po restartu:
  `f587ce5f3db64954ad2ce7f208af1d02`, `GetMissingPaymentsCount`, oběť SPID 57.
- Log obsahuje také start aplikace v 09:23:40. Důvod těchto startů log neuvádí.
- Některá volání jsou rychlá i během incidentu. Řada z nich využívá cache;
  rychlá odpověď proto nedokazuje, že SQL Server pracuje bez problémů.

## Audit DeadlockDiagnostics

Nasazeno commitem `bb67ed2` z 29. 9. 2026. Prověřena byla také cesta přes
`Database`, `HierarchicSqlDataReader`, `ConnectionCreator` a transakční manager.
Existující necommitnuté změny diagnostiky byly ponechány beze změny.

- `Capture` propouští pouze SQL chybu 1205. Timeout -2 do fronty nevstupuje.
- `Request` drží DTO, sink a `Func<SqlConnection>`. Nedrží `SqlCommand`,
  `SqlDataReader`, výjimku ani přímo původní otevřené připojení.
- Delegát je instanční metoda manageru, takže jej nepřímo drží v paměti.
  Manager obsahuje `ThreadLocal` transakce. To je vazba na další objekty,
  ale samotné držení reference nezabraňuje provedení jejich `Dispose`.
  Nebyla nalezena cesta, kterou by fronta odkládala úklid původní transakce.
- `ConnectionCreator.OpenUnmanagedConnection` přes název vrací NOVÉ ZAVŘENÉ
  připojení. Sběrač jej otevírá až ve `ReadGraphs`.
- Spojení, příkazy i readery sběrače jsou v `using`; spojení se uklidí i při
  výjimce a před prodlevou dalšího pokusu. Jeden pracovník může mít současně
  nejvýše jedno diagnostické spojení. Fronta má limit 32 a `TryAdd` nečeká.
- Přenos ExecutionContext při startu pracovníka je potlačen, TransactionScope
  je potlačen a diagnostické připojení používá `Enlist=false`.
- Dispose připojení při zapnutém poolingu běžně ponechá fyzickou SQL session
  v poolu. Samotné `sleeping` spojení bez transakce není důkaz úniku.
- Rizikem diagnostiky je dotaz přes `system_health*.xel`, který pro každý
  incident čte historii XE souborů a až třikrát ji zkouší prohledat. Timeout
  omezuje příkaz, nezaručuje malou spotřebu prostředků. Při četných deadlocích
  může přidávat zátěž SQL. Dnešní log ale před restartem žádný deadlock
  nezaznamenal, takže neposkytuje důkaz spuštění této zátěže v době incidentu.

Závěr: hypotéza fronty hromadící otevřená SQL spojení se v prověřené
implementaci nepotvrdila. Záznamy dnešního incidentu tuto hypotézu dále oslabují.
Nelze tím vyloučit incidenty z jiného procesu nebo nezaznamenané chyby.

## Ostatní prověřené změny a kandidáti

Prohlédnuta historie od září 2026, zejména změny od 11. 9. a změny commitu
s diagnostikou. Nebyla nalezena prokazatelná nová cesta úniku spojení.

- Nové sledování zásilek běží v `ProcessStepBase.ExecuteOrderTransaction`.
  `TrackShipments` uvnitř transakce čte DB a následně volá HTTP dopravce;
  Packeta má timeout 60 sekund. Transakce a připojení tak mohou zůstat živé
  během síťového čekání. Čtení v ReadCommitted samo o sobě neznamená držení
  všech čtecích zámků po celou transakci. Dopad vyžaduje výpis čekání a log
  JobLauncheru; dodaný webový log jej neprokazuje.
- Sledování zásilek nově prochází i starší odeslané objednávky s aktivním
  sledováním (`HistoryDepthDays = null`), což může přidávat práci jobům a SQL.
- Nový bulletin přidává rozsáhlejší analytická čtení. Jeho souběh s incidentem
  z dodaného webového logu neznáme.
- Změna `vwOrderDiscount` z 29. 9. přidává PriceMultiplier a NULLIF,
  agregační JOINy jsou přítomny už v předchozí verzi z roku 2023.
  Samotný diff tedy neprokazuje nově zavedené velké agregace.
- `OrdersFacade.SetOrderSentAsync` spouští `Task.Run` uvnitř TransactionScope.
  Je to rizikové místo pro přenos ambientní transakce, ale podle historie
  nebylo v prověřeném období změněno. Nelze jej označit za novou regresi.

## Zachycení při dalším zpomalení

1. V SSMS připojeném k dané SQL instanci otevřít `CaptureElsaSlowdown.sql`.
   Nastavit výsledky do souboru (Ctrl+Shift+F) a spustit CELÝ skript ještě
   před restartem IIS. Skript pořídí tři snímky za přibližně deset sekund,
   nezapisuje aplikační data ani nemění serverovou konfiguraci.
   Vyžaduje VIEW SERVER STATE, na SQL Serveru 2022+ VIEW SERVER PERFORMANCE STATE.
2. Uchovat všechny výsledkové sady. V SSMS zkontrolovat, že se dlouhé SQL texty
   netruncují nastaveným limitem délky sloupce. Pokud je nutná rychlá obnova,
   po dokončení sběru provést obvyklý restart.
3. Současně zaznamenat ve Správci úloh / Monitoru prostředků CPU, paměť
   a disk; zvlášť procesy `w3wp`, `sqlservr` a JobLauncher. Uchovat PID.
4. Uchovat webový log a log JobLauncheru pro stejný interval. Volitelně
   provést stejný SQL výpis i při normálním provozu jako základ srovnání.

Interpretace:

- `LCK_M_*` a kladné `blocking_session_id`: dohledat uvedenou session v sadě A
  a její transakci v sadě B. Zvlášť podezřelá je dlouho neaktivní session
  s otevřenou transakcí, která blokuje ostatní. PID určí proces vlastníka.
- Rostoucí CPU a logical_reads bez blokování: prověřit konkrétní SQL a plán.
- `RESOURCE_SEMAPHORE`: čekání na paměť pro dotaz. `THREADPOOL`: nedostatek SQL
  workerů. Trvale vysoké runnable_tasks_count ukazuje tlak na SQL CPU.
- `PAGEIOLATCH_*` / `WRITELOG`: prověřit diskovou latenci a aktivní dotazy.
- SQL bez související zátěže a čekání, ale zatížený `w3wp`: další krok je dump
  IIS procesu během incidentu a analýza vláken / GC. Po restartu už stav chybí.

Restart IIS sám nerozlišuje příčiny: ukončí klientské sessions i případné
transakce, vyprázdní procesní cache a uvolní prostředky webového procesu.
Nezvyšovat SQL timeouty jako náhradu odstranění příčiny.

## Ověření a zdroje

Aplikační kód nebyl změněn. Build nebyl spuštěn. Diagnostický SQL nebyl
vykonán na skutečném serveru; byla provedena statická kontrola a kontrola
použitých DMV proti dokumentaci. Nové soubory mají CRLF.

- [Microsoft: Query timeouts](https://learn.microsoft.com/troubleshoot/sql/database-engine/performance/troubleshoot-query-timeouts)
- [Microsoft: SQL blocking](https://learn.microsoft.com/en-us/troubleshoot/sql/database-engine/performance/understand-resolve-blocking)
- [Microsoft: Connection pooling](https://learn.microsoft.com/en-us/sql/connect/ado-net/sql-server-connection-pooling)
- [Microsoft: sys.dm_exec_connections](https://learn.microsoft.com/en-us/sql/relational-databases/system-dynamic-management-objects/sys-dm-exec-connections-transact-sql)
