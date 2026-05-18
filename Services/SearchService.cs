using LibraryWebSystem.Models;
using LibraryWebSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryWebSystem.Services;

public class SearchService : ISearchService
{
    private readonly LibraryContext _context;

    public SearchService(LibraryContext context) => _context = context;

    public async Task<List<Book>> SearchAsync(string? title, string? author, int? yearFrom, int? yearTo, string? format)
    {
        // 1. Сначала загружаем ВСЕ книги с авторами из базы
        var allBooks = await _context.Books
            .Include(b => b.BookAuthors)
            .ThenInclude(ba => ba.Author)
            .ToListAsync();

        // 2. Фильтруем уже в памяти (C# игнорирует регистр через StringComparison)
        var results = allBooks.Where(b =>
        {
            // Фильтр по названию (регистронезависимый)
            bool titleMatch = string.IsNullOrWhiteSpace(title) || 
                b.Title.Contains(title, StringComparison.OrdinalIgnoreCase);

            // Фильтр по автору (регистронезависимый)
            bool authorMatch = string.IsNullOrWhiteSpace(author) ||
                b.BookAuthors.Any(ba => 
                    ba.Author.FirstName.Contains(author, StringComparison.OrdinalIgnoreCase) ||
                    ba.Author.LastName.Contains(author, StringComparison.OrdinalIgnoreCase));

            // Фильтр по году "от"
            bool yearFromMatch = !yearFrom.HasValue || b.YearPublished >= yearFrom.Value;

            // Фильтр по году "до"
            bool yearToMatch = !yearTo.HasValue || b.YearPublished <= yearTo.Value;

            // Фильтр по формату (регистронезависимый)
            bool formatMatch = string.IsNullOrWhiteSpace(format) ||
                b.FileFormat.Equals(format, StringComparison.OrdinalIgnoreCase);

            return titleMatch && authorMatch && yearFromMatch && yearToMatch && formatMatch;
        }).ToList();

        return results;
    }
}