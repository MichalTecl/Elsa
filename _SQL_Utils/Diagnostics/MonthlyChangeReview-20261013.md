# Review výkonových regresí Elsy, 13. 10. 2026

Závěr: v prověřeném měsíčním diffu není doložen nový únik SQL spojení, který by vysvětlil incident. Incident je skutečné zpomalení vykonávání SQL, ne pouze pomalé UI. Nejsilnější pracovní hypotézy jsou blokování vyvolané webovým procesem a nedostatek prostředků, případně zesílené souběžným obnovováním cache. Konkrétní viník vyžaduje snímek SQL čekání a webového procesu PŘED restartem. Připsat incident konkrétnímu commitu jen podle data by nyní bylo spekulací.

## Rozsah a omezení

- Hlavní rozsah: 13. 9.–13. 10. 2026; diff `6f0a3cd^..e5dd418`, osm commitů, 112 změněných souborů včetně kopií v Portal, 4 595 přidaných a 162 odebraných řádků.
- Posouzeny běhové změny C#, SQL, UI, registrace služeb, závislosti a návazné původní implementace ORM, transakcí, DI, cache a spouštění jobů. U objemných nových rendererů a analytiky zaměření na výkon a životnost prostředků, nikoli úplná účetní správnost výpočtů.
- Navíc cíleně ověřeny změny z 8.–11. 9. navazující na import objednávek, nové počítadlo selhání a ERP volání. Nejde o tvrzení, že byl kompletně auditován celý tento starší interval.
- HEAD není důkaz nasazené verze. Lokálně byly už před review necommitnuté změny: zejména vypnutí ObtainShipmentInfo/TrackShipments, rozšíření diagnostiky a oprava převodu DB_ID. Nebyly změněny.
- Přečten existující rozbor `ElsaSlowdown-20261013.md`; rozhodující údaje z původního logu byly nezávisle přepočteny. Existující SQL diagnostika nebyla přepsána.
- Build nebyl spuštěn. Žádné připojení na produkční DB, reset, změna konfigurace ani nasazení. Aplikační zdrojové soubory nebyly upraveny.

## Důkazy incidentu

Zdroj: `C:\Users\mtecl\Downloads\Pomala elsa\20261013.log` (550 382 bajtů), dodané XML ve vedlejším adresáři Deadlocks. V tomto review žádný živý produkční přístup.

| Zjištění | Význam |
|---|---|
| 31 záznamů Execution Timeout Expired, SQL chyba -2; stacky v ExecuteReader/čtení výsledku | Connection.Open není místo těchto selhání; timeout přidělení spojení z poolu log nedokládá. |
| GetInventories 11×, GetStatus jobů 7×, FindOrder 6×, PackOrder 3×, report šarží 3×, GetLevels 1× | Nejde pouze o dopravce, jedinou stránku nebo jednu tabulku objednávek. |
| 7 různých ClientConnectionId v těchto 31 chybách; některé se opakují 8–9× | Selhání se týkají více spojení. Počet všech otevřených spojení z toho určit nelze. |
| Start aplikace 09:23:40; první timeout 09:39:56; souvislá série od 11:55; další start 12:30:48 | První timeout přišel už přibližně 16 minut po startu. Model „pouze leak po mnoha hodinách“ není jediná možnost. Příčinu startů log neurčuje. |
| Po startu ve 12:30:48 v dodaném úseku už žádný execution timeout; report šarží např. 171 ms a 62 ms | Potvrzuje praktický efekt restartu, ale nejde o srovnání totožných parametrů. Log končí krátce po restartu, ne po celém dalším dni. |
| Jediný zalogovaný deadlock dne 13. 10. je ve 12:42:00 | Ve webovém logu není podklad pro deadlockovou diagnostiku jako původce předchozího zpomalení. |
| 49 startů PerformErpActionSafe s akcí SetOrderSent má odpovídající dokončení | V tomto logu se nehromadí nedokončené takto označené ERP operace. Jeden běh trvá 39 s. Dokončovací log sám nepotvrzuje stav všech transakcí v SQL. |
| V logu nejsou zaznamenaná spuštění DeletePackage ani ruční Launch jobu | Nový mazací tok ani ruční bulletin nejsou logem podpořené spouštěče. Absence záznamu není absolutní vyloučení. |

SQL timeout není náhodná kosmetická chyba. Znamená, že klient v limitu nedokončil příkaz. Nerozlišuje sám blokování, pomalý plán, I/O ani nedostatek prostředků. Viz [Microsoft: Query timeouts](https://learn.microsoft.com/troubleshoot/sql/database-engine/performance/troubleshoot-query-timeouts).

## Konkrétní nedostatky zavedené v měsíčním rozsahu

### 1. P1: výpadek seznamu Packety se zkouší znovu pro každou objednávku

`Elsa.Integration.ShipmentProviders.Zasilkovna/ZasilkovnaShipmentProvider.cs:33–37`, zavedení d061425; pokračování po chybě v `Elsa.Jobs.OrdersPostprocessing/Steps/ProcessStepBase.cs:146–149`.

Cache i čas posledního načtení se nastaví až po úspěchu LoadShipments. Když selže login, timeout nebo parsování kterékoliv stránky, následující objednávka znovu vyvolá kompletní načítání. Pro N objednávek může vzniknout N stejných pokusů v jednom běhu, každý až s tisícem stránek a timeoutem 60 s na HTTP požadavek. Limit 1 000 objednávek je velikost stránky, nikoli limit celého běhu. Při 100 objednávkách a jednom minutovém timeoutu na pokus jde o přibližně 100 minut čekání, ne o jeden neúspěšný pokus.

Celý krok navíc vstupuje do transakce ještě před HTTP. TrackShipments nejprve skutečně načte zásilky z DB, a proto drží spojení i během následného HTTP. U ObtainShipmentInfo může být SQL spojení otevřené už získáním mapování dopravce; záleží na cache. Transakční režim je ReadCommitted: nelze tvrdit, že všechny čtecí zámky nutně přežívají až do konce transakce.

Náprava: načíst snapshot před smyčkou a mimo DB transakci; při chybě přerušit příslušný krok nebo zavést interval do dalšího pokusu; krátká transakce až pro uložení ověřeného výsledku. Neoznačovat chybu načtení jako prázdný seznam zásilek.

Vztah k incidentu: reálná nová provozní chyba, ale plánovaný běh je v Elsa.JobLauncher, který samotný IISRESET neukončí. Vyšší podezření pouze při ručním spuštění jobu z webu nebo doloženém sdíleném přetížení SQL. Lokálně už jsou tyto dva kroky vypnuté; stav produkce neověřen.

### 2. P2: nefunkční zásilky neprojdou třicetidenním vyřazením

`Elsa.Jobs.OrdersPostprocessing/Steps/TrackShipments.cs:69–91`, a0389a2; DPD přeskočení v aktuálním HEAD.

HTTP volání a jeho validace předcházejí výpočtu Unwatched. Výjimka z API opustí metodu před vyřazením, stejně jako DPD return false. Při opakované chybě zůstává zásilka aktivní i po 30 dnech. QueryOrders nemá časovou hranici, takže staré chyby přidávají práci při každém dalším běhu. Jediná chybující zásilka blokuje dokončení zpracování celé objednávky.

Náprava: trvalé vyřazení podle věku provést před voláním API a uložit i při chybě jiné zásilky; chyby izolovat po zásilkách, zavést další termín pokusu. V rámci lokálního dočasného vypnutí jde o latentní chybu před opětovným zapnutím.

Vztah k incidentu: vysvětluje rostoucí zátěž jobu v čase, ale samostatně špatně vysvětluje okamžitou a dlouhodobější úlevu po restartu pouze IIS.

### 3. P2: nové pollingy nemají omezení souběhu a ústup při chybě

`Elsa.Apps.Reporting/UI/Widgets/Reporting/BulletinWidget.js:17,32–45`, 2772d4a; `Elsa.Portal/UI/Pages/Packing/OrdersPacking.html:415–423`, 6f0a3cd.

Bulletin může poslat požadavek současně při focus, visibilitychange a timeru. requestId pouze zahodí starší odpověď; již odeslaný požadavek nezruší. Počítadlo selhání importu běží každých 30 s i ve skryté záložce. Oba toky pokračují, i když předchozí volání stále čeká. Společný Lt.Call.js nemá nastavený XMLHttpRequest.timeout ani rušení předchozího volání. Při SQL či session čekání tak nové volání může přibýt do již existující fronty. Při zdravém serveru jde o malou zátěž; netvrdím, že několik těchto dotazů samo přetíží server.

Ověření bez buildu: skutečný BulletinWidget.js spuštěn v Node vm se zachycenými HTTP callbacky; bez jediné odpovědi vznikly 4 požadavky (init, focus, visibilitychange, timer). Skutečný inline skript z balení vytvořil 3 čekající požadavky při hidden=true. Zároveň ověřeno, že opětovné načtení BulletinWidget.js díky singleton guardu nepřidá další interval. Tedy NEJDE o prokázaný leak intervalů bulletinu.

Náprava: nejvýše jedno probíhající volání, další plánovat po dokončení; zpožďovat opakování při chybách a zastavit polling neviditelné stránky. U Bulletinu je vhodné silent volání, aby kontrola odznaku neovládala globální indikátor uživatelské operace.

Vztah k incidentu: pravděpodobnější zesilovač existujícího SQL problému než jeho samostatný původce. Samotný IISRESET intervaly v otevřených prohlížečích nemaže.

### 4. Diagnostika může být nákladná, ale důkazy zde míří proti ní

`Robowire/Robowire.RobOrm.SqlServer/DeadlockDiagnostics.cs`, bb67ed2.

Při skutečném deadlocku se až třikrát prohledává system_health*.xel bez sdílení výsledku mezi incidenty. Při trvalé sérii deadlocků může jeden pracovník průběžně zatěžovat SQL. Fronta 32 omezuje počet čekajících úloh, nikoli celkový počet scanů za den. Vhodné je časové omezení četnosti a sdílení jednoho načtení grafů pro více událostí.

Proti hypotéze úniku spojení: Capture ignoruje chyby jiné než 1205, TryAdd nečeká, pracovník je jeden; nové diagnostické spojení, command a reader jsou v using; ExecutionContext a TransactionScope jsou potlačené a Enlist=false. Ve frontě je metoda manageru, nikoli původní SqlCommand/reader. Držení manageru referencí samo neodkládá Dispose původní transakce. ORM Select materializuje výsledky a má reader i transakci v using. Nová změna navíc přidala Dispose dříve neuklízeného commandu.

Dodané tři XML jsou z 5., 6. a 8. 10., všechna CollectionFailed / Specified cast is not valid. Nejsou to grafy dnešního incidentu a neprokazují, že prohledávání XEL vůbec proběhlo. HEAD používá reader.GetInt32(0) pro DB_ID(); lokální změna už obsahuje explicitní CAST a Convert.ToInt32. Oprava diagnostiky je rozumná, ale není důkaz opravy zpomalování.

## Starší mechanismy relevantní pro dnešní incident

### Souběžné obnovování cache násobí SQL práci

`Elsa.Common/Caching/Cache.cs:18–23`, poslední změna souboru 2024; `Elsa.Apps.ScheduledJobs/ScheduledJobsController.cs:45–56,92–115`, poslední změna červenec 2026.

ConcurrentDictionary.GetOrAdd nezaručuje jedno spuštění factory pro daný klíč. Při souběžném cache miss může každý požadavek provádět plnou SQL práci. Po chybě se žádná položka neuloží a nové požadavky zkoušejí vše znovu. Přesně tato cache cesta je ve stacích 7 timeoutů GetStatus a 11 GetInventories. GetStatus načítá historii zvlášť pro každý job (max. 100 běhů na job); není to neomezené načítání celé historie, ale nově registrovaný bulletin přidává další dotaz do každé obnovy.

Je zde i druhý problém: expirace se počítá před factory a při již expirovaném výsledku se provádí rekurzivní opakování. Jestli úspěšná factory vždy trvá déle než TTL, vzniká opakované počítání bez výsledku. To je podmíněně prokazatelná chyba, nikoli důkaz dnešního incidentu: TTL GetInventories je 5 minut a GetStatus 1 hodina, zatímco zaznamenané příkazy timeoutují přibližně za 30 s.

Doporučená oprava: koordinace jedné obnovy pro klíč, platnost počítaná až od úspěšného dokončení, krátký odstup po chybě; navrhnout správnou invalidaci a životnost zámků. Výsledek u různých uživatelů nesdílet, pokud závisí na oprávněních. Samotné přidání globálního locku by zavedlo jiné blokování.

Dokumentace: [Microsoft: ConcurrentDictionary.GetOrAdd](https://learn.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentdictionary-2.getoradd) výslovně připouští opakované spuštění factory při souběhu.

### Task.Run uvnitř transakce a sdílení requestových služeb

`Elsa.Commerce.Core/Impl/OrdersFacade.cs:207–242` a `Robowire/Robowire.RobOrm.SqlServer/Transaction/SqlTransaction.cs:83`.

SetOrderSentAsync spouští Task.Run ještě uvnitř otevřené transakce; TransactionScope má AsyncFlowOption.Enabled. Background task používá stejné služby a objekty objednávky z requestu; manager přitom vede vlastní transakce v ThreadLocal. Je to riziková kombinace životností a ambientní transakce, kterou doporučuji odstranit odděleným scope po potvrzení původní transakce. Není to změna posledního měsíce a v logu se všechny nalezené akce dokončí. Není korektní toto označit za prokázaný leak nebo nový viník.

Obdobně ScheduledJobsController.Launch spouští job přes Task.Run v IIS. Proto platí „joby běží mimo IIS“ pouze pro plánovač, nikoli ruční spuštění. Nový bulletin může ručně běžet i ve webovém procesu; dnešní log takové spuštění nedokládá.

## Ostatní změny a důvody nižšího podezření

| Commit / oblast | Posouzení |
|---|---|
| 6f0a3cd (13. 9.) balení | Nové count polling volání; viz nález 3. COUNT agregace, nikoli načítání celého seznamu chyb do paměti. |
| d63b9c1, 2772d4a (14.–15. 9.) bulletin a DocGen | Analytika čte až 24 měsíců objednávek a celou historii prvního nákupu; významná jednorázová zátěž. Data se pro nový generatedAt resetují, nepřidávají se donekonečna. Plánovač: sobota 02–03 h; dnešní incident je v úterý přes den. Ruční běh ověřit v Jobs logu. Web pouze čte hotový HTML soubor, negeneruje analytiku při každém zobrazení. |
| d061425, a0389a2 (15. 9.) dopravci | Viz nálezy 1–2. HTTP odpovědi jsou v using; tracking HttpClient je statický, webový klient je disposable; nenalezena nová neomezená statická kolekce zásilek. Facade v balení deleguje původní legacy operaci, nespouští nový kompletní scraping. |
| bb67ed2 (29. 9.) ORM a log | Viz audit diagnostiky. Běžný úspěšný SQL příkaz nepřidává diagnostickou práci do fronty. ThreadLocal/TransactionScope implementace se nezměnila. |
| bb67ed2, vwOrderDiscount | Agregační joiny již byly ve verzi z roku 2023. Nově PriceMultiplier a NULLIF; nelze je vydávat za nově zavedený full scan. Reálný plán je třeba měřit. |
| bb67ed2, index PurchaseDate | Nový index může změnit výběr plánu a cenu zápisu. Nemám plán před/po ani statistiky. Je to kandidát pro Query Store, nikoli podklad pro slepé odstranění indexu. IISRESET neznamená vyprázdnění SQL plan cache. |
| bb67ed2, xrep_ordersByDaysAndHours | Agreguje 10 let a ignoruje @projectId. Zbytečně široký dotaz a samostatná chyba izolace reportu mezi projekty. Volá se při vyžádání tohoto reportu, ne automaticky v každém requestu; dnešní log ho jako zdroj nezachycuje. |
| bb67ed2, AutoSuggest | Opravuje dispatch change/input; nepřidává nekonečný timer nebo SQL background smyčku. |
| 6be3331 (5. 10.) mazání účetních dat | Nové explicitní volání existující hromadné mazací procedury. Může krátkodobě blokovat; wrapper má using transakci. Podmínkou podezření je skutečné použití. Dnešní webový log neukazuje jeho volání. |
| e5dd418 (7. 10.) šarže, DPD | Validace délky šarže lokální; DPD API je dočasně přeskočené. Přeskočení nepřidává HTTP provoz. |
| HTML mail / inspektor | Přesun existujících regexů do HtmlToPlainTextConverter; nedošlo ke změně globálního SQL připojování. |
| Projekty, balíčky, registrace | Nové projekty a HtmlAgilityPack; v hlavním intervalu žádná výměna SqlClient balíčku nebo verze .NET cíle stávajícího ORM. |
| Starší import 8.–11. 9. | Nově evidence selhání a Resolve pro jednotlivé objednávky přidávají SQL práci; relevantní k zatížení importem, ale bez důkazu úniku poolu. |

## Pořadí hypotéz a další rozhodnutí

1. **SQL blokování nebo dlouhá práce vlastněná w3wp.** Nejlépe odpovídá ukončení problému při restartu klienta. Potvrzení: LCK_M_* a blokující SPID/PID patřící w3wp, zejména sleeping session s otevřenou transakcí. Široký dopad na nezávislé tabulky znamená, že nestačí automaticky obvinit jediný zámek objednávky.
2. **Nedostatek RAM/CPU/I/O, zvlášť při společném hostiteli IIS a SQL.** Restart webu může ulevit SQL i bez změny DB. Předchozí uložený rozbor uvádí společný stroj, zde to nebylo nově provozně ověřeno. Potvrzení: souběžný vývoj Private Bytes/GC webu, volné RAM, SQL memory grants a I/O čekání.
3. **SQL zátěž udržovaná cache miss a pollingem.** Kód mechanismus obsahuje a logy ukazují postižené cache cesty. Není důkaz, že právě tento mechanismus byl prvotní spouštěč. Oprava koordinace cache má vyšší obecný přínos než zvyšování timeoutů.
4. **Nové joby nebo změna SQL plánů.** Prověřit reálné časy Jobs logu, plán PurchaseOrder dotazů a změny po novém indexu. Plánovaný job nezmizí při IISRESET; bez těchto dat nižší shoda.
5. **Nový únik spojení / deadlock collector.** Po auditu a dnešním logu nízké podezření; ne absolutně vyloučeno pro jiný proces nebo jinou nasazenou verzi.

Při dalším incidentu použít už existující [CaptureElsaSlowdown.sql](CaptureElsaSlowdown.sql) PŘED restartem. Tři krátké snímky umožní spojit čekání, transakci a host_process_id. Současně uložit využití RAM/CPU/disku pro w3wp, sqlservr a JobLauncher a jejich PID. Pokud SQL ukáže klid, ale web je zahlcený, je potřeba dump webového procesu během problému. Uchovat frontend a Jobs log pro stejný interval a přesný nasazený commit. Teprve pak má smysl rozhodnout mezi opravou transakce, cache, konkrétního SQL plánu či paměti.

[Microsoft: SQL blocking](https://learn.microsoft.com/en-us/troubleshoot/sql/database-engine/performance/understand-resolve-blocking) popisuje právě rozlišení aktivního dotazu a neaktivní session s otevřenou transakcí. Restart IIS zároveň ukončí jeho spojení a změní paměť i počet běžících požadavků; sám tedy neurčuje jedinou příčinu.

Neprovádět současně několik experimentálních zásahů, protože pak nebude jasné, co pomohlo. Dosavadní lokální vypnutí sledování zásilek ponechat při posouzení jako samostatný experiment, jehož úspěch lze hodnotit až podle skutečně nasazené verze a celého zatěžovaného dne.

## Ověření review

- Porovnání osmice commitů a navazujících implementací; dohledání historie starších podezřelých míst.
- Nezávislé parsování 31 timeoutů a korelace 49 startů/dokončení ERP akcí z místního logu.
- Dva cílené JS scénáře nad skutečným zdrojovým kódem prošly v Node bez buildu a bez sítě.
- Produkční root cause ani oprava nebyly provozně potvrzeny. C# testy vyžadující build nespouštěny.
- Vytvořen pouze tento report a seznam měněných souborů. CRLF ověřeno.
