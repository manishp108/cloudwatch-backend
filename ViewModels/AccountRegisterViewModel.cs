using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BackEnd.ViewModels
{
    public class AccountRegisterViewModel      // ViewModel used for user registration requests
    {

        [StringLength(60, MinimumLength = 3)]
        [BindProperty]
        [Required]
        public string Username { get; set; }

        public string Message { get; set; }          // Optional message field for API responses

    }
}
