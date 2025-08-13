// Import necessary libraries for AI embeddings and in-memory vector storage
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Connectors.InMemory;

// Create an in-memory vector store to hold our movie data with embeddings
// This is a temporary storage that exists only while the program runs
var vectorStore = new InMemoryVectorStore();

// === STEP 1: SET UP MOVIE COLLECTION ===
// Get a collection specifically for storing movies with integer IDs and MovieVector data
var movies = vectorStore.GetCollection<int, MovieVector<int>>("movies");

// Ensure the collection exists in the vector store (creates it if it doesn't exist)
await movies.EnsureCollectionExistsAsync();

// Load our sample movie data from a factory class
// This gives us a list of movies with titles and descriptions but no embeddings yet
var movieData = MovieFactory<int>.GetMovieVectorList();

// === STEP 2: GENERATE EMBEDDINGS FOR MOVIES ===
// Create an embedding generator using Ollama (a local AI model server)
// Embeddings convert text into numerical vectors that represent semantic meaning
IEmbeddingGenerator<string, Embedding<float>> generator =
    new OllamaEmbeddingGenerator(new Uri("http://localhost:11434/"), "all-minilm"); // This model is proven to work well with embeddings and is very fast
    //new OllamaEmbeddingGenerator(new Uri("http://localhost:11434/"), "llama3.2:1b"); // Alternative model - returns wrong results, not as fast as all-minilm

// Loop through each movie and generate embeddings for their descriptions
foreach (var movie in movieData)
{
    // Generate a vector representation of the movie's description
    // This converts the text description into numbers that capture its meaning
    movie.Vector = await generator.GenerateVectorAsync(movie.Description);
    
    // Store the movie (now with its embedding vector) in our vector database
    // Upsert means "insert or update" - adds new or updates existing records
    await movies.UpsertAsync(movie);
}

// === STEP 3: PERFORM SEMANTIC SEARCH ===
// Define what we're looking for - this is our search query
var query = "Find a movie with Leonardo di caprio in it. If not sure don't return any";

// Convert our search query into the same vector format as our movie descriptions
// This allows us to compare the query against our stored movie embeddings
var queryEmbedding = await generator.GenerateVectorAsync(query);

// Display what we're searching for
Console.WriteLine("Query to the AI: " + query);

// Search through our movie collection for the most similar matches
// 'top: 2' means we want the 2 best matches
await foreach (var resultItem in movies.SearchAsync(queryEmbedding, top: 2))
{
    // Filter out results with low similarity scores (less than 0.3)
    // Score represents how similar the movie is to our query (higher = more similar)
    if(resultItem.Score < 0.3)
        continue;
    
    // Display the matching movie information
    Console.WriteLine($"Title: {resultItem.Record.Title}");
    Console.WriteLine($"Description: {resultItem.Record.Description}");
    Console.WriteLine($"Score: {resultItem.Score}"); // Similarity score (0-1, where 1 is perfect match)
    Console.WriteLine(); // Add blank line for readability
}