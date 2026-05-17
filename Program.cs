using LibraryWebSystem.Data;
using LibraryWebSystem.Models;
using LibraryWebSystem.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ===== 1. ПОДКЛЮЧЕНИЕ К БД (SQLite) =====
builder.Services.AddDbContext<LibraryContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ===== 2. РЕГИСТРАЦИЯ СЕРВИСОВ =====
builder.Services.AddScoped<ISearchService, SearchService>();

// ===== 3. НАСТРОЙКА BLAZOR SERVER =====
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

var app = builder.Build();

// ===== 4. ИНИЦИАЛИЗАЦИЯ БД =====
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

// ===== 5. MIDDLEWARE =====
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// ===== 6. МАРШРУТИЗАЦИЯ BLAZOR =====
app.MapRazorComponents<LibraryWebSystem.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();