
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BackEnd.ViewModels
{
    public class AccountLoginViewModel      // ViewModel used for user login requests
    {

        [StringLength(60, MinimumLength = 3)]
        [BindProperty]
        [Required]
        public string Username { get; set; }
    }
}
