using LibraryWebSystem.Data;
using LibraryWebSystem.Models;
using LibraryWebSystem.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Подключение к БД (SQLite)
builder.Services.AddDbContext<LibraryContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Регистрация сервиса поиска (Dependency Injection)
builder.Services.AddScoped<ISearchService, SearchService>();

// 3. Настройка Blazor Server
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

var app = builder.Build();

// ===== 4. MIDDLEWARE =====

// Раздача статических файлов (CSS, JS, картинки)
app.UseStaticFiles();

// Anti-forgery (обязательно для Blazor форм)
app.UseAntiforgery();

// Blazor Server маршрутизация
app.MapRazorComponents<LibraryWebSystem.Components.App>()
    .AddInteractiveServerRenderMode();

// ===== 5. ИНИЦИАЛИЗАЦИЯ БД И ТЕСТОВЫЕ ДАННЫЕ =====
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibraryContext>();
    db.Database.EnsureCreated();

    if (!db.Books.Any())
    {
        // Книга 1 + Автор
        var b1 = new Book 
        { 
            Title = "Мастер и Маргарита", 
            YearPublished = 1967, 
            Price = 479, 
            FileFormat = "PDF",
            ISBN = "978-5-17-080320-5" 
        };
        var a1 = new Author { FirstName = "Михаил", LastName = "Булгаков" };
        db.BookAuthors.Add(new BookAuthor { Book = b1, Author = a1 });
        db.Books.Add(b1);

        // Книга 2 + Автор
        var b2 = new Book 
        { 
            Title = "Война и мир", 
            YearPublished = 1869, 
            Price = 399, 
            FileFormat = "EPUB",
            ISBN = "978-5-699-12345-6" 
        };
        var a2 = new Author { FirstName = "Лев", LastName = "Толстой" };
        db.BookAuthors.Add(new BookAuthor { Book = b2, Author = a2 });
        db.Books.Add(b2);

        await db.SaveChangesAsync();
    }
}

// ===== 6. API ENDPOINTS =====

// GET /api/books — получить все книги
app.MapGet("/api/books", async (LibraryContext db) =>
{
    var books = await db.Books
        .Include(b => b.BookAuthors)
        .ThenInclude(ba => ba.Author)
        .ToListAsync();
    
    return Results.Json(new 
    { 
        total = books.Count, 
        data = books.Select(b => new 
        { 
            b.BookId, 
            b.Title, 
            b.YearPublished, 
            b.Price, 
            b.FileFormat,
            Authors = b.BookAuthors.Select(ba => ba.Author.FirstName + " " + ba.Author.LastName)
        }) 
    });
});

// GET /api/books/{id} — получить книгу по ID
app.MapGet("/api/books/{id:int}", async (int id, LibraryContext db) =>
{
    var book = await db.Books
        .Include(b => b.BookAuthors)
        .ThenInclude(ba => ba.Author)
        .FirstOrDefaultAsync(b => b.BookId == id);
    
    if (book == null) 
        return Results.NotFound(new { error = "Книга не найдена" });
    
    return Results.Json(new 
    { 
        book.BookId, 
        book.Title, 
        book.YearPublished, 
        book.Price, 
        book.FileFormat,
        Authors = book.BookAuthors.Select(ba => ba.Author.FirstName + " " + ba.Author.LastName)
    });
});

// POST /api/books — добавить новую книгу
app.MapPost("/api/books", async (Book newBook, LibraryContext db) =>
{
    if (string.IsNullOrWhiteSpace(newBook.Title))
        return Results.BadRequest(new { error = "Название книги обязательно" });
    
    db.Books.Add(newBook);
    await db.SaveChangesAsync();
    
    return Results.Created($"/api/books/{newBook.BookId}", newBook);
});

// GET /api/config — получить настройки приложения
app.MapGet("/api/config", (IConfiguration config) =>
{
    return Results.Json(new
    {
        appName = "LibraryWebSystem",
        version = "1.0",
        dbProvider = "SQLite",
        endpoints = new[] { 
            "GET /api/books", 
            "GET /api/books/{id}", 
            "POST /api/books",
            "GET /api/config",
            "GET /search",
            "GET /addbook"
        }
    });
});

// ===== 7. ЗАПУСК ПРИЛОЖЕНИЯ =====
app.Run();