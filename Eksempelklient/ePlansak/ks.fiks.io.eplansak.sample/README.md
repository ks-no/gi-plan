# fiks.io.eplansak.sample

## Oppsett av prosjekt
- Oppdater prosjektet med en egen appsettings.development.json som har med integrasjons passord i FIKS
- Kryptering av kommunikasjon/innhold: Oppdater med egen selvsignert krypteringsnøkkel (privkey.pem) som har lastet offentlig nøkkel opp i FIKS
- Autentisering: Bruk eget virksomhetssertifikat mot Maskinporten (ThumbprintIdPortenVirksomhetssertifikat)

## Oppsett av maskinporten
- Bestill eller bruke eget virksomhetssertifikat
- Legg inn public del av virksomhetssertifikat i maskinporten: følg veiledning på [Difi maskinporten](https://samarbeid.difi.no/felleslosninger/maskinporten)

## Oppsett i FIKS Integrasjon
- Lag eget selvsignert sertifikat for krypering i FIKS
- Last opp krypteringssertifikat i FIKS administrasjonen: følg veiledning nederst på [FIKS integrasjonsutvikling](https://ks-no.github.io/fiks-platform/integrasjoner/)

## FIKS IO meldingsprotokoll
For ePlansak systemer så er meldingsprotokoll no.ks.fiks. aktuell å støttes som avsender.

