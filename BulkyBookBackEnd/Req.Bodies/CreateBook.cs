﻿using BulkyBookBackEnd.Data;
using BulkyBookBackEnd.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BulkyBookBackEnd.Req.Bodies
{
    public class CreateBook : IValidatableObject
    {
        [Required]
        public string? CategoryName { get; set; } = default!;

        [Required]
        public string? Title { get; set; } = default!;

        [Required]
        public string? Description { get; set; } = default!;

        [Required]
        public string? Publisher { get; set; } = default!;

        [Required]
        public float? Price { get; set; } = default!;

        [Required]
        public float? Cost { get; set; } = default!;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Price < Cost)
            {
                yield return new ValidationResult("Invalid Pricing");
            }
        }

        [Range(minimum: 0, maximum: int.MaxValue, ErrorMessage = "Must be greater than or equal to {0}")]
        public int? Units { get; set; } = 0;

        public IFormFile? Image { get; set; }

        public async static Task<Book> CreateAsync(BookDbContext db, CreateBook body)
        {
            try
            {
                var book = await db.Books.FirstOrDefaultAsync(b => b.Title == body.Title && b.Publisher == body.Publisher);
                if (book != null)
                {
                    return null;
                }
                var category = await db.Categories.FirstOrDefaultAsync(b => b.Name == body.CategoryName);
                if (category == default(Category))
                {
                    category = new Category()
                    {
                        Name = body.CategoryName
                    };
                    await db.Categories.AddAsync(category);
                    await db.SaveChangesAsync();

                }
                var newBook = new Book()
                {
                    Title = body.Title,
                    Publisher = body.Publisher,
                    Description = body.Description,
                    Units = (int)body.Units,
                    Cost = (float)body.Cost,
                    Price = (float)body.Price,
                    Category = category

                };
                await db.AddAsync(newBook);
                await db.SaveChangesAsync();
                if (body.Image != null)
                {
                    var file = body.Image;
                    var fileName = $"book-{newBook.Id}";
                    var filePath = Path.GetTempFileName();
                    using (var stream = File.Create(filePath))
                    {
                        await file.CopyToAsync(stream);
                        var cloudinary = new CloudinaryClass();
                        var filePathFromServer = $"{filePath}/{fileName}";
                        var imageUrl = await cloudinary.BookImageUpload(
                                            filePathFromServer,
                                            fileName
                                        );
                        book.ImageUrl = imageUrl;
                        book.ImageName = fileName;
                        File.Delete(filePathFromServer);
                    }
                }
                db.Entry(newBook).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return newBook;
            }
            catch (Exception)
            {
                return null;
            }


        }
    }

    
}