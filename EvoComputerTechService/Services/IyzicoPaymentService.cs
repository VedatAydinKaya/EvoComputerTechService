using AutoMapper;
using EvoComputerTechService.Models.Identity;
using EvoComputerTechService.Models.Payment;
using EvoComputerTechService.Services;

using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using MUsefulMethods;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace EvoComputerTechService.Services
{
    public class IyzicoPaymentService : IPaymentService
    {

        private readonly IyzicoPaymentOptions _options;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly UserManager<ApplicationUser> _userManager;
        
        public IyzicoPaymentService(IConfiguration configuration, IMapper mapper, UserManager<ApplicationUser> userManager)
        {
            _configuration = configuration;
            _mapper = mapper;
            _userManager = userManager;
            
            var section = _configuration.GetSection(IyzicoPaymentOptions.Key);  // Gets a configuration sub-section with the specified key

            _options = new IyzicoPaymentOptions()
            {
                ApiKey = section["ApiKey"],
                SecretKey = section["SecretKey"],
                BaseUrl = section["BaseUrl"],
                ThreedsCallbackUrl = section["ThreedsCallbackUrl"]

            };
        }
        private string GenerateConverstaionId()
        {
            return StringHelpers.GenerateUniqueCode();
        }

        private CreatePaymentRequest InitialPaymentRequest(PaymentModel paymentModel)
        {

            var paymentRequest = new CreatePaymentRequest()
            {

                Installment = paymentModel.Installment,
                Locale = Locale.TR.ToString(),
                ConversationId = GenerateConverstaionId(),
                Price = paymentModel.Price.ToString(new CultureInfo("en-US")),
                PaidPrice = paymentModel.PaidPrice.ToString(new CultureInfo("en-US")),
                Currency = Currency.TRY.ToString(),
                BasketId = StringHelpers.GenerateUniqueCode(),
                PaymentChannel = PaymentChannel.WEB.ToString(),
                PaymentGroup = PaymentGroup.SUBSCRIPTION.ToString(),
                PaymentCard = _mapper.Map<PaymentCard>(paymentModel.CardModel),
                Buyer = _mapper.Map<Buyer>(paymentModel.Customer),
                BillingAddress = _mapper.Map<Address>(paymentModel.Address),


            };

            var basketItems = new List<BasketItem>();

            foreach (var basketModel in paymentModel.BasketList)
            {
                basketItems.Add(_mapper.Map<BasketItem>(basketModel));
            }

            paymentRequest.BasketItems = basketItems;

            return paymentRequest;
        }
        public InstallmentModel CheckInstallments(string binNumber, decimal price)
        {

            var conversationId = GenerateConverstaionId();

            var request = new RetrieveInstallmentInfoRequest()
            {
                Locale = Locale.TR.ToString(),
                ConversationId = conversationId,
                BinNumber = binNumber.Substring(0, 6),
                Price = price.ToString(new CultureInfo("en-US"))
            };

            var result = InstallmentInfo.Retrieve(request, _options);
            if (result.Status == "failure")
            {
                throw new Exception(result.ErrorMessage);
            }


            if (result.ConversationId != conversationId)
            {
                throw new Exception("Hatalı Istek Olusturuldu");
            }

            var resultModel = _mapper.Map<InstallmentModel>(result.InstallmentDetails[0]); // <Tdestination>(object source) = executes mapping from source object to T destination

            return resultModel;
        }
        public PaymentResponseModel Pay(PaymentModel paymentModel)
        {
            var request = this.InitialPaymentRequest(paymentModel); // return CreatePaymentRequest
            var payment = Payment.Create(request, _options);

            return _mapper.Map<PaymentResponseModel>(payment);
        }
    }
}
