#pragma warning disable 
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata.Ecma335;
using Microsoft.AspNetCore.Http;
using TheWall.Models;

namespace TheWall.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private  MyContext _context;
    private bool InSession
    {
        get {return HttpContext.Session.GetInt32("userId") != null;}
    }
    private User LoggedInUser
    {
        get{return _context.Users.Include(m=>m.CreatedMessages).FirstOrDefault(u=>u.UserId == HttpContext.Session.GetInt32("userId"));}
    }

    public HomeController(ILogger<HomeController> logger,MyContext context)
    {
        _logger = logger;
        _context = context;
    }


    public IActionResult Index()
    {
        return View();
    }
        //__________________Registration_________________
        [HttpPost("user/register")]
    public IActionResult Register(User newUser)
    {
        if (ModelState.IsValid)
        {
            if (_context.Users.Any(u => u.Email == newUser.Email))
            {
                ModelState.AddModelError("Email", "Email already exist");
                return View("Index");
            }
            //hashing password
            PasswordHasher<User> hasher = new PasswordHasher<User>();
            newUser.Password = hasher.HashPassword(newUser, newUser.Password);
            //adding to database
            _context.Add(newUser);
            _context.SaveChanges();
            TempData["Message"] = "Registered Successfully";
            return RedirectToAction("Index");
        }
        return View("Index");
    }
    //_______Login_______________________
    [HttpPost("/user/login")]
    public IActionResult Login(Login logUser)
    {
        if (ModelState.IsValid)
        {
            User? userInDb = _context.Users.FirstOrDefault(l => l.Email == logUser.LoginEmail);
            if (userInDb == null)
            {
                ModelState.AddModelError("LoginEmail", "Incorrect validation");
                return View("Index");
            }
            //comparing password
            PasswordHasher<Login> hasher = new PasswordHasher<Login>();
            var result = hasher.VerifyHashedPassword(logUser, userInDb.Password, logUser.LoginPassword);
            if (result == 0)
            {
                ModelState.AddModelError("LoginPassword", "Incorrect validation");
                return View("Index");
            }
            HttpContext.Session.SetInt32("userId", userInDb.UserId);
            return RedirectToAction("Wall");
        }
        return View("Index");
    }
    //_________LogOut___________
    [HttpGet("logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index");
    }
    //__________The_Wall_View
    [HttpGet("wall")]
    public IActionResult Wall()
    {
        if(!InSession)
        {
            RedirectToAction("Index");
        }
        ViewBag.User = LoggedInUser;
        ViewBag.AllMessages = _context.Messages.Include(u=>u.Creator)
                                            .Include(r=>r.HasComments)
                                            .ThenInclude(c=>c.User)
                                            .ToList()
                                            .OrderByDescending(o=>o.CreatedAt);                                                
        return View();
    }

    //____________New Message_____

    [HttpPost("/message/new")]
    public IActionResult Message()
    {
        Message newMsg = new Message();
        newMsg.UserId = LoggedInUser.UserId;
        newMsg.MessageText = Request.Form["MessageText"];
        _context.Messages.Add(newMsg);
        _context.SaveChanges();
        return RedirectToAction("Wall");
    }
    //__________New_Comment_________
    [HttpPost("/comment/{messageId}")]
    public IActionResult Comment(int messageId)
    {
        Comment newCom = new Comment();
        newCom.CommentText = Request.Form["CommentText"];
        newCom.UserId = LoggedInUser.UserId;
        newCom.MessageId = messageId;
        _context.Comments.Add(newCom);
        _context.SaveChanges();
        return RedirectToAction("Wall");
    }
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

}
