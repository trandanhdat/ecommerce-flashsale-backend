using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FlashSale.Domain.SeedWork;
using FlashSale.Infrastructure.Persistence.Interceptors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ecommerce_flashsale_backend.Infrastructure.UnitTests
{
    // 1. Tạo một cái Event giả để test
    public class DummyDomainEvent : IDomainEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    // 2. Tạo một cái Entity giả kế thừa Entity của hệ thống để nhét Event vào
    public class DummyEntity : Entity
    {
        public string Name { get; set; } = "Test";
        
        public void DoSomething()
        {
            AddDomainEvent(new DummyDomainEvent());
        }
    }

    // 3. Tạo một DbContext giả lập chỉ dùng cho việc chạy Test
    public class TestDbContext : DbContext
    {
        private readonly DispatchDomainEventsInterceptor _interceptor;

        public TestDbContext(
            DbContextOptions<TestDbContext> options, 
            DispatchDomainEventsInterceptor interceptor) : base(options)
        {
            _interceptor = interceptor;
        }

        public DbSet<DummyEntity> DummyEntities => Set<DummyEntity>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Nhúng cái Interceptor vào DbContext giống hệt như cách làm ở Program.cs
            optionsBuilder.AddInterceptors(_interceptor);
            base.OnConfiguring(optionsBuilder);
        }
    }

    public class DispatchDomainEventsInterceptorTests
    {
        private readonly Mock<IMediator> _mockMediator;
        private readonly DispatchDomainEventsInterceptor _interceptor;

        public DispatchDomainEventsInterceptorTests()
        {
            _mockMediator = new Mock<IMediator>();
            _interceptor = new DispatchDomainEventsInterceptor(_mockMediator.Object);
        }

        private TestDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Mỗi lần chạy tạo 1 DB ảo khác nhau
                .Options;

            return new TestDbContext(options, _interceptor);
        }

        [Fact]
        public async Task SavingChangesAsync_ShouldPublishDomainEvents_AndClearThemAfterwards()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            
            var entity = new DummyEntity();
            entity.DoSomething(); // Sinh ra 1 cái DummyDomainEvent nằm trong bụng của Entity
            
            // Ban đầu entity đang ôm 1 cái event
            entity.DomainEvents.Should().HaveCount(1);

            dbContext.DummyEntities.Add(entity);

            // Act
            // Khi gọi SaveChangesAsync, Interceptor sẽ chui vào chạy chặn ngang
            await dbContext.SaveChangesAsync();

            // Assert
            // 1. Phải đảm bảo cái Event đó đã được gửi qua Bưu điện (IMediator.Publish)
            _mockMediator.Verify(m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()), Times.Once);

            // 2. Phải đảm bảo sau khi gửi xong, bụng của Entity đã được làm sạch (ClearDomainEvents)
            // Nếu không làm sạch, lần SaveChanges tiếp theo nó lại gửi email 2 lần!
            entity.DomainEvents.Should().BeEmpty();
        }

        [Fact]
        public async Task SavingChangesAsync_ShouldDoNothing_WhenNoDomainEventsExist()
        {
            // Arrange
            using var dbContext = CreateDbContext();
            
            var entity = new DummyEntity();
            // KHÔNG gọi DoSomething(), tức là không có event nào
            
            dbContext.DummyEntities.Add(entity);

            // Act
            await dbContext.SaveChangesAsync();

            // Assert
            // Bưu điện phải đứng im không được gửi gì cả
            _mockMediator.Verify(m => m.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
