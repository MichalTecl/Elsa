# Diagnostika deadlocků RobOrm

Elsa zapíná sběr v `CommonRegistry`. SQL příkazy přes `RobOrm.SqlServer.Database`
při chybě 1205 dostanou `Exception.Data["RobOrm.DeadlockDiagnosticId"]`.
Logger Elsy přidá ID do začátku chybové zprávy, i pokud je SQL výjimka zabalená
v jiné výjimce. Výjimka ani aplikační transakce se automaticky neopakují.

## Výstupy

V `C:\Elsa\Log\Deadlocks` vznikají:

- `<id>.xml`: UTC čas, server, databáze, ClientConnectionId, SPID (pokud jej
  obsahuje anglická chybová zpráva), SQL příkaz a stav sběru.
- `<id>-candidate-N.xdl`: kandidátní grafy otevřitelné v SQL Server Management Studio.

Stavy jsou `Pending`, `Candidates`, `NotFound` nebo `CollectionFailed`.
Kandidáti odpovídají časovému oknu -15 až +5 sekund, databázi oběti a případně
SPID oběti. Nejde o zaručené přiřazení: SPID se mohou znovu použít a čas aplikace
se může lišit od serveru. Synchronizujte hodiny serverů. ClientConnectionId se
ukládá jako kontext; základní graf nemusí obsahovat údaj umožňující jeho spárování.

## Provozní předpoklady

SQL Server musí mít spuštěnou `system_health` s cílem `event_file`.
Sběrač získá cestu z metadat relace a čte její rotované soubory na SQL serveru;
aplikace nepotřebuje přímý přístup k těmto souborům.
SQL účet musí mít oprávnění pro čtení Extended Events a serverových DMV
(typicky `VIEW SERVER STATE` do SQL Server 2019, `VIEW SERVER PERFORMANCE STATE`
od SQL Server 2022). RobOrm oprávnění ani relace sám nemění.
Po odmítnutí oprávnění se další pokusy se stejným připojením potlačí do restartu
procesu. Po udělení oprávnění proto aplikaci restartujte.

Účet aplikace potřebuje zápis do výstupní složky. Soubory mohou obsahovat SQL
literály a data z grafu; spravujte je jako ostatní diagnostické logy Elsy.
Hodnoty parametrů ani connection string se do výstupu explicitně nepřidávají.
Rotace těchto výstupních souborů není součástí implementace; zahrňte složku
Deadlocks do obvyklé archivace a úklidu logů.

## Omezení

Sběr běží na jednom vlákně s frontou 32 požadavků, bez přenosu ExecutionContext
nebo původní transakce. Diagnostické připojení má `Enlist=false`, timeout připojení
5 sekund a timeout SQL příkazů 5/10 sekund. Graf se hledá až třikrát s prodlevami
2, 5 a 5 sekund. Při plné frontě je tato skutečnost uvedena v Exception.Data
u diagnostického ID; soubor pro odmítnutý požadavek nevzniká.

Fronta není trvalá: při ukončení procesu se nevyřízená diagnostika ztratí.
Nezávislý záznam v `system_health` tím není ovlivněn. Chyba úložiště nesmí zastavit
pracovníka ani přepsat aplikační výjimku, takže při nemožnosti zápisu nemusí
vzniknout soubor. Přímá použití SqlClient mimo Database (např. migrace) nejsou
instrumentována. Ruční SQL je pokryto po dobu callbacku `Database.Execute`;
reader se z tohoto callbacku nesmí vracet k pozdějšímu čtení.

## Ověření na testovacím SQL Serveru

1. Vyvolejte skutečný deadlock dvěma transakcemi zamykajícími stejné dva řádky
   v opačném pořadí; jednu větev spusťte přes RobOrm. Ověřte, že volající dostane
   původní chybu 1205 a ID v logu odpovídá vzniklému XML a XDL.
2. Opakujte pro ORM SELECT, kde chyba přijde až při `Read()`, a pro ruční SQL
   s více výsledky přes `Read`/`NextResult`. Ověřte zachycení i jediný incident
   pro stejnou výjimku.
3. Ověřte s účtem bez oprávnění k Extended Events: původní chyba zůstane 1205,
   XML obsahuje `CollectionFailed` a další incidenty již neprovádějí XE dotazy.
4. Zastavte testovací `system_health` nebo odstraňte graf z dostupné historie:
   diagnostika musí skončit popisem nedostupnosti / `NotFound` bez změny chování
   původní operace. Po testu obnovte původní konfiguraci relace.
5. Zablokujte zápis do diagnostické složky: aplikační výjimka musí stále projít
   a pracovník musí pokračovat po obnovení přístupu.

V tomto vývojovém průchodu byla ověřena syntaxe C# a statické napojení cest.
Build ani integrační test se skutečným deadlockem nebyly spuštěny.
