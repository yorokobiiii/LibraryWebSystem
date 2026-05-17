using LibraryWebSystem.Models;
using LibraryWebSystem.Data; 
using Microsoft.EntityFrameworkCore;

namespace LibraryWebSystem.Services;

public class SearchService : ISearchService
{
    private readonly LibraryContext _context;

    // Внедрение зависимостей через конструктор 
    public SearchService(LibraryContext context) => _context = context;

    public async Task<List<Book>> SearchAsync(string? title, string? author, int? yearFrom, int? yearTo, string? format)
    {
        // Базовый запрос с подгрузкой авторов
        var query = _context.Books
            .Include(b => b.BookAuthors)
            .ThenInclude(ba => ba.Author)
            .AsQueryable();

        // 1. Поиск по названию (частичное совпадение)
        if (!string.IsNullOrWhiteSpace(title))
            query = query.Where(b => b.Title.Contains(title));

        // 2. Поиск по автору
        if (!string.IsNullOrWhiteSpace(author))
            query = query.Where(b => b.BookAuthors.Any(ba => 
                ba.Author.FirstName.Contains(author) || ba.Author.LastName.Contains(author)));

        // 3. Год "от"
        if (yearFrom.HasValue)
            query = query.Where(b => b.YearPublished >= yearFrom.Value);

        // 4. Год "до"
        if (yearTo.HasValue)
            query = query.Where(b => b.YearPublished <= yearTo.Value);

        // 5. Формат файла
        if (!string.IsNullOrWhiteSpace(format))
            query = query.Where(b => b.FileFormat == format);

        return await query.ToListAsync();
    }
}