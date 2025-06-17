// ***********************************************************************
// Author           : glensouza
// Created          : 01-14-2025
//
// Last Modified By : glensouza
// Last Modified On : 01-16-2025
// ***********************************************************************
// <summary>This plugin is used to search documents for the employer Contoso</summary>
// ***********************************************************************

using System.ComponentModel;
using System.Text.Json.Serialization;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;

namespace Blazor.AI.Web.Plugins;

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// ReSharper disable once ClassNeverInstantiated.Global
public class SearchPlugin(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator, SearchIndexClient indexClient)
{
    [KernelFunction("contoso_search")]
    [Description("Search documents for employer Contoso")]
    public async Task<string> SearchAsync([Description("The users optimized semantic search query")] string query)
    {
        // Convert string query to vector
        Embedding<float> embedding = await embeddingGenerator.GenerateAsync(query);

        // Set the index to use in AI Search, the hard coded "demo" index was established in the SeedData project
        SearchClient searchClient = indexClient.GetSearchClient("demo");

        // Configure request parameters
        VectorizedQuery vectorQuery = new(embedding.Vector);
        vectorQuery.Fields.Add("contentVector"); // name of the vector field from Azure AI Search schema established in the SeedData project 

        // Perform search request
        SearchOptions searchOptions = new() { VectorSearch = new VectorSearchOptions { Queries = { vectorQuery } } };
        Response<SearchResults<RAGSearchDocument>> response = await searchClient.SearchAsync<RAGSearchDocument>(searchOptions);

        // Collect search results
        await foreach (SearchResult<RAGSearchDocument> result in response.Value.GetResultsAsync())
        {
            return result.Document.Content; // Return text from first result
        }

        return string.Empty;
    }

    //This schema comes from the Azure AI Search model established in the SeedData project
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class RAGSearchDocument
    {
        [JsonPropertyName("title")]
        public string Title { get; init; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; init; } = string.Empty;
    }
}
