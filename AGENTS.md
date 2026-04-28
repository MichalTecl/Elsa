# AGENTS.md

- Nikdy nespoustej build, pokud to neni vyslovne zadano.
- Projekt `Portal` neupravuj primo, pokud to neni vyslovne zadano. Jeho obsah se synchronizuje z odpovidajicich podprojektu pomoci utility, proto se zmeny maji delat primarne tam.
- Na dev stroji prubezne bezi tool, ktery prenasi UI a SQL soubory z podprojektu do `Portal`. Neni potreba do toho manualne zasahovat; vyjimka je mazani nebo prejmenovani souboru v podprojektu, kdy je potreba smazat stare soubory, ktere uz byly do `Portal` nakopirovane.
- V UI pouzivej normalni ceske popisky s diakritikou, i kdyz uzivatel pise bez diakritiky.
