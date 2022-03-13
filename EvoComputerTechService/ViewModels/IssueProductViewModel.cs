using EvoComputerTechService.Models.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace EvoComputerTechService.ViewModels
{
    public class IssueProductViewModel
    {

       
        public List<Product> Products { get; set; }

        public List<IssueProducts> IssueProducts { get; set; }





    }
}
