using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace DesktopEye.Services.TranslationService;

public class NllbOnnxTranslationService : ITranslationService
{
    private InferenceSession _session;
    private Dictionary<string, int> _vocab;
    private Dictionary<int, string> _reverseVocab;
    private TokenizerConfig _tokenizerConfig;

    public NllbOnnxTranslationService(string modelPath, string tokenizerPath, string tokenizerConfigPath = null)
    {
        LoadModel(modelPath, tokenizerPath, tokenizerConfigPath);
    }
    
    public class TokenizerConfig
    {
        public string model_type { get; set; }
        public Dictionary<string, object> added_tokens_decoder { get; set; }
        public string bos_token { get; set; }
        public string eos_token { get; set; }
        public string pad_token { get; set; }
        public string unk_token { get; set; }
        public List<string> src_lang { get; set; }
        public List<string> tgt_lang { get; set; }
    }
    
    public class TokenizerData
    {
        public Dictionary<string, int> vocab { get; set; }
        public List<object> merges { get; set; }
        public Dictionary<string, object> added_tokens { get; set; }
        public Dictionary<string, object> normalizer { get; set; }
        public Dictionary<string, object> pre_tokenizer { get; set; }
        public Dictionary<string, object> post_processor { get; set; }
        public Dictionary<string, object> decoder { get; set; }
    }
    
    public void LoadModel(string modelPath, string tokenizerPath, string tokenizerConfigPath = null)
    {
        try
        {
            // Load ONNX model
            Console.WriteLine("Loading ONNX model...");
            var sessionOptions = new SessionOptions();
            sessionOptions.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_EXTENDED;
            
            _session = new InferenceSession(modelPath, sessionOptions);
            Console.WriteLine($"Model loaded successfully. Input names: {string.Join(", ", _session.InputMetadata.Keys)}");
            Console.WriteLine($"Output names: {string.Join(", ", _session.OutputMetadata.Keys)}");
            
            // Load tokenizer
            LoadTokenizer(tokenizerPath);
            
            // Load tokenizer config if provided
            if (!string.IsNullOrEmpty(tokenizerConfigPath) && File.Exists(tokenizerConfigPath))
            {
                LoadTokenizerConfig(tokenizerConfigPath);
            }
            
            Console.WriteLine("Model and tokenizer loaded successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading model: {ex.Message}");
            throw;
        }
    }
    
    private void LoadTokenizer(string tokenizerPath)
    {
        Console.WriteLine("Loading tokenizer...");
        
        if (!File.Exists(tokenizerPath))
        {
            throw new FileNotFoundException($"Tokenizer file not found: {tokenizerPath}");
        }
        
        try
        {
            string jsonContent = File.ReadAllText(tokenizerPath);
            var options = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            };
            
            var tokenizerData = JsonSerializer.Deserialize<TokenizerData>(jsonContent, options);
            
            if (tokenizerData?.vocab != null)
            {
                _vocab = tokenizerData.vocab;
                _reverseVocab = _vocab.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
                Console.WriteLine($"Vocabulary loaded with {_vocab.Count} tokens");
            }
            else
            {
                // Try to parse as direct vocab dictionary
                var vocabDict = JsonSerializer.Deserialize<Dictionary<string, int>>(jsonContent, options);
                if (vocabDict != null)
                {
                    _vocab = vocabDict;
                    _reverseVocab = _vocab.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
                    Console.WriteLine($"Vocabulary loaded with {_vocab.Count} tokens");
                }
                else
                {
                    throw new InvalidOperationException("Could not parse tokenizer vocabulary");
                }
            }
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error parsing tokenizer JSON: {ex.Message}");
            throw;
        }
    }
    
    private void LoadTokenizerConfig(string configPath)
    {
        Console.WriteLine("Loading tokenizer config...");
        
        try
        {
            string configContent = File.ReadAllText(configPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            };
            
            _tokenizerConfig = JsonSerializer.Deserialize<TokenizerConfig>(configContent, options);
            Console.WriteLine("Tokenizer config loaded successfully");
            
            if (_tokenizerConfig.src_lang != null)
            {
                Console.WriteLine($"Source languages: {string.Join(", ", _tokenizerConfig.src_lang.Take(5))}...");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not load tokenizer config: {ex.Message}");
        }
    }
    
    // Basic tokenization method (simplified - NLLB uses SentencePiece which is more complex)
    public List<int> Tokenize(string text, string srcLang = "eng_Latn")
    {
        if (_vocab == null)
        {
            throw new InvalidOperationException("Tokenizer not loaded");
        }
        
        var tokens = new List<int>();
        
        // Add source language token if available
        string langToken = $"__{srcLang}__";
        if (_vocab.ContainsKey(langToken))
        {
            tokens.Add(_vocab[langToken]);
        }
        
        // Simple word-level tokenization (in practice, NLLB uses subword tokenization)
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var word in words)
        {
            if (_vocab.ContainsKey(word))
            {
                tokens.Add(_vocab[word]);
            }
            else
            {
                // Handle unknown tokens
                string unkToken = _tokenizerConfig?.unk_token ?? "<unk>";
                if (_vocab.ContainsKey(unkToken))
                {
                    tokens.Add(_vocab[unkToken]);
                }
            }
        }
        
        return tokens;
    }
    
    public string Detokenize(List<int> tokenIds)
    {
        if (_reverseVocab == null)
        {
            throw new InvalidOperationException("Tokenizer not loaded");
        }
        
        var tokens = tokenIds
            .Where(id => _reverseVocab.ContainsKey(id))
            .Select(id => _reverseVocab[id])
            .Where(token => !token.StartsWith("__") || !token.EndsWith("__")) // Filter out language tokens
            .ToList();
        
        return string.Join(" ", tokens);
    }
    
    // Method to run inference (basic structure)
    public float[] RunInference(List<int> inputTokens, string targetLang = "fra_Latn")
    {
        if (_session == null)
        {
            throw new InvalidOperationException("Model not loaded");
        }
        
        // Convert tokens to tensor format
        var inputShape = new int[] { 1, inputTokens.Count };
        var inputData = inputTokens.Select(t => (long)t).ToArray();
        var inputTensor = new DenseTensor<long>(new Memory<long>(inputData), inputShape);
        
        // Prepare inputs for the model
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", inputTensor)
        };
        
        // Add attention mask if required
        var attentionMaskData = Enumerable.Repeat(1L, inputTokens.Count).ToArray();
        var attentionMask = new DenseTensor<long>(new Memory<long>(attentionMaskData), inputShape);
        inputs.Add(NamedOnnxValue.CreateFromTensor("attention_mask", attentionMask));
        
        // Run inference
        using var results = _session.Run(inputs);
        
        // Extract output (this will depend on your specific NLLB model structure)
        var output = results.FirstOrDefault()?.AsTensor<float>();
        return output?.ToArray() ?? new float[0];
    }
    
    public void Dispose()
    {
        _session?.Dispose();
    }

}