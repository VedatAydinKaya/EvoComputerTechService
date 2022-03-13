using EvoComputerTechService.Data;
using EvoComputerTechService.Extensions;
using EvoComputerTechService.Models;
using EvoComputerTechService.Models.Entities;
using EvoComputerTechService.Models.Identity;
using EvoComputerTechService.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EvoComputerTechService.Areas.Admin.Controllers
{
    public class OperatorController : OperatorBaseController
    {
        private readonly MyContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public OperatorController(MyContext dbContext, UserManager<ApplicationUser> userManager,IEmailSender emailSender)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult WaitingIssues()
        {
            var waitingIssues = _dbContext.Issues.Where(x=>x.IssueState == IssueStates.Beklemede).ToList();

            return View(waitingIssues);
        }

        [HttpGet]
        public   IActionResult ActiveIssues()
        {
            var activeIssues = _dbContext.Issues.Where(x => x.IssueState == IssueStates.Islemde).ToList();

            return View(activeIssues);
        }

        [HttpGet]
        public IActionResult AssignedIssues()
        {
            var assignedIssues = _dbContext.Issues.Where(x => x.IssueState == IssueStates.Atandi).ToList();

            return View(assignedIssues);
        }

        [HttpGet]
        public IActionResult CompletedIssues()
        {
            var completedIssues = _dbContext.Issues.Where(x => x.IssueState == IssueStates.Tamamlandi).ToList();

            return View(completedIssues);
        }

        [HttpGet]
        public IActionResult AssignTechnician(Guid id)
        {
            var issue = _dbContext.Issues.Find(id);

            var Technicians = new List<SelectListItem>();

            var x = _userManager.GetUsersInRoleAsync("Technician").Result;
            var users = x.OfType<ApplicationUser>();

            foreach (var item in users)
            {
                Technicians.Add(new SelectListItem
                {
                    Text = $"{item.Name} {item.Surname}",
                    Value = item.Id.ToString()
                });
            }

            ViewBag.Technicians = Technicians;
            
            return View(issue);
        }

        [HttpPost]
        public async Task<IActionResult> AssignTechnician(string[] Technician,Guid id)
        {
            //if (Technician.Length>1)
            //{
                 
            //}

            var issue = _dbContext.Issues.Find(id);
            issue.TechnicianId = Technician[0];
            issue.IssueState = IssueStates.Atandi;

            var user=await _userManager.FindByIdAsync(issue.TechnicianId);

            _dbContext.SaveChanges();

            await _emailSender.SendAsync(new EmailMessage()
            {
                //User maili koy
                Contacts = new string[] { "abc@gmail.com" },
                Body = $"{HttpContext.User.Identity.Name} Adlı Kullanıcı  Tarafından" +
                $" {issue.IssueName} Adlı Arıza {user.Name} Atandı",
                Subject = "Teknik Arıza Kaydı",
                Cc = new string[] { "abc@gmail.com" },
                Bcc = new string[] { },
            });

            return RedirectToAction("AssignedIssues");
        }



    }
}
