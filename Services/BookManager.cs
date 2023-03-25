using AutoMapper;
using Entities.DataTransferObjects;
using Entities.Exceptions;
using Entities.LinkModels;
using Entities.Models;
using Entities.RequestFeatures;
using Repositories.Contracts;
using Services.Contracts;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class BookManager : IBookService
    {
        private readonly ICategoryService _categoryService;
        private readonly IRepositoryManager _manager; // kitap ekleme silme arama güncelleme gibi işlemlerin yapılacgı yerdir.
        private readonly ILoggerService _logger; // hata ayıklama, bilgi, uyarı gibi mesajları döndürmemizi sağlayan yerdir.
        private readonly IMapper _mapper; // tür dönüşümlerini yapacagımız yerdir.
        private readonly IBookLinks _bookLinks; // linkleme işlemlerini üstlenen yerdir.

        public BookManager(IRepositoryManager manager,
            ILoggerService logger,
            IMapper mapper, IBookLinks bookLinks, ICategoryService categoryService)
        {
            _manager = manager; //depency İnjection ile ilgili interface üzerinden rahatcana istenilen fonksiyona erişebiliyoruz
            _logger = logger;
            _mapper = mapper;
            _bookLinks = bookLinks;
            _categoryService = categoryService;
        }
        // dto title ve price döndürüyor.
        public async Task<BookDto> CreateOneBookAsync(BookDtoForInsertion bookDto)
        {
            var category = await _categoryService
                .GetOneCategoryByIdAsync(bookDto.CategoryId, false);

            var entity = _mapper.Map<Book>(bookDto); // dtoyu book nesnesine dönüştürüyor. sonrasında özellikler entitye atar
            
            _manager.Book.CreateOneBook(entity); // manager üzerinden create fonksiyonuna ulaşıp entity özellikleri ile veri tabanına ekleme yapar.
            await _manager.SaveAsync(); // veri tabanına eklenen özellikler kaydedilir.
            return _mapper.Map<BookDto>(entity); // sonrasında entity dto ya dönüşür. sonuç olarak bize title ve price döner.
        }

        public async Task DeleteOneBookAsync(int id, bool trackChanges)
        {
            var entity = await GetOneBookByIdAndCheckExists(id, trackChanges);// ilgili id'ye sahip olan kitap varsa kontrol edilir.
            _manager.Book.DeleteOneBook(entity); // varsa kitap burada silinir
            await _manager.SaveAsync();// kaydetme işlemi yapılır.
        }

        public async Task<(LinkResponse linkResponse, MetaData metaData)>
            GetAllBooksAsync(LinkParameters linkParameters,
            bool trackChanges)
        {
            if (!linkParameters.BookParameters.ValidPriceRange)
                throw new PriceOutofRangeBadRequestException();

            var booksWithMetaData = await _manager
                .Book
                .GetAllBooksAsync(linkParameters.BookParameters, trackChanges);

            var booksDto = _mapper.Map<IEnumerable<BookDto>>(booksWithMetaData);

            var links = _bookLinks.TryGenerateLinks(booksDto,
                linkParameters.BookParameters.Fields,
                linkParameters.HttpContext);
            return (linkResponse: links, metaData: booksWithMetaData.MetaData);
        }

        public async Task<List<Book>> GetAllBooksAsync(bool trackChanges)
        {
            var books =  await _manager.Book.GetAllBooksAsync(trackChanges);
            return books;
        }

        public async Task<IEnumerable<Book>> GetAllBooksWithDetailsAsync(bool trackChanges)
        {
            return await _manager
                .Book
                .GetAllBooksWithDetailsAsync(trackChanges);
        }

        public async Task<BookDto> GetOneBookByIdAsync(int id, bool trackChanges)
        {
            var book = await GetOneBookByIdAndCheckExists(id, trackChanges);

            if (book is null)
                throw new BookNotFoundException(id);
            return _mapper.Map<BookDto>(book);
        }

        public async Task<(BookDtoForUpdate bookDtoForUpdate, Book book)>
            GetOneBookForPatchAsync(int id, bool trackChanges)
        {
            var book = await GetOneBookByIdAndCheckExists(id, trackChanges);
            var bookDtoForUpdate = _mapper.Map<BookDtoForUpdate>(book);
            return (bookDtoForUpdate, book);
        }

        public async Task SaveChangesForPatchAsync(BookDtoForUpdate bookDtoForUpdate, Book book)
        {
            _mapper.Map(bookDtoForUpdate, book);
            await _manager.SaveAsync();
        }

        public async Task UpdateOneBookAsync(int id,
            BookDtoForUpdate bookDto,
            bool trackChanges)
        {
            var entity = await GetOneBookByIdAndCheckExists(id, trackChanges);
            entity = _mapper.Map<Book>(bookDto);
            _manager.Book.Update(entity);
            await _manager.SaveAsync();
        }

        private async Task<Book> GetOneBookByIdAndCheckExists(int id, bool trackChanges)
        {
            // check entity 
            var entity = await _manager.Book.GetOneBookByIdAsync(id, trackChanges);

            if (entity is null)
                throw new BookNotFoundException(id);

            return entity;
        }
    }
}