using Refit;
using System.Collections.Generic;
using System.Threading.Tasks;
using JanitorAspNet.Models;

namespace JanitorAspNet.Clients
{
    /// <summary>
    /// Jellyfin API client for media server operations
    /// </summary>
    public interface IJellyfinClient
    {
        // Library Management
        [Get("/Library/VirtualFolders")]
        Task<List<VirtualFolder>> GetVirtualFoldersAsync();

        [Post("/Library/VirtualFolders")]
        Task<VirtualFolder> CreateVirtualFolderAsync([Body] CreateVirtualFolderRequest request);

        [Post("/Library/VirtualFolders/Paths")]
        Task AddPathToLibraryAsync([Body] AddPathRequest request);

        // Item Management
        [Get("/Items")]
        Task<ItemQueryResult> GetItemsAsync([Query] string? searchTerm = null, [Query] string? includeItemTypes = null);

        [Delete("/Items/{itemId}")]
        Task DeleteItemAsync(string itemId);

        [Get("/Items/{itemId}")]
        Task<BaseItemDto> GetItemAsync(string itemId);

        // Collection Management
        [Post("/Collections")]
        Task<CollectionCreationResult> CreateCollectionAsync([Body] CreateCollectionRequest request);

        [Post("/Collections/{collectionId}/Items")]
        Task AddToCollectionAsync(string collectionId, [Query] string ids);

        // Authentication
        [Post("/Users/AuthenticateByName")]
        Task<AuthenticationResult> AuthenticateAsync([Body] AuthenticateUserByName request);

        // Library Refresh and Scanning
        [Post("/Library/Refresh")]
        Task RefreshLibraryAsync();

        [Post("/Library/VirtualFolders/{name}/Refresh")]
        Task RefreshVirtualFolderAsync(string name);

        // User Management
        [Get("/Users")]
        Task<List<UserDto>> GetUsersAsync();

        [Get("/Users/{userId}/Items")]
        Task<ItemQueryResult> GetUserItemsAsync(string userId, [Query] string? includeItemTypes = null);
    }

    /// <summary>
    /// Emby API client for media server operations
    /// </summary>
    public interface IEmbyClient
    {
        // Library Management
        [Get("/Library/VirtualFolders")]
        Task<List<VirtualFolder>> GetVirtualFoldersAsync();

        [Post("/Library/VirtualFolders")]
        Task<VirtualFolder> CreateVirtualFolderAsync([Body] CreateVirtualFolderRequest request);

        // Item Management
        [Get("/Items")]
        Task<ItemQueryResult> GetItemsAsync([Query] string? searchTerm = null, [Query] string? includeItemTypes = null);

        [Delete("/Items/{itemId}")]
        Task DeleteItemAsync(string itemId);

        [Get("/Items/{itemId}")]
        Task<BaseItemDto> GetItemAsync(string itemId);

        // Collection Management
        [Post("/Collections")]
        Task<CollectionCreationResult> CreateCollectionAsync([Body] CreateCollectionRequest request);

        [Post("/Collections/{collectionId}/Items")]
        Task AddToCollectionAsync(string collectionId, [Query] string ids);

        // Authentication
        [Post("/Users/AuthenticateByName")]
        Task<AuthenticationResult> AuthenticateAsync([Body] AuthenticateUserByName request);
    }

    /// <summary>
    /// Plex API client for media server operations
    /// </summary>
    public interface IPlexClient
    {
        // Library Management
        [Get("/library/sections")]
        Task<PlexLibraryResponse> GetLibrarySectionsAsync();

        [Get("/library/sections/{sectionId}/all")]
        Task<PlexItemsResponse> GetLibraryItemsAsync(string sectionId);

        // Item Management
        [Delete("/library/metadata/{ratingKey}")]
        Task DeleteItemAsync(string ratingKey);

        [Get("/library/metadata/{ratingKey}")]
        Task<PlexItemResponse> GetItemAsync(string ratingKey);

        // Collection Management
        [Post("/library/collections")]
        Task<PlexCollectionResponse> CreateCollectionAsync([Body] PlexCreateCollectionRequest request);

        [Put("/library/collections/{collectionId}/items")]
        Task AddToCollectionAsync(string collectionId, [Query] string ratingKeys);

        // Authentication
        [Post("/users/sign_in.xml")]
        Task<PlexAuthResponse> AuthenticateAsync([Body] PlexAuthRequest request);
    }
}

// Supporting model classes for Jellyfin/Emby
namespace JanitorAspNet.Models
{
    public record CreateVirtualFolderRequest
    {
        public string Name { get; init; } = "";
        public string CollectionType { get; init; } = "";
        public List<string> Paths { get; init; } = new();
    }

    public record AddPathRequest
    {
        public string Name { get; init; } = "";
        public string Path { get; init; } = "";
    }

    public record ItemQueryResult
    {
        public List<BaseItemDto> Items { get; init; } = new();
        public int TotalRecordCount { get; init; }
        public int StartIndex { get; init; }
    }

    public record BaseItemDto
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string Type { get; init; } = "";
        public string Path { get; init; } = "";
        public DateTime DateCreated { get; init; }
        public DateTime? DateLastMediaAdded { get; init; }
        public List<string> Genres { get; init; } = new();
        public int? ProductionYear { get; init; }
        public string? ImdbId { get; init; }
        public int? TmdbId { get; init; }
        public int? TvdbId { get; init; }
    }

    public record CollectionCreationResult
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
    }

    public record CreateCollectionRequest
    {
        public string Name { get; init; } = "";
        public List<string> ItemIds { get; init; } = new();
        public bool IsLocked { get; init; }
    }

    public record AuthenticationResult
    {
        public UserDto User { get; init; } = new();
        public string AccessToken { get; init; } = "";
        public string ServerId { get; init; } = "";
    }

    public record AuthenticateUserByName
    {
        public string Username { get; init; } = "";
        public string Pw { get; init; } = "";
    }

    public record UserDto
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public bool IsAdministrator { get; init; }
        public bool IsHidden { get; init; }
        public bool IsDisabled { get; init; }
    }

    // Plex-specific models
    public record PlexLibraryResponse
    {
        public List<PlexLibrarySection> Directory { get; init; } = new();
    }

    public record PlexLibrarySection
    {
        public string Key { get; init; } = "";
        public string Title { get; init; } = "";
        public string Type { get; init; } = "";
    }

    public record PlexItemsResponse
    {
        public List<PlexItem> Metadata { get; init; } = new();
    }

    public record PlexItem
    {
        public string RatingKey { get; init; } = "";
        public string Title { get; init; } = "";
        public string Type { get; init; } = "";
        public DateTime AddedAt { get; init; }
        public string? Guid { get; init; }
    }

    public record PlexItemResponse
    {
        public List<PlexItem> Metadata { get; init; } = new();
    }

    public record PlexCollectionResponse
    {
        public string RatingKey { get; init; } = "";
        public string Title { get; init; } = "";
    }

    public record PlexCreateCollectionRequest
    {
        public string Type { get; init; } = "";
        public string Title { get; init; } = "";
        public string Smart { get; init; } = "";
        public string SectionId { get; init; } = "";
    }

    public record PlexAuthResponse
    {
        public PlexUser User { get; init; } = new();
        public string AuthToken { get; init; } = "";
    }

    public record PlexUser
    {
        public string Id { get; init; } = "";
        public string Username { get; init; } = "";
        public string Title { get; init; } = "";
    }

    public record PlexAuthRequest
    {
        public string Username { get; init; } = "";
        public string Password { get; init; } = "";
    }
}
