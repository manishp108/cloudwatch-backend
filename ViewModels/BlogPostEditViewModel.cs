using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BackEnd.ViewModels
{
    public class BlogPostEditViewModel
    {
        public string PostId { get; set; } // Unique identifier of the blog post


        [Required(AllowEmptyStrings = false)]  // Blog post title (required)
        public string Title { get; set; }


        [Required(AllowEmptyStrings = false)] // Blog post content (required)
        public string Content { get; set; }

    }
}
