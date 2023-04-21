IF NOT EXISTS (SELECT 1 FROM ShipmentProviderLookup)
BEGIN
    INSERT INTO ShipmentProviderLookup (ProjectId, ProviderName, ShipMethodWildcardPattern)
    VALUES
        (1, 'zasilkovna', '*Zásilkovna*'),
        (1, 'zasilkovna', '*SLOVENSKO - Kurýr*'),
        (1, 'dpd', '*DPD*')
END
