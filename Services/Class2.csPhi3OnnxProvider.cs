using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class Phi3OnnxProvider : IDisposable
{
    private readonly InferenceSession _session;
    private const int MaxSequenceLength = 2048; // Of 128 of 512, afhankelijk van je model

    public Phi3OnnxProvider(string modelPath)
    {
        if (!File.Exists(modelPath))
            throw new FileNotFoundException("ONNX-model niet gevonden.", modelPath);

        _session = new InferenceSession(modelPath);
    }

    // Dummy tokenizer voor demo; in productie: gebruik de officiele Phi-3 tokenizer output!
    private int[] SimpleTokenizer(string input)
    {
        // TODO: Vervang door echte tokenizer
        return input.Split(' ').Select(s => Math.Abs(s.GetHashCode()) % 10000).ToArray();
    }

    public async Task<string> GetTextCompletionAsync(string prompt, CancellationToken cancellationToken)
    {
        // Tokenize input (gebruik echte tokenizer of importeer getokenizeerde input)
        var inputIds = SimpleTokenizer(prompt).Take(MaxSequenceLength).ToArray();

        // Padding indien nodig
        var inputList = inputIds.ToList();
        while (inputList.Count < MaxSequenceLength)
            inputList.Add(0);

        var inputTensor = new DenseTensor<long>(new[] { 1, MaxSequenceLength });
        for (int i = 0; i < MaxSequenceLength; i++)
            inputTensor[0, i] = inputList[i];

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", inputTensor)
            // Voeg meer tensors toe indien het model dat vereist, zoals "attention_mask"
        };

        using (var results = _session.Run(inputs))
        {
            // Pak de output (bijvoorbeeld "logits" of "output_ids")
            var output = results.First().AsEnumerable<float>().ToArray();

            // Dummy: retourneer eerste 10 waarden als string
            return string.Join(", ", output.Take(10));
        }
    }

    public void Dispose()
    {
        _session?.Dispose();
    }
}
