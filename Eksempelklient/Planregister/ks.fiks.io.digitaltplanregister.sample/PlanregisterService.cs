using Ks.Fiks.Maskinporten.Client;
using KS.Fiks.ASiC_E;
using KS.Fiks.IO.Client;
using KS.Fiks.IO.Client.Configuration;
using KS.Fiks.IO.Client.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ks.fiks.io.digitaltplanregister.sample
{

    public class PlanregisterService : IHostedService, IDisposable
    {
        FiksIOClient client;

        public PlanregisterService()
        {
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Planregister Service is starting.");
            IConfiguration config = new ConfigurationBuilder()

            .AddJsonFile("appsettings.json", true, true)
            .AddJsonFile("appsettings.development.json", true, true)
            .Build();

            Console.WriteLine("Setter opp FIKS integrasjon for digitalt planregister...");
            Guid accountId = Guid.Parse(config["accountId"]);  /* Fiks IO accountId as Guid Banke kommune digitalt planregister konto*/
            string privateKey = File.ReadAllText("privkey.pem"); ; /* Private key for offentlig nøkkel supplied to Fiks IO account */
            Guid integrationId = Guid.Parse(config["integrationId"]); /* Integration id as Guid ePlansak system X */
            string integrationPassword = config["integrationPassword"];  /* Integration password */

            // Fiks IO account configuration
            var account = new KontoConfiguration(
                                accountId,
                                privateKey);

            // Id and password for integration associated to the Fiks IO account.
            var integration = new IntegrasjonConfiguration(
                                    integrationId,
                                    integrationPassword, "ks:fiks");

            // ID-porten machine to machine configuration
            var maskinporten = new MaskinportenClientConfiguration(
                audience: @"https://oidc-ver2.difi.no/idporten-oidc-provider/", // ID-porten audience path
                tokenEndpoint: @"https://oidc-ver2.difi.no/idporten-oidc-provider/token", // ID-porten token path
                issuer: @"arkitektum_test",  // issuer name
                numberOfSecondsLeftBeforeExpire: 10, // The token will be refreshed 10 seconds before it expires
                certificate: GetCertificate(config["ThumbprintIdPortenVirksomhetssertifikat"]));

            // Optional: Use custom api host (i.e. for connecting to test api)
            var api = new ApiConfiguration(
                            scheme: "https",
                            host: "api.fiks.test.ks.no",
                            port: 443);

            // Optional: Use custom amqp host (i.e. for connection to test queue)
            var amqp = new AmqpConfiguration(
                            host: "io.fiks.test.ks.no",
                            port: 5671);

            // Combine all configurations
            var configuration = new FiksIOConfiguration(account, integration, maskinporten, api, amqp);
            client = new FiksIOClient(configuration); // See setup of configuration below



            client.NewSubscription(OnReceivedMelding);

            Console.WriteLine("Abonnerer på meldinger på konto " + accountId.ToString() + " ...");

            return Task.CompletedTask;
        }

        static void OnReceivedMelding(object sender, MottattMeldingArgs mottatt)
        {
            // Process the message
            if (mottatt.Melding.MeldingType == "no.ks.fiks.gi.plan.innsyn.finnplanerformatrikkelenhet.v2")
            {
                Console.WriteLine("Melding " + mottatt.Melding.MeldingId + " " + mottatt.Melding.MeldingType + " mottas...");
                string payload = File.ReadAllText("sampleResultatPlaner.json");
                string jsonSchemaName = "no.ks.fiks.gi.plan.innsyn.finnplanerformatrikkelenhet.v2.schema.json";
                string payloadJsonSchemaName = "no.ks.fiks.gi.plan.innsyn.planerformatrikkelenhet.v2.schema.json";
                string returnMeldingstype = "no.ks.fiks.gi.plan.innsyn.planerformatrikkelenhet.v2";

                HandleRequestWithReturnPayload(mottatt, jsonSchemaName, payload, payloadJsonSchemaName, returnMeldingstype);
            }

            else if (mottatt.Melding.MeldingType == "no.ks.fiks.gi.plan.innsyn.finnplaner.v2") // Sjekke med Tor Kjetil om korrekt meldingstype
            {
                Console.WriteLine("Melding " + mottatt.Melding.MeldingId + " " + mottatt.Melding.MeldingType + " mottas...");
                string payload = File.ReadAllText("sampleResultatPlaner.json");
                string jsonSchemaName = "no.ks.fiks.gi.plan.innsyn.finnplaner.v2.schema.json";
                string payloadJsonSchemaName = "no.ks.fiks.gi.plan.innsyn.planerforsøk.v2.schema.json";
                string returnMeldingstype = "no.ks.fiks.gi.plan.oppdatering.mottatt.v2";

                HandleRequestWithReturnPayload(mottatt, jsonSchemaName, payload, payloadJsonSchemaName, returnMeldingstype);
            }
            else if (mottatt.Melding.MeldingType == "no.ks.fiks.gi.plan.innsyn.finndispensasjoner.v2") // Sjekke med Tor Kjetil om korrekt meldingstype
            {
                Console.WriteLine("Melding " + mottatt.Melding.MeldingId + " " + mottatt.Melding.MeldingType + " mottas...");
                string payload = File.ReadAllText("sampleResultatDispensasjoner.json");
                string jsonSchemaName = "no.ks.fiks.gi.plan.innsyn.finndispensasjoner.v2.schema.json";
                string payloadJsonSchemaName = "no.ks.fiks.gi.plan.oppdatering.registrerdispensasjonplan.v2.schema.json";
                string returnMeldingstype = "no.ks.fiks.gi.plan.oppdatering.mottatt.v2";


                HandleRequestWithReturnPayload(mottatt, jsonSchemaName, payload, payloadJsonSchemaName, returnMeldingstype);
            }
            else if (mottatt.Melding.MeldingType == "no.ks.fiks.gi.plan.innsyn.finnplanbehandlinger.v2") // Sjekke med Tor Kjetil om korrekt meldingstype
            {
                Console.WriteLine("Melding " + mottatt.Melding.MeldingId + " " + mottatt.Melding.MeldingType + " mottas...");
                string payload = File.ReadAllText("sampleResultatPlaner.json");
                string jsonSchemaName = "no.ks.fiks.gi.plan.innsyn.finnplanbehandlinger.v2.schema.json";
                string payloadJsonSchemaName = "";//Mangler skjema for payload? 
                string returnMeldingstype = "no.ks.fiks.gi.plan.oppdatering.mottatt.v2";

                HandleRequestWithReturnPayload(mottatt, jsonSchemaName, payload, payloadJsonSchemaName, returnMeldingstype);
            }
            else if (mottatt.Melding.MeldingType == "no.ks.fiks.gi.plan.innsyn.hentarealplan.v2") // Sjekke med Tor Kjetil om korrekt meldingstype
            {
                Console.WriteLine("Melding " + mottatt.Melding.MeldingId + " " + mottatt.Melding.MeldingType + " mottas...");
                string payload = File.ReadAllText("sampleResultatPlaner.json"); //Skulle inkludert en planbehandling i return? Trenger hvis vi skal teste på returPlanbhenaldinger true/false
                string jsonSchemaName = "no.ks.fiks.gi.plan.innsyn.hentarealplan.v2.schema.json";
                string payloadJsonSchemaName = "";//Mangler skjema for payload? 
                string returnMeldingstype = "no.ks.fiks.gi.plan.oppdatering.mottatt.v2";

                HandleRequestWithReturnPayload(mottatt, jsonSchemaName, payload, payloadJsonSchemaName, returnMeldingstype);
            }
            else if (mottatt.Melding.MeldingType == "no.ks.fiks.gi.plan.innsyn.sjekkmidlertidigforbud.v2") // Sjekke med Tor Kjetil om korrekt meldingstype
            {
                Console.WriteLine("Melding " + mottatt.Melding.MeldingId + " " + mottatt.Melding.MeldingType + " mottas...");
                string payload = File.ReadAllText(""); //Trenger å vite jsonschema for å lage payload.
                string jsonSchemaName = "no.ks.fiks.gi.plan.innsyn.sjekkmidlertidigforbud.v2.schema.json";
                string payloadJsonSchemaName = "";//Mangler skjema for payload? 
                string returnMeldingstype = "no.ks.fiks.gi.plan.oppdatering.mottatt.v2";

                HandleRequestWithReturnPayload(mottatt, jsonSchemaName, payload, payloadJsonSchemaName, returnMeldingstype);
            }
            else if (mottatt.Melding.MeldingType == "no.ks.fiks.gi.plan.oppdatering.opprettarealplan.v2")
            {
                Console.WriteLine("Melding " + mottatt.Melding.MeldingId + " " + mottatt.Melding.MeldingType + " mottas...");
                string payload = File.ReadAllText("sampleNyPlanident.json");
                string jsonSchemaName = "no.ks.fiks.gi.plan.oppdatering.opprettarealplan.v2.schema.json";
                string payloadJsonSchemaName = "no.ks.fiks.gi.plan.oppdatering.meldingomplanident.v2.schema.json";
                string returnMeldingstype = "no.ks.fiks.gi.plan.oppdatering.meldingomplanident.v2";

                HandleRequestWithReturnPayload(mottatt, jsonSchemaName, payload, payloadJsonSchemaName, returnMeldingstype);
            }
            else if (mottatt.Melding.MeldingType == "no.ks.fiks.gi.plan.oppdatering.registrerPlanbehandling.v2")
            {
                Console.WriteLine("Melding " + mottatt.Melding.MeldingId + " " + mottatt.Melding.MeldingType + " mottas...");
                string jsonSchemaName = "no.ks.fiks.gi.plan.oppdatering.registrerPlanbehandling.v2.schema.json";
                string returnMeldingstype = "no.ks.fiks.gi.plan.oppdatering.mottatt.v2";

                HandleRequestWithoutReturnPayload(mottatt, jsonSchemaName, returnMeldingstype);
            }
            else if (mottatt.Melding.MeldingType == "no.ks.fiks.gi.plan.oppdatering.registrerplanavgrensning.v2")
            {
                Console.WriteLine("Melding " + mottatt.Melding.MeldingId + " " + mottatt.Melding.MeldingType + " mottas...");
                string jsonSchemaName = "no.ks.fiks.gi.plan.oppdatering.registrerplanavgrensning.v2.schema.json";
                string returnMeldingstype = "no.ks.fiks.gi.plan.oppdatering.mottatt.v2";

                HandleRequestWithoutReturnPayload(mottatt, jsonSchemaName, returnMeldingstype);
            }

            else if (mottatt.Melding.MeldingType == "no.ks.fiks.gi.plan.oppdatering.registrerdispensasjonplan.v2")
            {
                Console.WriteLine("Melding " + mottatt.Melding.MeldingId + " " + mottatt.Melding.MeldingType + " mottas...");
                string jsonSchemaName = "no.ks.fiks.gi.plan.oppdatering.registrerdispensasjonplan.v2.schema.json";
                string returnMeldingstype = "no.ks.fiks.gi.plan.oppdatering.mottatt.v2";

                HandleRequestWithoutReturnPayload(mottatt, jsonSchemaName, returnMeldingstype);
            }
            else if (mottatt.Melding.MeldingType == "no.ks.fiks.gi.plan.oppdatering.oppdaterarealplan.v2")
            {
                Console.WriteLine("Melding " + mottatt.Melding.MeldingId + " " + mottatt.Melding.MeldingType + " mottas...");
                string jsonSchemaName = "no.ks.fiks.gi.plan.oppdatering.oppdaterarealplan.v2.schema.json";
                string returnMeldingstype = "no.ks.fiks.gi.plan.oppdatering.mottatt.v2";

                HandleRequestWithoutReturnPayload(mottatt, jsonSchemaName, returnMeldingstype);
            }

            //else if (mottatt.Melding.MeldingType == "no.ks.fiks.gi.plan.oppdatering.planleggingigangsatt.v2")
            //{
            //    Console.WriteLine("Melding " + mottatt.Melding.MeldingId + " " + mottatt.Melding.MeldingType + " mottas...");
            //    string jsonSchemaName = "no.ks.fiks.gi.plan.oppdatering.planleggingigangsatt.v2.schema.json";
            //    string returnMeldingstype = "no.ks.fiks.gi.plan.oppdatering.mottatt.v2";

            //    HandleRequestWithoutReturnPayload(mottatt, jsonSchemaName, returnMeldingstype);
            //}

            //else if (mottatt.Melding.MeldingType == "no.ks.fiks.gi.plan.oppdatering.planvedtakikraftsatt.v2")
            //{
            //    Console.WriteLine("Melding " + mottatt.Melding.MeldingId + " " + mottatt.Melding.MeldingType + " mottas...");
            //    string jsonSchemaName = "no.ks.fiks.gi.plan.oppdatering.planvedtakikraftsatt.v2.schema.json";
            //    string returnMeldingstype = "no.ks.fiks.gi.plan.oppdatering.mottatt.v2";

            //    HandleRequestWithoutReturnPayload(mottatt, jsonSchemaName, returnMeldingstype);
            //}
            else if (mottatt.Melding.MeldingType == "no.ks.fiks.gi.plan.oppdatering.registrermidlertidigforbudmottiltak.v2")
            {
                Console.WriteLine("Melding " + mottatt.Melding.MeldingId + " " + mottatt.Melding.MeldingType + " mottas...");
                string jsonSchemaName = "no.ks.fiks.gi.plan.oppdatering.registrermidlertidigforbudmottiltak.v2.schema.json";
                string returnMeldingstype = "no.ks.fiks.gi.plan.oppdatering.mottatt.v2";

                HandleRequestWithoutReturnPayload(mottatt, jsonSchemaName, returnMeldingstype);
            }
        }

        private static void HandleRequestWithoutReturnPayload(MottattMeldingArgs mottatt, string jsonSchemaName, string returnMeldingstype)
        {
            Console.WriteLine("Melding " + mottatt.Melding.MeldingId + " " + mottatt.Melding.MeldingType + " mottas...");



            if (mottatt.Melding.HasPayload)
            { // Verify that message has payload
                List<List<string>> errorMessages = new List<List<string>>() { new List<string>(), new List<string>() };
                IAsicReader reader = new AsiceReader();
                using (var inputStream = mottatt.Melding.DecryptedStream.Result)
                using (var asice = reader.Read(inputStream))
                {
                    foreach (var asiceReadEntry in asice.Entries)
                    {
                        using (var entryStream = asiceReadEntry.OpenStream())
                        {
                            if (asiceReadEntry.FileName.Contains(".json"))
                            {
                                errorMessages = ValidateJsonFile(new StreamReader(entryStream).ReadToEnd(), Path.Combine("schema", jsonSchemaName));
                            }
                            else
                                Console.WriteLine("Mottatt vedlegg: " + asiceReadEntry.FileName);
                        }
                    }
                }

                if (errorMessages[0].Count == 0)
                {
                    var svarmsg = mottatt.SvarSender.Svar(returnMeldingstype).Result;
                    Console.WriteLine("Svarmelding " + svarmsg.MeldingId + " " + svarmsg.MeldingType + " sendt...");
                    mottatt.SvarSender.Ack(); // Ack message to remove it from the queue
                }
                else
                {
                    Console.WriteLine("Feil i validering med jsonschema: ", jsonSchemaName);
                    mottatt.SvarSender.Svar("no.ks.fiks.kvittering.ugyldigforespørsel.v1", String.Join("\n ", errorMessages[0]), "feil.txt");
                    Console.WriteLine(String.Join("\n ", errorMessages[0]));
                    mottatt.SvarSender.Ack(); // Ack message to remove it from the queue
                }
            }
            else
            {
                var svarmsg = mottatt.SvarSender.Svar("no.ks.fiks.kvittering.ugyldigforespørsel.v1", "Meldingen mangler innhold", "feil.txt").Result;
                Console.WriteLine("Svarmelding " + svarmsg.MeldingId + " " + svarmsg.MeldingType + " Meldingen mangler innhold");

                mottatt.SvarSender.Ack(); // Ack message to remove it from the queue
            }
        }

        private static void HandleRequestWithReturnPayload(MottattMeldingArgs mottatt, string jsonSchemaName, string payload, string payloadJsonSchemaName, string returnMeldingstype)
        {
            Console.WriteLine("Melding " + mottatt.Melding.MeldingId + " " + mottatt.Melding.MeldingType + " mottas...");



            if (mottatt.Melding.HasPayload)
            { // Verify that message has payload
                List<List<string>> errorMessages = new List<List<string>>() { new List<string>(), new List<string>() };
                IAsicReader reader = new AsiceReader();
                using (var inputStream = mottatt.Melding.DecryptedStream.Result)
                using (var asice = reader.Read(inputStream))
                {
                    foreach (var asiceReadEntry in asice.Entries)
                    {
                        using (var entryStream = asiceReadEntry.OpenStream())
                        {
                            if (asiceReadEntry.FileName.Contains(".json"))
                            {
                                errorMessages = ValidateJsonFile(new StreamReader(entryStream).ReadToEnd(), Path.Combine("schema", jsonSchemaName));
                            }
                            else
                                Console.WriteLine("Mottatt vedlegg: " + asiceReadEntry.FileName);
                        }
                    }
                }

                if (errorMessages[0].Count == 0)
                {
                    errorMessages = ValidateJsonFile(payload, Path.Combine("schema", payloadJsonSchemaName));

                    if (errorMessages[0].Count == 0)
                    {
                        var svarmsg = mottatt.SvarSender.Svar(returnMeldingstype, payload, "resultat.json").Result; //Meldingstype på svar ikke definert er 08.07.21
                        Console.WriteLine("Svarmelding " + svarmsg.MeldingId + " " + svarmsg.MeldingType + " sendt...");
                        Console.WriteLine(payload);
                        mottatt.SvarSender.Ack(); // Ack message to remove it from the queue
                    }
                    else
                    {
                        Console.WriteLine("Feil i validering med jsonschema: ", payloadJsonSchemaName);
                        mottatt.SvarSender.Svar("no.ks.fiks.kvittering.ugyldigforespørsel.v1", String.Join("\n ", errorMessages[0]), "feil.txt");
                        Console.WriteLine(String.Join("\n ", errorMessages[0]));
                        mottatt.SvarSender.Ack(); // Ack message to remove it from the queue
                    }
                }
                else
                {
                    Console.WriteLine("Feil i validering med jsonschema: ", jsonSchemaName);
                    mottatt.SvarSender.Svar("no.ks.fiks.kvittering.ugyldigforespørsel.v1", String.Join("\n ", errorMessages[0]), "feil.txt");
                    Console.WriteLine(String.Join("\n ", errorMessages[0]));
                    mottatt.SvarSender.Ack(); // Ack message to remove it from the queue
                }
            }
            else
            {
                var svarmsg = mottatt.SvarSender.Svar("no.ks.fiks.kvittering.ugyldigforespørsel.v1", "Meldingen mangler innhold", "feil.txt").Result;
                Console.WriteLine("Svarmelding " + svarmsg.MeldingId + " " + svarmsg.MeldingType + " Meldingen mangler innhold");

                mottatt.SvarSender.Ack(); // Ack message to remove it from the queue
            }
        }

        private static List<List<string>> ValidateJsonFile(string jsonString, string pathToSchema)
        {
            List<List<string>> validationErrorMessages = new List<List<string>>() { new List<string>(), new List<string>() };
            using (TextReader file = File.OpenText(pathToSchema))
            {
                JObject jObject = JObject.Parse(jsonString);
                JSchema schema = JSchema.Parse(file.ReadToEnd());
                //TODO:Skille mellom errors og warnings hvis det er 
                jObject.Validate(schema, (o, a) =>
                {
                    validationErrorMessages[0].Add(a.Message);
                });
            }
            return validationErrorMessages;
        }


        public Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Planregister Service is stopping.2");
            //Client?
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            //Client?
            client.Dispose();
        }

        private static X509Certificate2 GetCertificate(string ThumbprintIdPortenVirksomhetssertifikat)
        {

            //Det samme virksomhetssertifikat som er registrert hos ID-porten
            X509Store store = new X509Store(StoreLocation.CurrentUser);
            X509Certificate2 cer = null;
            store.Open(OpenFlags.ReadOnly);
            //Henter Arkitektum sitt virksomhetssertifikat
            X509Certificate2Collection cers = store.Certificates.Find(X509FindType.FindByThumbprint, ThumbprintIdPortenVirksomhetssertifikat, false);
            if (cers.Count > 0)
            {
                cer = cers[0];
            };
            store.Close();

            return cer;
        }
    }
}
