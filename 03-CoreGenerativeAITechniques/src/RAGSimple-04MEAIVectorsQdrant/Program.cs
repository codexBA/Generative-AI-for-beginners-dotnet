using System.Diagnostics;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Qdrant.Client;

var vectorStore = new QdrantVectorStore(new QdrantClient("localhost"), true);

// get movie list
var movies = vectorStore.GetCollection<ulong, MovieVector<ulong>>("movies");
await movies.EnsureCollectionExistsAsync();
var movieData = MovieFactory<ulong>.GetMovieVectorList();

// get embeddings generator and generate embeddings for movies
IEmbeddingGenerator<string, Embedding<float>> generator =
    new OllamaEmbeddingGenerator(new Uri("http://localhost:11434/"), "all-minilm");
foreach (var movie in movieData)
{
    movie.Vector = await generator.GenerateVectorAsync(movie.Description);
    await movies.UpsertAsync(movie);
}

// perform the search
var query = "movie with keanu acting";
var queryEmbedding = await generator.GenerateVectorAsync(query);

Console.WriteLine($"Results for Query: {query}: ");
await foreach (var resultItem in movies.SearchAsync(queryEmbedding, top: 2))
{
    Console.WriteLine($"Title: {resultItem.Record.Title}");
    Console.WriteLine($"Description: {resultItem.Record.Description}");
    Console.WriteLine($"Score: {resultItem.Score}");
    Console.WriteLine();
}



var queryWithConfidence = "western movie with james bond";
var resultsWithConfidence = await SearchMoviesWithConfidenceAsync(queryWithConfidence, 0.6f);

if (!resultsWithConfidence.Any())
{
    Console.WriteLine("No confident matches found. Your dataset might not contain relevant movies.");
    Console.WriteLine("Try a broader search or check your movie collection.");
}


async Task<List<MovieResult>> SearchMoviesWithConfidenceAsync(string queryWithConfidence, float minScore = 0.6f)
{
    Console.WriteLine("----------------------------------");
    Console.WriteLine("Running Search with CONFIDENCE!");
    Console.WriteLine("----------------------------------");

    Console.WriteLine($"\r\nResults for Query: {queryWithConfidence}: ");
    Console.WriteLine("..............");
    var queryEmbedding = await generator.GenerateVectorAsync(queryWithConfidence);
    var results = new List<MovieResult>();
    
    await foreach (var result in movies.SearchAsync(queryEmbedding, top: 10))
    {
        Console.WriteLine($"Candidate: {result.Record.Title} - Score: {result.Score:F3}");
        
        if (result.Score >= minScore)
        {
            results.Add(new MovieResult 
            { 
                Movie = result.Record, 
                Score = result.Score,
                Confidence = "High"
            });
        }
        else if (result.Score >= 0.4f)
        {
            results.Add(new MovieResult 
            { 
                Movie = result.Record, 
                Score = result.Score,
                Confidence = "Low - might not be relevant"
            });
        }
        // Below 0.4 = don't show to user
    }
    
    return results;
}

public class MovieResult
{
    public MovieVector<ulong> Movie { get; set; }
    public double? Score { get; set; }
    public string Confidence {
        get;
        set;
    }
}