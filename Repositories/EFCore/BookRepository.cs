using Entities.Models;
using Entities.RequestFeatures;
using Microsoft.EntityFrameworkCore;
using Repositories.Contracts;
using Repositories.EFCore.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.EFCore
{
    public sealed class BookRepository : RepositoryBase<Book>, IBookRepository
    {
        public BookRepository(RepositoryContext context) : base(context)
        {

        }

        public void CreateOneBook(Book book) => Create(book); // create fonksiyonu çağrılarak alınan özellikleri veri tabanına kaydeder.
        public void DeleteOneBook(Book book) => Delete(book);// delete ile siler
        public async Task<PagedList<Book>> GetAllBooksAsync(BookParameters bookParameters,
            bool trackChanges)// trackchanges izlelip izlenmeme durumunu ifade eder.
        {
            var books = await FindAll(trackChanges)
                .FilterBooks(bookParameters.MinPrice, bookParameters.MaxPrice)
                .Search(bookParameters.SearchTerm)
                .Sort(bookParameters.OrderBy)
                .ToListAsync();

            return PagedList<Book>
                .ToPagedList(books,
                bookParameters.PageNumber,
                bookParameters.PageSize);
        }

        public async Task<List<Book>> GetAllBooksAsync(bool trackChanges)
        {
            return await FindAll(trackChanges)
                .OrderBy(b => b.Id)
                .ToListAsync();
        }

        public async Task<IEnumerable<Book>> GetAllBooksWithDetailsAsync(bool trackChanges)
        {
            return await _context
                .Books
                .Include(b => b.Category)
                .OrderBy(b => b.Id)
                .ToListAsync();
        }

        public async Task<Book> GetOneBookByIdAsync(int id, bool trackChanges) =>
            await FindByCondition(b => b.Id.Equals(id), trackChanges)
            .SingleOrDefaultAsync();
        public void UpdateOneBook(Book book) => Update(book);
    }
}