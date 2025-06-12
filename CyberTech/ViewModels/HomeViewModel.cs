using CyberTech.Models;
using System.Collections.Generic;

namespace CyberTech.ViewModels
{
    public class HomeViewModel
    {
        public List<Product> FeaturedProducts { get; set; } = new List<Product>();
        public List<Product> NewProducts { get; set; } = new List<Product>();
    }
} 