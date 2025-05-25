// AI_FileOrganizer/Services/IAiProvider.cs
using System.Threading;
using System.Threading.Tasks;

namespace AI_FileOrganizer.Services
{
    public interface IAiProvider
    {
        /// <summary>
        /// Roept de AI-API aan om tekstaanvulling te genereren.
        /// </summary>
        /// <param name="prompt">De prompt voor de AI.</param>
        /// <param name="modelName">De naam van het te gebruiken AI-model.</param>
        /// <param name="maxTokens">Het maximale aantal tokens om te genereren.</param>
        /// <param name="temperature">De creativiteit van de generatie (0.0 tot 1.0).</param>
        /// <param name="cancellationToken">Token voor annulering van de operatie.</param>
        /// <returns>De gegenereerde tekst of null bij falen.</returns>
        Task<string> GetTextCompletionAsync(string prompt, string modelName, int maxTokens, float temperature, CancellationToken cancellationToken);
    }
}