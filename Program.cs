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
    db.Database.Migrate();
    
    if (!db.Books.Any())
    {
        // Список книг для заполнения (Название, Год, Цена, Формат, ISBN, Имя Автора, Фамилия Автора)
        var booksData = new List<(string Title, int Year, decimal Price, string Format, string ISBN, string AuthFirst, string AuthLast)>
        {
            ("Мастер и Маргарита", 1967, 479, "PDF", "978-5-17-080320-5", "Михаил", "Булгаков"),
            ("Война и мир", 1869, 399, "EPUB", "978-5-699-12345-6", "Лев", "Толстой"),
            ("Преступление и наказание", 1866, 449, "FB2", "978-5-17-090456-7", "Фёдор", "Достоевский"),
            ("1984", 1949, 350, "PDF", "978-5-17-100001-1", "Джордж", "Оруэлл"),
            ("Гарри Поттер и философский камень", 1997, 550, "EPUB", "978-5-17-100002-2", "Джоан", "Роулинг"),
            ("Маленький принц", 1943, 300, "PDF", "978-5-699-78901-2", "Антуан", "де Сент-Экзюпери"),
            ("Евгений Онегин", 1833, 250, "FB2", "978-5-389-01234-5", "Александр", "Пушкин"),
            ("Анна Каренина", 1877, 429, "EPUB", "978-5-17-100003-3", "Лев", "Толстой"),
            ("Братья Карамазовы", 1880, 499, "PDF", "978-5-17-100004-4", "Фёдор", "Достоевский"),
            ("Собачье сердце", 1925, 320, "FB2", "978-5-17-100005-5", "Михаил", "Булгаков"),
            ("Герой нашего времени", 1840, 280, "EPUB", "978-5-699-89012-3", "Михаил", "Лермонтов"),
            ("Мёртвые души", 1842, 310, "PDF", "978-5-17-100006-6", "Николай", "Гоголь"),
            ("Отцы и дети", 1862, 290, "FB2", "978-5-389-23456-7", "Иван", "Тургенев"),
            ("Идиот", 1869, 460, "EPUB", "978-5-17-100007-7", "Фёдор", "Достоевский"),
            ("Три товарища", 1936, 410, "PDF", "978-5-17-100008-8", "Эрих Мария", "Ремарк"),
            ("Над пропастью во ржи", 1951, 340, "EPUB", "978-5-17-100009-9", "Джером", "Сэлинджер"),
            ("Три мушкетёра", 1844, 380, "FB2", "978-5-699-90123-4", "Александр", "Дюма"),
            ("Граф Монте-Кристо", 1846, 520, "PDF", "978-5-17-100010-0", "Александр", "Дюма"),
            ("Шерлок Холмс: Собака Баскервилей", 1902, 330, "EPUB", "978-5-389-34567-8", "Артур", "Конан Дойл"),
            ("Дюна", 1965, 590, "PDF", "978-5-17-100011-1", "Фрэнк", "Герберт")
        };

        // Добавляем книги и авторов в базу
        foreach (var item in booksData)
        {
            // 1. Ищем автора или создаем нового
            var author = db.Authors.FirstOrDefault(a => a.FirstName == item.AuthFirst && a.LastName == item.AuthLast);
            if (author == null)
            {
                author = new Author { FirstName = item.AuthFirst, LastName = item.AuthLast };
                db.Authors.Add(author);
                // Сохраняем сразу, чтобы получить ID автора
                await db.SaveChangesAsync(); 
            }

            // 2. Создаем книгу
            var book = new Book
            {
                Title = item.Title,
                YearPublished = item.Year,
                Price = item.Price,
                FileFormat = item.Format,
                ISBN = item.ISBN
            };
            db.Books.Add(book);
            await db.SaveChangesAsync(); // Сохраняем книгу, чтобы получить ID книги

            // 3. Связываем книгу и автора
            db.BookAuthors.Add(new BookAuthor
            {
                BookId = book.BookId,
                AuthorId = author.AuthorId,
                Role = "Автор"
            });
        }

        await db.SaveChangesAsync();
        Console.WriteLine($"База данных заполнена: {booksData.Count} книг.");
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