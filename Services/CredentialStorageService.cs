// AI_FileOrganizer/Services/CredentialStorageService.cs
using System;
using CredentialManagement; // Vereist de CredentialManagement NuGet-package
using AI_FileOrganizer.Utils; // Voor ILogger
using Newtonsoft.Json; // Voor het serialiseren van Azure-gegevens

namespace AI_FileOrganizer.Services
{
    public class CredentialStorageService
    {
        private readonly ILogger _logger;
        private const string AppPrefix = "AIFileOrganizer."; // Unieke prefix voor onze referenties

        public CredentialStorageService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Slaat een API-sleutel en optioneel een Azure Endpoint op in de Windows Credential Manager.
        /// De sleutel wordt opgeslagen als een credential voor de lokale gebruiker.
        /// </summary>
        /// <param name="providerName">De naam van de provider (bijv. "Gemini (Google)", "Azure OpenAI").</param>
        /// <param name="apiKey">De API-sleutel.</param>
        /// <param name="azureEndpoint">Optioneel: Het Azure Endpoint voor Azure OpenAI.</param>
        public void SaveApiKey(string providerName, string apiKey, string azureEndpoint = null)
        {
            if (string.IsNullOrWhiteSpace(providerName))
            {
                _logger.Log("FOUT bij opslaan API-sleutel: Providernaam is leeg.");
                return;
            }
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.Log($"WAARSCHUWING bij opslaan API-sleutel voor '{providerName}': API-sleutel is leeg. Verwijder bestaande.");
                DeleteApiKey(providerName); // Verwijder indien de sleutel wordt gewist
                return;
            }

            try
            {
                using (var credential = new Credential())
                {
                    credential.Target = GetCredentialTarget(providerName);
                    credential.Username = providerName; // Username kan de provider naam zijn, of leeg

                    if (!string.IsNullOrWhiteSpace(azureEndpoint))
                    {
                        // Voor Azure, combineer sleutel en endpoint in een JSON-string in het wachtwoordveld
                        var azureConfig = new { ApiKey = apiKey, Endpoint = azureEndpoint };
                        credential.Password = JsonConvert.SerializeObject(azureConfig);
                        credential.Type = CredentialType.Generic;
                    }
                    else
                    {
                        credential.Password = apiKey;
                        credential.Type = CredentialType.Generic; // Kan ook NetworkPassword zijn, maar Generic is flexibeler
                    }

                    credential.PersistanceType = PersistanceType.LocalComputer; // Alleen voor de huidige gebruiker

                    credential.Save(); // Slaat de referentie op
                    _logger.Log($"INFO: API-sleutel voor '{providerName}' succesvol opgeslagen.");
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"FOUT bij opslaan API-sleutel voor '{providerName}': {ex.Message}");
            }
        }

        /// <summary>
        /// Haalt een API-sleutel en optioneel Azure Endpoint op uit de Windows Credential Manager.
        /// </summary>
        /// <param name="providerName">De naam van de provider.</param>
        /// <returns>Een tuple (APIKey, AzureEndpoint), of (null, null) indien niet gevonden of fout.</returns>
        public (string ApiKey, string AzureEndpoint) GetApiKey(string providerName)
        {
            if (string.IsNullOrWhiteSpace(providerName))
            {
                _logger.Log("FOUT bij ophalen API-sleutel: Providernaam is leeg.");
                return (null, null);
            }

            try
            {
                using (var credential = new Credential())
                {
                    credential.Target = GetCredentialTarget(providerName);
                    credential.Load(); // Laadt de referentie

                    if (credential.Type == CredentialType.Generic && credential.Password.StartsWith("{") && credential.Password.Contains("ApiKey"))
                    {
                        // Dit is waarschijnlijk een Azure-configuratie (JSON)
                        var azureConfig = JsonConvert.DeserializeAnonymousType(credential.Password, new { ApiKey = "", Endpoint = "" });
                        _logger.Log($"INFO: Azure-configuratie voor '{providerName}' succesvol geladen.");
                        return (azureConfig.ApiKey, azureConfig.Endpoint);
                    }
                    else
                    {
                        _logger.Log($"INFO: API-sleutel voor '{providerName}' succesvol geladen.");
                        return (credential.Password, null); // Retourneer alleen de sleutel
                    }
                }
            }

            catch (Exception ex)
            {
                _logger.Log($"FOUT bij ophalen API-sleutel voor '{providerName}': {ex.Message}");
                return (null, null);
            }
        }

        /// <summary>
        /// Verwijdert een opgeslagen API-sleutel uit de Windows Credential Manager.
        /// </summary>
        /// <param name="providerName">De naam van de provider.</param>
        public void DeleteApiKey(string providerName)
        {
            if (string.IsNullOrWhiteSpace(providerName)) return;

            try
            {
                using (var credential = new Credential())
                {
                    credential.Target = GetCredentialTarget(providerName);
                    credential.Delete(); // Verwijdert de referentie
                    _logger.Log($"INFO: API-sleutel voor '{providerName}' succesvol verwijderd.");
                }
            }
          //  catch (Exception ex)
        //    {
                // Al niet aanwezig, dat is prima
           //     _logger.Log($"INFO: API-sleutel voor '{providerName}' was niet aanwezig om te verwijderen.");
       //     }
            catch (Exception ex)
            {
                _logger.Log($"FOUT bij verwijderen API-sleutel voor '{providerName}': {ex.Message}");
            }
        }

        private string GetCredentialTarget(string providerName)
        {
            return AppPrefix + providerName.Replace(" ", "").Replace("(", "").Replace(")", "").Replace(".", ""); // Maak een veilige target naam
        }
    }
}