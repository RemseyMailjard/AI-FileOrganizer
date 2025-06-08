// In AI_FileOrganizer/Services/EmptyFolderCleaner.cs
using AI_FileOrganizer.Utils;
using System;
using System.IO;
using System.Linq; // Nodig voor Enumerable.Any()

namespace AI_FileOrganizer.Services
{
    public class EmptyFolderCleaner
    {
        private readonly ILogger _logger;

        public EmptyFolderCleaner(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Verwijdert alle lege submappen binnen de opgegeven root directory.
        /// De root directory zelf wordt niet verwijderd.
        /// </summary>
        /// <param name="rootDirectory">Het pad naar de map waarin gezocht wordt.</param>
        /// <returns>True als er minstens één lege map is verwijderd, anders false.</returns>
        public bool DeleteEmptySubFolders(string rootDirectory) // Naam iets duidelijker
        {
            if (string.IsNullOrWhiteSpace(rootDirectory) || !Directory.Exists(rootDirectory))
            {
                _logger.Log($"FOUT: Map voor opschonen bestaat niet of pad is ongeldig: '{rootDirectory}'");
                return false;
            }

            _logger.Log($"INFO: Starten met controleren op lege submappen in '{rootDirectory}'.");
            bool anyFolderDeleted = DeleteEmptyFoldersRecursive(rootDirectory);
            if (!anyFolderDeleted)
            {
                _logger.Log($"INFO: Geen lege submappen gevonden om te verwijderen in '{rootDirectory}'.");
            }
            return anyFolderDeleted;
        }

        // De isRoot parameter is om te voorkomen dat de root folder zelf per ongeluk wordt gecheckt voor verwijdering
        // als de recursieve aanroep direct op de root zou beginnen en de root leeg zou zijn.
        // Echter, de huidige structuur van DeleteEmptySubFolders roept DeleteEmptyFoldersRecursive aan op subDir,
        // dus de root zelf wordt niet direct aan de 'verwijder als leeg' check onderworpen.
        // We kunnen het simpeler houden.
        private bool DeleteEmptyFoldersRecursive(string currentDirectory)
        {
            bool deletedInThisCall = false;

            // Eerst recursief alle submappen doorlopen
            // Gebruik try-catch voor GetDirectories voor het geval van permissieproblemen
            string[] subDirectories;
            try
            {
                subDirectories = Directory.GetDirectories(currentDirectory);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.Log($"WAARSCHUWING: Geen toegang tot submappen van '{currentDirectory}': {ex.Message}");
                return false; // Kan niet verder in deze tak
            }
            catch (Exception ex)
            {
                _logger.Log($"FOUT bij het ophalen van submappen van '{currentDirectory}': {ex.Message}");
                return false;
            }


            foreach (var subDir in subDirectories)
            {
                if (DeleteEmptyFoldersRecursive(subDir)) // Als de recursieve call iets heeft verwijderd
                {
                    deletedInThisCall = true;
                }
            }

            // Nadat alle submappen zijn verwerkt, controleer of de huidige map leeg is
            // EN dat het niet de root map is waarop de operatie is gestart (hoewel dit door de aanroeper wordt afgehandeld)
            if (IsDirectoryEmpty(currentDirectory))
            {
                try
                {
                    Directory.Delete(currentDirectory);
                    _logger.Log($"INFO: Lege map verwijderd: {currentDirectory}");
                    deletedInThisCall = true;
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.Log($"WAARSCHUWING: Geen permissie om map te verwijderen '{currentDirectory}': {ex.Message}");
                }
                catch (IOException ex) // Bijv. als map in gebruik is
                {
                    _logger.Log($"WAARSCHUWING: Map kon niet verwijderd worden (mogelijk in gebruik) '{currentDirectory}': {ex.Message}");
                }
                catch (Exception ex)
                {
                    _logger.Log($"FOUT bij verwijderen van map '{currentDirectory}': {ex.Message}");
                }
            }
            return deletedInThisCall;
        }

        private bool IsDirectoryEmpty(string path)
        {
            try
            {
                // Enumerable.Any() is efficiënter dan GetFiles().Length == 0 omdat het stopt na het eerste item.
                return !Directory.EnumerateFileSystemEntries(path).Any();
            }
            catch (UnauthorizedAccessException)
            {
                _logger.Log($"WAARSCHUWING: Geen toegang om inhoud te controleren van map '{path}'. Wordt niet als leeg beschouwd.");
                return false; // Als we geen toegang hebben, beschouw het niet als leeg om veilig te zijn
            }
            catch (Exception ex)
            {
                _logger.Log($"FOUT bij controleren of map leeg is '{path}': {ex.Message}. Wordt niet als leeg beschouwd.");
                return false;
            }
        }
    }
}