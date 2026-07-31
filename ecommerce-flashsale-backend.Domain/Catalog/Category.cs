using System;
using System.Collections.Generic;
using FlashSale.Domain.SeedWork;

namespace FlashSale.Domain.Catalog
{
    public class Category : AggregateRoot
    {
        public string Name { get; private set; }
        public string Slug { get; private set; }
        public string Description { get; private set; }
        public bool IsActive { get; private set; }

        public ICollection<Product> Products { get; private set; } = new List<Product>();

        protected Category() { }

        public Category(string name, string slug, string description)
        {
            Id = Guid.NewGuid();
            Name = name;
            Slug = slug;
            Description = description;
            IsActive = true;
        }

        public void Update(string name, string slug, string description)
        {
            Name = name;
            Slug = slug;
            Description = description;
        }

        public void Deactivate()
        {
            IsActive = false;
        }
    }
}
