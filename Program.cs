using LibraryWebSystem.Data;
using LibraryWebSystem.Models;
using LibraryWebSystem.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

//  Подключение к БД (SQLite)
builder.Services.AddDbContext<LibraryContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

//  Регистрация сервиса поиска (Dependency Injection)
builder.Services.AddScoped<ISearchService, SearchService>();

//  Настройка Blazor Server
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

var app = builder.Build();

// Инициализация БД и тестовые данные 
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibraryContext>();
    db.Database.EnsureCreated();

    if (!db.Books.Any())
    {
        var b1 = new Book { Title = "Мастер и Маргарита", YearPublished = 1967, Price = 479, FileFormat = "PDF" };
        var a1 = new Author { FirstName = "Михаил", LastName = "Булгаков" };
        db.BookAuthors.Add(new BookAuthor { Book = b1, Author = a1 });
        db.Books.Add(b1);

        var b2 = new Book { Title = "Война и мир", YearPublished = 1869, Price = 399, FileFormat = "EPUB" };
        var a2 = new Author { FirstName = "Лев", LastName = "Толстой" };
        db.BookAuthors.Add(new BookAuthor { Book = b2, Author = a2 });
        db.Books.Add(b2);

        await db.SaveChangesAsync();
    }
}


app.MapRazorComponents<LibraryWebSystem.Components.App>()
    .AddInteractiveServerRenderMode();


app.UseAntiforgery();



// GET /api/books - получить все книги
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

// GET /api/books/{id} - получить книгу по ID
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

// POST /api/books - добавить новую книгу
app.MapPost("/api/books", async (Book newBook, LibraryContext db) =>
{
    if (string.IsNullOrWhiteSpace(newBook.Title))
        return Results.BadRequest(new { error = "Название книги обязательно" });
    
    db.Books.Add(newBook);
    await db.SaveChangesAsync();
    
    return Results.Created($"/api/books/{newBook.BookId}", newBook);
});

// GET /api/config - получить настройки приложения
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
            "GET /api/readers",
            "GET /api/config",
            "GET /search"
        }
    });
});

// Стартовая страница
app.MapGet("/", () => """

    Доступные эндпоинты:
    • GET  /api/books          - список книг
    • GET  /api/books/{id}     - книга по ID
    • POST /api/books          - добавить книгу
    • GET  /api/readers        - список читателей
    • GET  /api/config         - настройки приложения
    
    Веб-интерфейс:
    • /search                  - гибкий поиск книг 

    """);

app.Run();