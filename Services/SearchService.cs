using LibraryWebSystem.Models;
using Microsoft.EntityFrameworkCore;
using LibraryWebSystem.Data;

namespace LibraryWebSystem.Services;

public class SearchService : ISearchService
{
    private readonly LibraryContext _context;

    public SearchService(LibraryContext context) => _context = context;

    public async Task<List<Book>> SearchAsync(string? title, string? author, int? yearFrom, int? yearTo, string? format)
    {

        var query = _context.Books
            .Include(b => b.BookAuthors)
            .ThenInclude(ba => ba.Author)
            .AsQueryable();

        // 1. Поиск по названию (частичное совпадение)
        if (!string.IsNullOrWhiteSpace(title))
            query = query.Where(b => b.Title.Contains(title));

        // 2. Поиск по автору (ищем в Имени или Фамилии)
        if (!string.IsNullOrWhiteSpace(author))
            query = query.Where(b => b.BookAuthors.Any(ba => 
                ba.Author.FirstName.Contains(author) || 
                ba.Author.LastName.Contains(author)));

        // 3. Фильтр по году "от"
        if (yearFrom.HasValue)
            query = query.Where(b => b.YearPublished >= yearFrom.Value);

        // 4. Фильтр по году "до"
        if (yearTo.HasValue)
            query = query.Where(b => b.YearPublished <= yearTo.Value);

        // 5. Фильтр по формату
        if (!string.IsNullOrWhiteSpace(format))
            query = query.Where(b => b.FileFormat == format);

        return await query.ToListAsync();
    }
}