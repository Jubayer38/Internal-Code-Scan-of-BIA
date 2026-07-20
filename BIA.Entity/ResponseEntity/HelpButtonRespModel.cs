using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class HelpButtonRespModel
    {
        public bool isError { get; set; }
        public string message { get; set; } = string.Empty;
        public List<UserType> data { get; set; } = new List<UserType>();

    }

    public class UserType
    {
        public int UserTypeId { get; set; }
        public string UserTypeName { get; set; } = string.Empty;
        public IEnumerable<ContentType> contentTypes { get; set; } = Enumerable.Empty<ContentType>();
    }


    public class ContentType
    {
        public int contentTypeId { get; set; }
        public string contentTypeName { get; set; } = string.Empty;
        public int UserTypeId { get; set; }
        public IEnumerable<ContentUrl> contentUrl { get; set; } = Enumerable.Empty<ContentUrl>();
    }    

    public class ContentUrl
    {
        public int urlId { get; set; }
        public string url { get; set; } = string.Empty; 
        public int userTypeId { get; set; }
    }
     
}
 