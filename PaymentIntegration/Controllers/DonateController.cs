using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PaymentIntegration.Models;
using PaymentIntegration.Repository;
using PayStack.Net;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PaymentIntegration.Controllers
{
    public class DonateController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _dbContext;
        private readonly string token;

        private PayStackApi payStack { get; set; }
        public DonateController(IConfiguration configuration, AppDbContext dbContext)
        {
            _configuration = configuration;
            _dbContext = dbContext;
            token = _configuration["Payment:PaystackSk"];
            payStack = new PayStackApi(token);

        }
        [HttpGet]

        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Index(DonateViewModel donate)
        {
            TransactionInitializeRequest request = new TransactionInitializeRequest()
            {
                AmountInKobo = donate.Amount * 100,
                Email = donate.Email,
                Reference = Generate().ToString(), 
                Currency = "NGN",
                CallbackUrl = "http://localhost:30449/donate/verify"


            };
            TransactionInitializeResponse response = payStack.Transactions.Initialize(request);
            if (response.Status)
            {
                var tranction = new TransactionModel()
                {
                    Amount = donate.Amount,
                    Email = donate.Email,
                    TrfRef = request.Reference,
                    Name = donate.Name,

                };
                await _dbContext.AddAsync(tranction);
                await _dbContext.SaveChangesAsync();
               return Redirect(response.Data.AuthorizationUrl);
            }
            ViewData["error"] = response.Message;

            return View();
        }
        public static int Generate()
        {
            Random rand = new Random((int)DateTime.Now.Ticks);
            return rand.Next(100000000, 999999999);
        }
        [HttpGet]
        public IActionResult Donations()
        {
            var transactions = _dbContext.Transactions.Where(x => x.status == true).ToList();
            ViewData["transaction"] = transactions;
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> Verify(string reference)
        {
            TransactionVerifyResponse response = payStack.Transactions.Verify(reference);
            if (response.Data.Status == "success")
            {
                var transaction = _dbContext.Transactions.Where(x => x.TrfRef == reference).FirstOrDefault();
                if (transaction != null)
                {
                    transaction.status = true;
                    _dbContext.Transactions.Update(transaction);
                    await _dbContext.SaveChangesAsync();
                    return RedirectToAction("Donations");
                }
            }
            ViewData["error"] = response.Data.GatewayResponse;
            return RedirectToAction("Index");
        }
    }
}
