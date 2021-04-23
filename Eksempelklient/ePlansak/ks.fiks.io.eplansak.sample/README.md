# fiks.io.eplansak.sample

## Oppsett av prosjekt
- Oppdater prosjektet med en egen appsettings.development.json som har med integrasjons passord i FIKS
- Kryptering av kommunikasjon/innhold: Oppdater med egen selvsignert krypteringsnøkkel (privkey.pem) som har lastet offentlig nøkkel opp i FIKS
- Autentisering: Bruk eget virksomhetssertifikat mot Maskinporten (ThumbprintIdPortenVirksomhetssertifikat)

## Bakgrunn
Dette er et forarbeid til arbeidsoppgaver i fornying av geointegrasjon for å vise muligheter og eksempler på FIKS IO integrasjon.
Flyten i meldinger baserer seg på brukstilfeller og prosessdiagram definert i ePlansak, Nasjonal produktspesifikasjon for arealplan og digitalt planregister(NPAD), samt arbeid med SOSI Plan 5.0.

 ![Meldingsflyt fra ePlansak til digitalt planregister](ks.fiks.io.eplansak.sample/doc/ePlansakflytmotplanregister.png)
 ![Meldingsflyt fra eByggesak til digitalt planregister](ks.fiks.io.eplansak.sample/doc/eByggesakflytmotplanregister.png)

## Oppsett av maskinporten
- Bestill eller bruke eget virksomhetssertifikat
- Legg inn public del av virksomhetssertifikat i maskinporten: følg veiledning på [Difi maskinporten](https://samarbeid.difi.no/felleslosninger/maskinporten)

## Oppsett i FIKS Integrasjon
- Lag eget selvsignert sertifikat for krypering i FIKS
- Last opp krypteringssertifikat i FIKS administrasjonen: følg veiledning nederst på [FIKS integrasjonsutvikling](https://ks-no.github.io/fiks-platform/integrasjoner/)

## FIKS IO meldingsprotokoll
For ePlansak systemer så er meldingsprotokoll no.ks.fiks. aktuell å støttes som avsender.

