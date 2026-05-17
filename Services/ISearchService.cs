using LibraryWebSystem.Models;

namespace LibraryWebSystem.Services;

public interface ISearchService
{
    Task<List<Book>> SearchAsync(string? title, string? author, int? yearFrom, int? yearTo, string? format);
}