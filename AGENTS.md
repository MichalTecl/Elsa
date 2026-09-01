# AGENTS.md

- Nikdy nespoustej build, pokud to neni vyslovne zadano.
- Projekt `Portal` neupravuj primo, pokud to neni vyslovne zadano. Jeho obsah se synchronizuje z odpovidajicich podprojektu pomoci utility, proto se zmeny maji delat primarne tam.
- Na dev stroji prubezne bezi tool, ktery prenasi UI a SQL soubory z podprojektu do `Portal`. Neni potreba do toho manualne zasahovat; vyjimka je mazani nebo prejmenovani souboru v podprojektu, kdy je potreba smazat stare soubory, ktere uz byly do `Portal` nakopirovane.
- Pro nove entity nevytvarej rucni SQL migrace ani `CREATE TABLE` skripty. Databazova migrace se po zalozeni entity v projektu generuje automaticky.
- V UI pouzivej normalni ceske popisky s diakritikou, i kdyz uzivatel pise bez diakritiky.
- Nepouzivej legacy madarskou konvenci pro fieldy a konstanty. Pro fieldy pouzivej `_field`, pro konstanty `CONSTANT`.
- Nepridavej rucni verzovani ani cache-busting do URL statickych souboru, napriklad `?v=20260817-2`. Verzovani assetu je zajisteno jinym mechanismem.
- Pri zmene existujiciho C# souboru preved v celem upravovanem souboru privatni fieldy z legacy konvence `m_field` na `_field` a aktualizuj vsechny jejich reference.
- Pri kazde uprave textoveho souboru zachovej jeho puvodni konce radku. V tomto repozitari pouzivej podle `.editorconfig` a `.gitattributes` Windows konce radku CRLF; pred dokoncenim over, ze upravene soubory nemaji LF ani smisene konce radku.
