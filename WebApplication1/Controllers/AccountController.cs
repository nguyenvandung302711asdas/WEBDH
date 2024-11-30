using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class AccountController : Controller
    {
        WatchStoreEntities9 db = new WatchStoreEntities9();
        // GET: Account
       
        public ActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Register(register sign)
        {
            if (ModelState.IsValid)
            {
                if (db.Customers.Any(x => x.Email == sign.Email) || db.Admins.Any(x => x.Email == sign.Email))
                {
                    ViewBag.Message = " Email already registered";
                }
                else
                {
                    var newCustomer = new Customer
                    {
                        FullName = sign.FullName,
                        Email = sign.Email,
                        Password = sign.Password,
                        Phone = sign.Phone,
                        Gender = sign.Gender,
                        Address = sign.Address
                    };
                    db.Customers.Add(newCustomer);
                    db.SaveChanges();
                    return RedirectToAction("Login", "Account");

                }
            }

            return View();
        }
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(login l)
        {
            var query1 = db.Customers
                  .FirstOrDefault(m => m.Email == l.Email && m.Password == l.Password);
            var query2 = db.Admins
                           .FirstOrDefault(a => a.Email == l.Email && a.Password == l.Password);
            if (query1 != null)
            {
                Session["UserLoggedIn"] = query1.FullName; // Lưu tên người dùng vào session
                Session["userEmail"] = query1.Email;
                Session["userId"] = query1.CustomerID;
                return RedirectToAction("Xuhuong", "Xuhuong");
            }
            else if (query2 != null)
            {
                Session["AddminLoggedIn"] = query2.FullName; // Lưu tên admin vào session

                return RedirectToAction("Index", "Adimin");
            }
            else
            {
                ModelState.AddModelError("", "Tên người dùng hoặc mật khẩu không đúng.");
            }
            return View();
        }
        public ActionResult Logout()
        {
            Session.Abandon();
            return RedirectToAction("Xuhuong", "Xuhuong");
        }

        public ActionResult ProfileUser()
        {
            List<Customer> customers = db.Customers.ToList();
            return View(customers);
        }
        public ActionResult EditProfileUser()
        {
            // Lấy email từ session để xác định người dùng hiện tại
            string userEmail = Session["userEmail"]?.ToString();
            if (userEmail == null) return RedirectToAction("Login", "Account");

            var customer = db.Customers.FirstOrDefault(c => c.Email == userEmail);
            if (customer == null) return HttpNotFound();

            return View(customer);
        }

        [HttpPost]
        public ActionResult EditProfileUser(Customer model)
        {
            if (ModelState.IsValid)
            {
                var customer = db.Customers.FirstOrDefault(c => c.CustomerID == model.CustomerID);
                if (customer != null)
                {
                    customer.FullName = model.FullName;
                    customer.Email = model.Email;
                    customer.Phone = model.Phone;
                    customer.Address = model.Address;
                    db.SaveChanges();  // Lưu thay đổi vào cơ sở dữ liệu
                    ViewBag.Message = "Profile updated successfully!";
                    return RedirectToAction("ProfileUser");
                }
            }
            return View(model);
        }
    }
}