using System.Collections.Generic;

namespace CyberTech.Models
{
    public class ProductCompareViewModel
    {
        public List<Product> Products { get; set; } = new List<Product>();
        public string ComparisonType { get; set; } // "laptop", "mouse", "keyboard", etc.
        public ProductAnalysis Analysis { get; set; }
        public List<Product> SuggestedProducts { get; set; } = new List<Product>();
        public Dictionary<string, List<ComparisonCriteria>> CriteriaByCategory { get; set; } = new Dictionary<string, List<ComparisonCriteria>>();
        public Dictionary<string, List<TechnicalSpec>> TechnicalSpecs { get; set; } = new Dictionary<string, List<TechnicalSpec>>();
    }

    public class TechnicalSpec
    {
        public string Key { get; set; }
        public string DisplayName { get; set; }
        public string Unit { get; set; }
        public bool HigherIsBetter { get; set; } = true;
        public List<SpecValue> Values { get; set; } = new List<SpecValue>();
    }

    public class SpecValue
    {
        public int ProductId { get; set; }
        public string RawValue { get; set; }
        public double NumericValue { get; set; }
        public string HighlightClass { get; set; } // "best", "worst", "equal"
    }

    public class ProductAnalysis
    {
        public int RecommendedProductId { get; set; }
        public string RecommendedProductName { get; set; }
        public string RecommendedProductImage { get; set; }
        public List<string> Reasons { get; set; } = new List<string>();
        public Dictionary<string, decimal> OverallScores { get; set; } = new Dictionary<string, decimal>();
    }

    public class ComparisonCriteria
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public decimal Weight { get; set; } // Trọng số cho việc tính điểm
        public decimal MaxScore { get; set; } = 10;
        public string Unit { get; set; } = "";
        public string ValueType { get; set; } = "numeric"; // numeric, boolean, text
    }

    public class ProductComparisonData
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductImage { get; set; }
        public decimal Price { get; set; }
        public Dictionary<string, object> Specifications { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, decimal> Scores { get; set; } = new Dictionary<string, decimal>();
        public decimal OverallScore { get; set; }
    }
} 