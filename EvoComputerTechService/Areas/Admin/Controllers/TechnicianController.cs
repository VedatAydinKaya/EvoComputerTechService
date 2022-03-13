using EvoComputerTechService.Data;
using EvoComputerTechService.Extensions;
using EvoComputerTechService.Models;
using EvoComputerTechService.Models.Entities;
using EvoComputerTechService.Models.Identity;
using EvoComputerTechService.Services;
using EvoComputerTechService.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EvoComputerTechService.Areas.Admin.Controllers
{
    public class TechnicianController : TechnicianBaseController
    {
        private readonly MyContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;


        public TechnicianController(MyContext dbContext, UserManager<ApplicationUser> userManager,IEmailSender emailSender)
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
        public async Task<IActionResult> MyIssues()
        {
            var user = await _userManager.FindByIdAsync(HttpContext.GetUserId());

            var myIssues = _dbContext.Issues.Where(x => x.TechnicianId == user.Id && x.IssueState == IssueStates.Atandi).ToList();

            return View(myIssues);
        }
        public IActionResult AcceptIssue(Guid id)
        {
            var issue = _dbContext.Issues.Find(id);
            issue.IssueState = IssueStates.Islemde;
            _dbContext.SaveChanges();

            return RedirectToAction("AcceptedIssues");
        }

        [HttpGet]
        public async Task<IActionResult> AcceptedIssues()
        {
            var user = await _userManager.FindByIdAsync(HttpContext.GetUserId());

            var acceptedIssues = _dbContext.Issues.Where(x => x.TechnicianId == user.Id &&
                x.IssueState == IssueStates.Islemde)
                .ToList();

            return View(acceptedIssues);
        }

        [HttpGet]
        public IActionResult IssueDetail(Guid id)
        {
          TempData["issueId"] = id;
          var issue = _dbContext.Issues.Find(id);
          var products=_dbContext.Products.ToList();
          var issueInProduct=_dbContext.IssueProducts.Where(x=>x.IssueId==issue.Id).Include(x=>x.Product).ToList();
          
          IssueProductViewModel model =new IssueProductViewModel();
          model.Products=products;
          model.IssueProducts = issueInProduct;

          TempData["buttonCheck"] = (int)issue.IssueState as object;

          return View(model);
          
        }
        [HttpPost]
        public IActionResult AddProduct(Guid id) 
        {
            var issueId = TempData["issueId"];

            var issue = _dbContext.Issues.Find(issueId);
            var product = _dbContext.Products.Find(id);

            var issueinProduct = _dbContext.IssueProducts.Where(x => x.IssueId == issue.Id).Include(x => x.Product).ToList();

            var control=issueinProduct.SingleOrDefault(x=>x.ProductId==product.Id);

            if (control==null)
            {
                IssueProducts model = new IssueProducts()
                {
                    Issue = issue,
                    Product = product,
                    ProductId = product.Id,
                    IssueId = issue.Id,
                    Price = product.Price,
                    Quantity = 1
                };
                 _dbContext.IssueProducts.Add(model);
            }
            else
            {
                control.Quantity++;
                control.Price= product.Price*control.Quantity;
            }

            _dbContext.SaveChanges();

            return RedirectToAction("IssueDetail", new { id = issueId });
        }
        [HttpPost]
        public  IActionResult DeleteProduct(Guid id) 
        {
            var product = _dbContext.Products.Find(id);
            var issueInProduct = _dbContext.IssueProducts.Where(x => x.ProductId == product.Id).FirstOrDefault();

            _dbContext.Remove(issueInProduct);
            _dbContext.SaveChanges();

            return RedirectToAction("IssueDetail", new { id = issueInProduct.IssueId }); 
        }
        public async Task<IActionResult> CompletedIssues()
        {
            // TempData["issueId"] = id;
             var issueId = TempData["issueId"];
            // var issue = _dbContext.Issues.Find(issueId);
            var issue = _dbContext.Issues.Find(issueId);
            issue.IssueState = IssueStates.Tamamlandi;
            _dbContext.SaveChanges();

            var user = await _userManager.FindByIdAsync(issue.UserId);

            //string link = HttpContext.Request.Url.Scheme + "://" + HttpContext.Request.Url.Authority + Url.Action("ResetPassword", "Account", new { key = randomString });
           // string url = this.Url.Action("Purchase", "Payment");
            await _emailSender.SendAsync(new EmailMessage()
            {
                //User maili koy
                Contacts = new string[] { "abc@gmail.com" },
                Body = $" {user.Name} Tarafından Olusturulan  {issue.IssueName} Adlı Arıza {HttpContext.User.Identity.Name} Tarafından  Tamamlandı Lutfen Odeme yapınız <br/> " + 
                $"Please make your payment by  < a href='https://localhost:44343/Payment/Purchase'> clicking here </ a >",
               
                // to do : URL LINKI CALISILACAK

               // // https://localhost:44343/admin/Technician/MyIssues
               //// < a href = 'https://localhost:44343/Payment/Purchase' > clicking here </ a >.
                Subject = " Odeme BEKLEME",
                Cc = new string[] { "abc@gmail.com" },
                Bcc = new string[] { },
            });

            return RedirectToAction("CompletedIssuesDone",new { id=issue.Id});
        }
        public async Task<IActionResult> CompletedIssuesDone()
        {
            var user = await _userManager.FindByIdAsync(HttpContext.GetUserId());

            //var completedIssues = _dbContext.Issues.Where(x => x.TechnicianId == user.Id && x.IssueState == IssueStates.Tamamlandi).ToList();

            var  completedIssues=_dbContext.Issues.Where(x=>x.TechnicianId==user.Id && (int)x.IssueState>=3).ToList();

            return View(completedIssues);
        }

    }

}
