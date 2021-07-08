using KS.Fiks.IO.Client;
using KS.Fiks.IO.Client.Models;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using Microsoft.Extensions.Configuration;
using KS.Fiks.IO.Client.Configuration;
using Ks.Fiks.Maskinporten.Client;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace ks.fiks.io.eplansak.sample
{
    public class ePlansakService : IHostedService, IDisposable
    {
        FiksIOClient client;
        IConfiguration config;

        public ePlansakService()
        {
            config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile("appsettings.development.json", true, true)
                .Build();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {

            Console.WriteLine("ePlansak Service is starting.");
            Console.WriteLine("Setter opp FIKS integrasjon for ePlansak...");
            Guid accountId = Guid.Parse(config["accountId"]);  /* Fiks IO accountId as Guid Banke kommune digitalt planregister konto*/
            string privateKey =  File.ReadAllText("privkey.pem"); ; /* Private key for offentlig nøkkel supplied to Fiks IO account */
            Guid integrationId = Guid.Parse(config["integrationId"]); /* Integration id as Guid ePlansak system X */
            string integrationPassword = config["integrationPassword"];  /* Integration password */
            Guid sendToaccountId = Guid.Parse(config["sendToAccountId"]);

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

            Guid receiverId = sendToaccountId; // Receiver id as Guid
            Guid senderId = accountId; // Sender id as Guid

            client.NewSubscription(OnReceivedMelding);

            Console.WriteLine("Abonnerer på meldinger på konto " + accountId.ToString() + " ...");


            FinnPlanerForMatrikkelenhet();




            return Task.CompletedTask;
        }

        private void FinnPlanerForMatrikkelenhet()
        {
            Guid receiverId = Guid.Parse(config["sendToAccountId"]); // Receiver id as Guid
            Guid senderId = Guid.Parse(config["accountId"]); // Sender id as Guid

            var messageRequest = new MeldingRequest(
                                      mottakerKontoId: receiverId,
                                      avsenderKontoId: senderId,
                                      meldingType: "no.ks.fiks.gi.plan.innsyn.finnplanerformatrikkelenhet.v2");                                                                                               

            string payload = File.ReadAllText("sampleFinnPlanerMatrikkelenhet.json");

            var msg = client.Send(messageRequest, payload, "FinnPlanerMatrikkelenhet.json").Result;
            Console.WriteLine("Melding " + msg.MeldingId.ToString() + " sendt..." + msg.MeldingType);
            Console.WriteLine(payload);

        }

        static void OnReceivedMelding(object sender, MottattMeldingArgs fileArgs)
        {
            //Se oversikt over meldingstyper på https://github.com/ks-no/fiks-io-meldingstype-katalog/tree/test/schema

            // Process the message
            if (fileArgs.Melding.MeldingType == "no.ks.fiks.gi.plan.innsyn.planerformatrikkelenhet.v2")
            {
                Console.WriteLine("Melding " + fileArgs.Melding.MeldingId + " " + fileArgs.Melding.MeldingType + " mottas...");

                //TODO håndtere meldingen med ønsket funksjonalitet

                Console.WriteLine("ePlansak viser resultat");

                fileArgs.SvarSender.Ack(); // Ack message to remove it from the queue

            }
            else if (fileArgs.Melding.MeldingType == "no.geointegrasjon.plan.oppdatering.planidentopprettet.v1")
            {
                Console.WriteLine("Melding " + fileArgs.Melding.MeldingId + " " + fileArgs.Melding.MeldingType + " mottas...");

                //TODO håndtere meldingen med ønsket funksjonalitet

                Console.WriteLine("ePlansak oppdaterer sak med tiltdelt arealplanident......");

                fileArgs.SvarSender.Ack(); // Ack message to remove it from the queue

            }
            else if (fileArgs.Melding.MeldingType == "no.ks.geointegrasjon.ok.v1")
            {
                Console.WriteLine("Melding " + fileArgs.Melding.MeldingId + " " + fileArgs.Melding.MeldingType + " mottas...");

                //TODO håndtere meldingen med ønsket funksjonalitet

                Console.WriteLine("Melding er håndtert i planregister ok ......");

                fileArgs.SvarSender.Ack(); // Ack message to remove it from the queue

            }
            else
            {
                Console.WriteLine("Ubehandlet melding i køen " + fileArgs.Melding.MeldingId + " " + fileArgs.Melding.MeldingType);

            }
        }
        private void SendPlanleggingIgangsatt()
        {
            Guid receiverId = Guid.Parse(config["sendToAccountId"]); // Receiver id as Guid
            Guid senderId = Guid.Parse(config["accountId"]); // Sender id as Guid

            var messageRequest = new MeldingRequest(
                                      mottakerKontoId: receiverId,
                                      avsenderKontoId: senderId,
                                      meldingType: "no.geointegrasjon.plan.oppdatering.planleggingigangsatt.v1"); // Message type as string https://fiks.ks.no/plan.oppretteplanidentinput.v1.schema.json
                                                                                                           //Se oversikt over meldingstyper på https://github.com/ks-no/fiks-io-meldingstype-katalog/tree/test/schema


            string payload = File.ReadAllText("samplePlanleggingIgangsatt.json");
            

            var msg = client.Send(messageRequest, payload, "PlanleggingIgangsatt.json").Result;
            Console.WriteLine("Melding " + msg.MeldingId.ToString() + " sendt..." + msg.MeldingType);
            Console.WriteLine(payload);

        }

        private void SendPlanvedtak()
        {
            Guid receiverId = Guid.Parse(config["sendToAccountId"]); // Receiver id as Guid
            Guid senderId = Guid.Parse(config["accountId"]); // Sender id as Guid

            var messageRequest = new MeldingRequest(
                                      mottakerKontoId: receiverId,
                                      avsenderKontoId: senderId,
                                      meldingType: "no.geointegrasjon.plan.oppdatering.planvedtak.v1"); // Message type as string https://fiks.ks.no/plan.oppretteplanidentinput.v1.schema.json
                                                                                                        //Se oversikt over meldingstyper på https://github.com/ks-no/fiks-io-meldingstype-katalog/tree/test/schema

            client.Lookup(new LookupRequest("","", 3));

            string payload = File.ReadAllText("samplePlanvedtak.json");
            
            List<IPayload> payloads = new List<IPayload>();
            payloads.Add(new StringPayload(payload, "Planvedtak.json"));
            //payloads.Add(new KS.Fiks.IO.Client.Models.FilePayload(@"C:\dev\ks\ks.fiks.io.eplansak.sample\ks.fiks.io.eplansak.sample\files\06_36_2012_føresegner.pdf"));
            //payloads.Add(new KS.Fiks.IO.Client.Models.FilePayload(@"C:\dev\ks\ks.fiks.io.eplansak.sample\ks.fiks.io.eplansak.sample\files\06_36_2012_plankart.pdf"));
            //payloads.Add(new KS.Fiks.IO.Client.Models.FilePayload(@"C:\dev\ks\ks.fiks.io.eplansak.sample\ks.fiks.io.eplansak.sample\files\K-sak 112-12.pdf"));

            var msg = client.Send(messageRequest, payloads).Result;
            Console.WriteLine("Melding " + msg.MeldingId.ToString() + " sendt..." + msg.MeldingType + "...med 3 vedlegg");
            Console.WriteLine(payload);

        }

        private void SendPlanavgrensning()
        {
            Guid receiverId = Guid.Parse(config["sendToAccountId"]); // Konto for Planregister systemet
            Guid senderId = Guid.Parse(config["accountId"]); // Konto for ePlansak systemet

            var messageRequest = new MeldingRequest(
                                      mottakerKontoId: receiverId,
                                      avsenderKontoId: senderId,
                                      meldingType: "no.ks.fiks.gi.plan.oppdatering.registrerplanavgrensning.v2"); 

            string payload = File.ReadAllText("samplePlanavgrensning.json");

            List<IPayload> payloads = new List<IPayload>();
            payloads.Add(new StringPayload(payload, "Planavgrensning.json"));
            payloads.Add(new KS.Fiks.IO.Client.Models.FilePayload(@"omrissOppdatert.gml"));

            var msg = client.Send(messageRequest, payloads).Result;
            Console.WriteLine("Melding " + msg.MeldingId.ToString() + " sendt..." + msg.MeldingType + "...med 1 vedlegg");
            Console.WriteLine(payload);

        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("ePlansak Service is stopping.2");
            //Client?
            return Task.CompletedTask;
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
