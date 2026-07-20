using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class CherishCategoryListResModel
    {
        public bool isError { get; set; }
        public string message { get; set; } = string.Empty;
        public string default_category { get; set; } = string.Empty;
        public List<CategoryList> data { get; set; } = new List<CategoryList>();
    }
    public class CategoryList
    {
        public string category_id { get; set; } = string.Empty;
        public string category_Name { get; set; } = string.Empty;
    }

    public class CherishCategory
    {
        public string name { get; set; } = string.Empty;
        public string message { get; set; } = string.Empty;
        public string channel_name { get; set; } = string.Empty;
    }
}
