using AutoMapper;
using EvoComputerTechService.Data;
using EvoComputerTechService.Extensions;
using EvoComputerTechService.Models;
using EvoComputerTechService.Models.Entities;
using EvoComputerTechService.Models.Identity;
using EvoComputerTechService.Models.Payment;
using EvoComputerTechService.Services;
using EvoComputerTechService.ViewModels;
using Iyzipay.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EvoComputerTechService.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly MyContext _dbContext;
        private readonly IMapper _mapper;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public PaymentController(IPaymentService paymentservice, MyContext dbContext, IMapper mapper, UserManager<ApplicationUser> userManager,IEmailSender emailSender)
        {
            _paymentService = paymentservice;
            _dbContext = dbContext;
            _mapper = mapper;
            _userManager = userManager;
            _emailSender = emailSender;
            var cultureInfo = CultureInfo.GetCultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
        }
        [Authorize]
        public IActionResult Purchase(Guid id)
        {
            var issue = _dbContext.Issues.Find(id);

            if (issue == null)
                return RedirectToAction("Index", "Home");

            var issueProduct = _dbContext.IssueProducts.Where(x => x.IssueId == issue.Id /*&& x.Issue.IssueState == IssueStates.Tamamlandi*/).ToList();

            var product = _dbContext.IssueProducts.Where(x => x.IssueId == issue.Id).Select(x => x.Product).ToList();

            IssueProductViewModel model = new IssueProductViewModel();
            model.IssueProducts = issueProduct;
            model.Products = product;

            issue.IssueState = IssueStates.OdemeBekleme;

            _dbContext.SaveChanges();

            //  ViewBag.Subs = model.IssueProducts.Select(x => x.Issue).FirstOrDefault();


            TempData["IssueName"] = model.IssueProducts.Select(x => x.Issue.IssueName).FirstOrDefault();
            TempData["IssueState"] = model.IssueProducts.Select(x => x.Issue.IssueState).FirstOrDefault();

            TempData["issueProduct"] = model.IssueProducts as object;

            decimal price = 0;
            foreach (var item in model.IssueProducts)
            {
                price += item.Price;
            };

            PaymentViewModel modelnew = new PaymentViewModel()
            {
                BasketModel = new BasketModel()
                {
                    Category1 = issue.IssueName,
                    Id = issue.Id.ToString(),
                    ItemType = BasketItemType.VIRTUAL.ToString(),
                    Name = issue.IssueName,
                    Price = price.ToString()
                }
            };

            modelnew.Amount = price;

            return View(modelnew);
        }
        [Authorize]
        [HttpPost]
        public IActionResult CheckInstallment(string binNumber, decimal price)
        {
            if (binNumber.Length != 6 || binNumber.Length > 16)
                return BadRequest(new
                {
                    Message = "Bad req."
                });

            var result = _paymentService.CheckInstallments(binNumber, price);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Purchase(PaymentViewModel model)
        {

            var issue = _dbContext.Issues.Find(Guid.Parse(model.BasketModel.Id));

            if (issue == null)
                return RedirectToAction("Index", "Home");

            var issueProduct = _dbContext.IssueProducts.Where(x => x.IssueId == issue.Id && x.Issue.IssueState == IssueStates.OdemeBekleme).ToList();

            var product = _dbContext.IssueProducts.Where(x => x.IssueId == issue.Id).Select(x => x.Product).ToList();

            IssueProductViewModel model2 = new IssueProductViewModel();
            model2.IssueProducts = issueProduct;
            model2.Products = product;

            decimal price = 0;
            foreach (var item in model2.IssueProducts)
            {
                price += item.Price;
            };


            var basketModel = new BasketModel()
            {
                Category1 = issue.IssueName,
                Id = issue.Id.ToString(),
                ItemType = BasketItemType.VIRTUAL.ToString(),
                Name = issue.IssueName,
                Price = price.ToString(),
            };

            var user = await _userManager.FindByIdAsync(HttpContext.GetUserId());

            var addressModel = new AddressModel()
            {
                City = "Istanbul",
                ContactName = $"{user.Name} {user.Surname}",
                Country = "Turkiye",
                Description = issue.AddressDetail,
                ZipCode = "34700",
            };

            var customerModel = new CustomerModel()
            {
                City = "Istabul",
                Country = "Turkiye",
                Email = user.Email,
                GsmNumber = user.PhoneNumber,
                Id = user.Id,
                IdentityNumber = user.Id,
                Ip = Request.HttpContext.Connection.RemoteIpAddress?.ToString(),
                Name = user.Name,
                Surname = user.Surname,
                ZipCode = addressModel.ZipCode,
                LastLoginDate = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                RegistrationDate = $"{user.CreatedDate:yyyy-MM-dd HH:mm:ss}",
                RegistrationAddress = $" Enlem:{issue.Latitude} Boylam:{issue.Longitude} "
            };

            var paymentModel = new PaymentModel()
            {
                Installment = model.Installment,
                Address = addressModel,
                BasketList = new List<BasketModel>{ basketModel },
                Customer = customerModel,
                CardModel = model.CardModel,
                Price =Convert.ToDecimal(basketModel.Price),
                UserId = HttpContext.GetUserId(),
                Ip = Request.HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            var installmentInfo = _paymentService.CheckInstallments(paymentModel.CardModel.CardNumber.Substring(0, 6), paymentModel.Price);

            var installmentNumber = installmentInfo.InstallmentPrices.FirstOrDefault(x => x.InstallmentNumber == model.Installment);


            paymentModel.PaidPrice = decimal.Parse(installmentNumber != null ? installmentNumber.TotalPrice : installmentInfo.InstallmentPrices[0].TotalPrice);


            var result = _paymentService.Pay(paymentModel);

            if (result.Status=="success")
            {
                issue.IssueState = IssueStates.OdemeTamamlandı;

                await _emailSender.SendAsync(new EmailMessage()
                {
                    //User maili koy
                    Contacts = new string[] { "abc@gmail.com" },
                    Body = $"{issue.IssueName} Adlı Arızaya Ait {paymentModel.PaidPrice} Tutarındaki <br/> ODEME TAMAMLANDI  TESEKKUR EDERIZ :)",
              
                    // https://localhost:44343/admin/Technician/MyIssues
                    // < a href = 'https://localhost:44343/Payment/Purchase' > clicking here </ a >.
                    Subject = " Odeme TAMAMLANDI",
                    Cc = new string[] { "abc@gmail.com" },
                    Bcc = new string[] { },
                });

            }

            _dbContext.SaveChanges();

            return RedirectToAction("CompletedIssues", "Issue", new { id = issue.Id });
        }


    }
}
