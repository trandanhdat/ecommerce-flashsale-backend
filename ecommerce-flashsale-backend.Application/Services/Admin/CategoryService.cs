using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using ecommerce_flashsale_backend.Application.Common.Dtos;
using FlashSale.Application.Services.Admin.DTOs;
using FlashSale.Domain.Catalog;
using FlashSale.Domain.SeedWork;

namespace FlashSale.Application.Services.Admin
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMapper _mapper;

        public CategoryService(ICategoryRepository categoryRepository, IMapper mapper)
        {
            _categoryRepository = categoryRepository;
            _mapper = mapper;
        }

        public async Task<PagedResult<CategoryDto>> GetAllAsync(int page, int pageSize, string search, CancellationToken ct = default)
        {
            var categories = await _categoryRepository.GetAllAsync();
            var query = categories.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                // So khớp thường cho search đơn giản
                query = query.Where(c => c.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            var totalItems = query.Count();
            var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var dtoItems = _mapper.Map<System.Collections.Generic.List<CategoryDto>>(items);

            return new PagedResult<CategoryDto>
            {
                Items = dtoItems,
                TotalCount = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<CategoryDto> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null) throw new DomainException("Không tìm thấy danh mục.");

            return _mapper.Map<CategoryDto>(category);
        }

        public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto, CancellationToken ct = default)
        {
            var slug = GenerateSlug(dto.Name);

            if (await _categoryRepository.ExistsBySlugAsync(slug))
            {
                throw new DomainException($"Danh mục có slug '{slug}' đã tồn tại.");
            }

            // Description để trống hoặc bạn có thể update Dto để nhận thêm. Ở đây để chuỗi rỗng theo yêu cầu (chỉ có Name).
            var category = new Category(dto.Name, slug, string.Empty);
            
            await _categoryRepository.AddAsync(category);
            
            return _mapper.Map<CategoryDto>(category);
        }

        public async Task UpdateAsync(Guid id, UpdateCategoryDto dto, CancellationToken ct = default)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null) throw new DomainException("Không tìm thấy danh mục.");

            var slug = GenerateSlug(dto.Name);

            if (category.Slug != slug && await _categoryRepository.ExistsBySlugAsync(slug))
            {
                throw new DomainException($"Danh mục có slug '{slug}' đã tồn tại.");
            }

            category.Update(dto.Name, slug, category.Description);
            
            await _categoryRepository.UpdateAsync(category);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null) throw new DomainException("Không tìm thấy danh mục.");

            // Kiểm tra không cho xoá nếu Category còn Product liên kết
            if (category.Products != null && category.Products.Any())
            {
                throw new DomainException("Không thể xoá danh mục vì vẫn còn sản phẩm liên kết.");
            }

            await _categoryRepository.DeleteAsync(category);
        }

        // Tự sinh Slug từ Name (viết thường, thay dấu cách bằng "-", bỏ dấu tiếng Việt)
        private string GenerateSlug(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;

            var str = name.ToLowerInvariant();
            
            // Xóa dấu tiếng Việt cơ bản
            str = System.Text.RegularExpressions.Regex.Replace(str, "[áàảãạăắằẳẵặâấầẩẫậ]", "a");
            str = System.Text.RegularExpressions.Regex.Replace(str, "[éèẻẽẹêếềểễệ]", "e");
            str = System.Text.RegularExpressions.Regex.Replace(str, "[íìỉĩị]", "i");
            str = System.Text.RegularExpressions.Regex.Replace(str, "[óòỏõọôốồổỗộơớờởỡợ]", "o");
            str = System.Text.RegularExpressions.Regex.Replace(str, "[úùủũụưứừửữự]", "u");
            str = System.Text.RegularExpressions.Regex.Replace(str, "[ýỳỷỹỵ]", "y");
            str = System.Text.RegularExpressions.Regex.Replace(str, "[đ]", "d");
            
            // Xóa các ký tự không phải alphanumeric, thay thế khoảng trắng bằng '-'
            str = System.Text.RegularExpressions.Regex.Replace(str, "[^a-z0-9\\s-]", "");
            str = System.Text.RegularExpressions.Regex.Replace(str, "\\s+", "-");
            str = str.Trim('-');

            return str;
        }
    }
}
