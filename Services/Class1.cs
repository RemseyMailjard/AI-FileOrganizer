using Microsoft.ML.Tokenizers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using AI_FileOrganizer.Services;

public class OnnxRobBERTProvider : IAiProvider, IDisposable
{
    private readonly InferenceSession _session;
    private readonly BertTokenizer _tokenizer;
    private const int MaxSequenceLength = 128;

    public OnnxRobBERTProvider(AI_FileOrganizer.Utils.ILogger _logger, string modelPath, string vocabPath)
    {
        if (!File.Exists(modelPath))
            throw new FileNotFoundException("ONNX-model niet gevonden.", modelPath);
        if (!File.Exists(vocabPath))
            throw new FileNotFoundException("Vocab niet gevonden.", vocabPath);

        _session = new InferenceSession(modelPath);
        _tokenizer = BertTokenizer.Create(vocabPath);
    }

    /// <summary>
    /// Voorspel de best passende categorie uit een lijst op basis van de tekst.
    /// </summary>
    public string PredictCategory(string text, List<string> categories)
    {
        // [1] Tokenize
        var inputIds = _tokenizer.EncodeToIds(text, addSpecialTokens: true).Select(id => (long)id).ToList();
        var attentionMask = Enumerable.Repeat(1L, inputIds.Count).ToList();

        // [2] Padding
        while (inputIds.Count < MaxSequenceLength)
        {
            inputIds.Add(0);
            attentionMask.Add(0);
        }
        if (inputIds.Count > MaxSequenceLength)
        {
            inputIds = inputIds.Take(MaxSequenceLength).ToList();
            attentionMask = attentionMask.Take(MaxSequenceLength).ToList();
        }

        // [3] Naar tensor
        var inputIdsTensor = new DenseTensor<long>(new[] { 1, MaxSequenceLength });
        var attentionMaskTensor = new DenseTensor<long>(new[] { 1, MaxSequenceLength });
        for (int i = 0; i < MaxSequenceLength; i++)
        {
            inputIdsTensor[0, i] = inputIds[i];
            attentionMaskTensor[0, i] = attentionMask[i];
        }

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", inputIdsTensor),
            NamedOnnxValue.CreateFromTensor("attention_mask", attentionMaskTensor)
        };

        // [4] Forward pass
        using (var results = _session.Run(inputs))
        {
            var output = results.First().AsEnumerable<float>().ToArray();

            // [5] Softmax: output geeft waarschijnlijkheid per categorie
            var maxIdx = 0;
            float maxVal = output[0];
            for (int i = 1; i < output.Length; i++)
            {
                if (output[i] > maxVal)
                {
                    maxVal = output[i];
                    maxIdx = i;
                }
            }
            // Return de corresponderende categorie
            if (maxIdx < categories.Count)
                return categories[maxIdx];
            else
                return "Onbekend";
        }
    }

    // Voor AI interface (dummy)
    public Task<string> GetTextCompletionAsync(string prompt, string modelName, int maxTokens, float temperature, CancellationToken cancellationToken)
    {
        // Optioneel: kun je leeg laten of dezelfde als PredictCategory gebruiken
        return Task.FromResult("Not implemented for completion");
    }

    public void Dispose()
    {
        _session?.Dispose();
    }

    // ---- MINI TEST ----
    //public static void MiniTest()
    //{
    //    string modelPath = @"C:\Users\Remse\Desktop\onnx\model.onnx";
    //    string vocabPath = @"C:\Users\Remse\Desktop\onnx\vocab.txt";
    //    var provider = new OnnxRobBERTProvider(modelPath, vocabPath, "");

    //    var testTekst = "De belastingaangifte moet vóór 1 mei ingediend zijn bij de Belastingdienst.";
    //    var categorieen = new List<string> { "Financieel", "Gezondheid", "Reizen", "Werk", "Overig" };

    //    string predicted = provider.PredictCategory(testTekst, categorieen);
    //    Console.WriteLine($"Predicted category: {predicted}");
    //}
}
