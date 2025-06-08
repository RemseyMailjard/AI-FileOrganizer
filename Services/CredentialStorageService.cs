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
                DeleteApiKey(providerName);
                return;
            }

            try
            {
                using (var credential = new Credential())
                {
                    credential.Target = GetCredentialTarget(providerName);
                    credential.Username = providerName;

                    if (!string.IsNullOrWhiteSpace(azureEndpoint))
                    {
                        var azureConfig = new { ApiKey = apiKey, Endpoint = azureEndpoint };
                        credential.Password = JsonConvert.SerializeObject(azureConfig);
                        credential.Type = CredentialType.Generic;
                    }
                    else
                    {
                        credential.Password = apiKey;
                        credential.Type = CredentialType.Generic;
                    }

                    credential.PersistanceType = PersistanceType.LocalComputer;
                    credential.Save();
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
        /// <returns>Een System.Tuple<string, string> (APIKey, AzureEndpoint), of (null, null) indien niet gevonden of fout.</returns>
        public Tuple<string, string> GetApiKey(string providerName) // <<< GEWIJZIGD RETURN TYPE
        {
            if (string.IsNullOrWhiteSpace(providerName))
            {
                _logger.Log("FOUT bij ophalen API-sleutel: Providernaam is leeg.");
                return Tuple.Create<string, string>(null, null); // <<< GEWIJZIGD RETURN STATEMENT
            }

            try
            {
                using (var credential = new Credential())
                {
                    credential.Target = GetCredentialTarget(providerName);
                    credential.Load();

                    if (credential.Type == CredentialType.Generic && credential.Password != null && credential.Password.StartsWith("{") && credential.Password.Contains("\"ApiKey\"")) // Controleer ook op null
                    {
                        // Let op: DeserializeAnonymousType is prima, maar je kunt ook een specifieke klasse definiëren
                        // als je meer controle wilt of als de structuur complexer wordt.
                        var azureConfig = JsonConvert.DeserializeAnonymousType(credential.Password, new { ApiKey = "", Endpoint = "" });
                        _logger.Log($"INFO: Azure-configuratie voor '{providerName}' succesvol geladen.");
                        return Tuple.Create(azureConfig.ApiKey, azureConfig.Endpoint); // <<< GEWIJZIGD RETURN STATEMENT
                    }
                    else
                    {
                        _logger.Log($"INFO: API-sleutel voor '{providerName}' succesvol geladen.");
                        return Tuple.Create(credential.Password, (string)null); // <<< GEWIJZIGD RETURN STATEMENT
                    }
                }
            }
            // Specifieke uitzondering voor wanneer de credential niet gevonden wordt.
            // De CredentialManagement library gooit een Exception met een specifieke message.
            // Helaas geen specifieke exception type, dus we moeten op de message checken of een HResult.
            // Voor nu vangen we algemeen en loggen, maar dit kan verfijnd worden.
            catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1168) // ERROR_NOT_FOUND
            {
                _logger.Log($"INFO: Geen API-sleutel gevonden voor '{providerName}'.");
                return Tuple.Create<string, string>(null, null);
            }
            catch (Exception ex)
            {
                // Loggen van andere onverwachte fouten
                _logger.Log($"FOUT bij ophalen API-sleutel voor '{providerName}': {ex.Message}");
                return Tuple.Create<string, string>(null, null); // <<< GEWIJZIGD RETURN STATEMENT
            }
        }

        public void DeleteApiKey(string providerName)
        {
            if (string.IsNullOrWhiteSpace(providerName)) return;

            try
            {
                using (var credential = new Credential())
                {
                    credential.Target = GetCredentialTarget(providerName);
                    // Probeer eerst te laden om te zien of het bestaat, voorkomt exception als het er niet is.
                    // Echter, Delete() zelf gooit een exception als het niet bestaat, dus de catch is nodig.
                    credential.Delete();
                    _logger.Log($"INFO: API-sleutel voor '{providerName}' succesvol verwijderd.");
                }
            }
            catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1168) // ERROR_NOT_FOUND
            {
                _logger.Log($"INFO: API-sleutel voor '{providerName}' was niet aanwezig om te verwijderen (of al verwijderd).");
            }
            catch (Exception ex)
            {
                _logger.Log($"FOUT bij verwijderen API-sleutel voor '{providerName}': {ex.Message}");
            }
        }

        private string GetCredentialTarget(string providerName)
        {
            // Maak een target naam die voldoet aan de eisen (bv. niet te lang, geen rare tekens).
            // De huidige aanpak is redelijk.
            string safeProviderName = providerName.Replace(" ", "").Replace("(", "").Replace(")", "").Replace(".", "").Replace("/", "");
            string target = AppPrefix + safeProviderName;
            // Target names in Credential Manager kunnen beperkt zijn in lengte (bv. 256 chars).
            // Als providerName erg lang kan worden, overweeg een hash of inkorting.
            if (target.Length > 250) // Veilige marge
            {
                target = target.Substring(0, 250);
            }
            return target;
        }
    }
}