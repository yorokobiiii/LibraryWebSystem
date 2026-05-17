using System.ComponentModel.DataAnnotations;

namespace LibraryWebSystem.Models;

// Промежуточная таблица для связи N:N
public class BookAuthor
{
    [Key] public int Id { get; set; }
    public int BookId { get; set; }
    public int AuthorId { get; set; }

    [StringLength(50)]
    public string Role { get; set; } = "Автор";

    public  Book Book { get; set; } = null!;
    public  Author Author { get; set; } = null!;
}