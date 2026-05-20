using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryWebSystem.Models;

[Table("Books")]
public class Book
{
    [Key] public int BookId { get; set; }
    [Required, StringLength(200)] public string Title { get; set; } = string.Empty;
    [StringLength(50)] public string? ISBN { get; set; }
    [Range(1000, 9999)] public int YearPublished { get; set; }
    [StringLength(20)] public string FileFormat { get; set; } = "PDF";
    [Column(TypeName = "decimal(10,2)")] public decimal Price { get; set; } = 0.00m;

    // Связь N:N
    public  ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
}

