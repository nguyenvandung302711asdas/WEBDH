using iTextSharp.text.pdf;
using iTextSharp.text;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebApplication1.Models;
namespace admin4.Controllers
{
    public class CustomersController : Controller
    {
        private WatchStoreEntities9 db = new WatchStoreEntities9();

        // GET: Customers
        public ActionResult Index(string search = "", string SortColumn = "CustomerID", string IconClass = "fa-sort-asc", int page = 1, int entriesPerPage = 10, string gender = "Gioi tinh")
        {
            // Initialize the queryable for customers
            var or = db.Customers.AsQueryable();

            // Apply gender filter if a valid value is selected
            if (!string.IsNullOrWhiteSpace(gender) && gender.Trim() != "Gioi tinh")
            {
                or = or.Where(p => p.Gender == gender.Trim());
            }

            // Store the selected gender in ViewBag
            ViewBag.Gender = gender;

            // Filter customers based on search input
            if (!string.IsNullOrEmpty(search))
            {
                or = or.Where(c => c.FullName.Contains(search));
            }

            // Store the search term in ViewBag to keep it in the search field
            ViewBag.SearchTerm = search;

            // Sorting logic
            or = SortCustomers(or, SortColumn, IconClass);
            // Filter the entries where check_Remove == 1
            var filteredCustomers = or.Where(c => c.check_Remove == 1);

            // Pagination logic
            int totalEntries = filteredCustomers.Count();
            int totalPages = (int)Math.Ceiling((double)totalEntries / entriesPerPage);

            // Calculate records to skip for pagination
            var paginatedCustomers = filteredCustomers.Skip((page - 1) * entriesPerPage).Take(entriesPerPage).ToList();

            // Set ViewBag values for pagination and sorting
            ViewBag.TotalEntries = totalEntries;
            ViewBag.Page = page;
            ViewBag.NoOfPages = totalPages;
            ViewBag.StartEntry = (page - 1) * entriesPerPage + 1;
            ViewBag.EndEntry = Math.Min(page * entriesPerPage, totalEntries);



            ViewBag.SortColumn = SortColumn;
            ViewBag.IconClass = IconClass;

            // Pass the paginated customer list to the view
            return View(paginatedCustomers);
        }

        // Helper method for sorting customers
        private IQueryable<Customer> SortCustomers(IQueryable<Customer> customers, string sortColumn, string iconClass)
        {
            switch (sortColumn)
            {
                case "FullName":
                    return iconClass == "fa-sort-asc" ? customers.OrderBy(c => c.FullName) : customers.OrderByDescending(c => c.FullName);
                case "Email":
                    return iconClass == "fa-sort-asc" ? customers.OrderBy(c => c.Email) : customers.OrderByDescending(c => c.Email);
                // Add more cases as needed for other columns
                default:
                    return iconClass == "fa-sort-asc" ? customers.OrderBy(c => c.CustomerID) : customers.OrderByDescending(c => c.CustomerID);
            }
        }


        // GET: Customers/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Customer customer = db.Customers.Find(id);
            if (customer == null)
            {
                return HttpNotFound();
            }
            return View(customer);
        }
        // GET: Customers/Create

        // GET: Customer/Create
        // GET: Customer/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Customer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Customer model, HttpPostedFileBase ImgCustomer)
        {
            if (ModelState.IsValid)
            {
                // Xử lý upload file ảnh
                if (ImgCustomer != null && ImgCustomer.ContentLength > 0)
                {
                    // Lấy tên file
                    var fileName = Path.GetFileName(ImgCustomer.FileName);

                    // Đường dẫn để lưu file trong thư mục
                    var path = Path.Combine(Server.MapPath("~/Content/assets/images/avatars/"), fileName);
                     
                    // Lưu file vào thư mục
                    ImgCustomer.SaveAs(path);

                    // Gán tên file vào thuộc tính ImgCustomer của model
                    model.ImgCustomer = fileName;
                }
                else
                {
                    // Nếu không có file upload, sử dụng ảnh mặc định
                    model.ImgCustomer = "default.png";
                }

                // Thiết lập các giá trị mặc định khác
                model.check_Remove = 1;

                // Lưu model vào cơ sở dữ liệu
                db.Customers.Add(model);
                db.SaveChanges();

                // Chuyển hướng về trang danh sách
                return RedirectToAction("Index");
            }

            // Nếu có lỗi, trả về view với thông tin đã nhập
            return View(model);
        }




        // GET: Customers/Edit/5

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "ID không hợp lệ.");
            }

            try
            {
                Customer customer = db.Customers.Find(id);
                if (customer == null)
                {
                    return HttpNotFound("Không tìm thấy khách hàng.");
                }

                return View(customer);
            }
            catch (Exception ex)
            {
                // Ghi log lỗi (tùy thuộc vào hệ thống của bạn)
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Có lỗi xảy ra khi xử lý yêu cầu.");
            }
        }


        // POST: Customers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        // POST: Customers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "CustomerID,FullName,Email,Phone,Gender,Address")] Customer customer)
        {
            if (!ModelState.IsValid)
            {
                // Nếu dữ liệu không hợp lệ, trả về view với thông báo lỗi
                return View(customer);
            }

            try
            {
                // Tìm khách hàng trong cơ sở dữ liệu để đảm bảo tính chính xác
                var existingCustomer = db.Customers.Find(customer.CustomerID);
                if (existingCustomer == null)
                {
                    return HttpNotFound("Không tìm thấy khách hàng để chỉnh sửa.");
                }

                // Chỉ cập nhật các trường được phép (tránh ghi đè mật khẩu hoặc các trường nhạy cảm)
                existingCustomer.FullName = customer.FullName;
                existingCustomer.Email = customer.Email;
                existingCustomer.Phone = customer.Phone;
                existingCustomer.Gender = customer.Gender;
                existingCustomer.Address = customer.Address;

                // Lưu thay đổi vào cơ sở dữ liệu
                db.Entry(existingCustomer).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();

                // Chuyển hướng về danh sách khách hàng sau khi lưu thành công
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Ghi log lỗi (tùy thuộc vào hệ thống của bạn)
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật khách hàng. Vui lòng thử lại.");
                return View(customer);
            }
        }

        // GET: Customers/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Customer customer = db.Customers.Find(id);
            if (customer == null)
            {
                return HttpNotFound();
            }
            return View(customer);
        }

        // POST: Customers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Customer customer = db.Customers.Find(id);
            //db.Customers.Remove(customer);
            customer.check_Remove = 0;


            Product product = db.Products.Find(id);
            //product.StockQuantity = 0;
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }



      


        public ActionResult ExportToPDF(int? customerId, string searchTerm = "")
        {
            // Lọc danh sách khách hàng có check_Remove == 1
            var customers = db.Customers
                              .Where(c => c.check_Remove == 1)
                              .OrderBy(c => c.CustomerID)
                              .ToList();

            // Tạo tài liệu PDF
            Document pdfDoc = new Document(PageSize.A4, 10f, 10f, 20f, 20f);
            MemoryStream memoryStream = new MemoryStream();

            PdfWriter writer = PdfWriter.GetInstance(pdfDoc, memoryStream);
            pdfDoc.Open();

            try
            {
                // Đường dẫn đến file font
                string fontPath = Server.MapPath("~/Content/Font/NotoSans.ttf");
      

                // Kiểm tra nếu file font không tồn tại
                if (!System.IO.File.Exists(fontPath))
                {
                    throw new FileNotFoundException("Font không tồn tại tại đường dẫn: " + fontPath);
                }

                // Sử dụng font tùy chỉnh
                BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                Font font = new Font(baseFont, 12, Font.NORMAL);
                Font headerFont = new Font(baseFont, 14, Font.BOLD);

                // Tiêu đề
                pdfDoc.Add(new Paragraph("DANH SÁCH KHÁCH HÀNG", headerFont));
                pdfDoc.Add(new Paragraph($"Tìm kiếm: {(string.IsNullOrEmpty(searchTerm) ? "Tất cả" : searchTerm)}", font));
                pdfDoc.Add(new Paragraph(" ")); // Dòng trống

                // Bảng dữ liệu
                PdfPTable table = new PdfPTable(6) { WidthPercentage = 100 }; // 6 cột

                // Cài đặt độ rộng cột
                table.SetWidths(new float[] { 10f, 25f, 30f, 20f, 30f, 15f });

                // Thêm header
                table.AddCell(new PdfPCell(new Phrase("CustomerID", headerFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
                table.AddCell(new PdfPCell(new Phrase("FullName", headerFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
                table.AddCell(new PdfPCell(new Phrase("Email", headerFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
                table.AddCell(new PdfPCell(new Phrase("Phone", headerFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
                table.AddCell(new PdfPCell(new Phrase("Address", headerFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
                table.AddCell(new PdfPCell(new Phrase("Gender", headerFont)) { HorizontalAlignment = Element.ALIGN_CENTER });

                // Thêm dữ liệu khách hàng vào bảng
                foreach (var customer in customers)
                {
                    table.AddCell(new PdfPCell(new Phrase(customer.CustomerID.ToString(), font)) { HorizontalAlignment = Element.ALIGN_LEFT });
                    table.AddCell(new PdfPCell(new Phrase(customer.FullName, font)) { HorizontalAlignment = Element.ALIGN_LEFT });
                    table.AddCell(new PdfPCell(new Phrase(customer.Email, font)) { HorizontalAlignment = Element.ALIGN_LEFT });
                    table.AddCell(new PdfPCell(new Phrase(customer.Phone, font)) { HorizontalAlignment = Element.ALIGN_LEFT });
                    table.AddCell(new PdfPCell(new Phrase(customer.Address, font)) { HorizontalAlignment = Element.ALIGN_LEFT });
                    table.AddCell(new PdfPCell(new Phrase(customer.Gender, font)) { HorizontalAlignment = Element.ALIGN_LEFT });
                }

                pdfDoc.Add(table);
            }
            catch (Exception ex)
            {
                // Thêm thông báo lỗi vào PDF (dành cho kiểm tra trong quá trình phát triển)
                pdfDoc.Add(new Paragraph("Có lỗi xảy ra: " + ex.Message));
            }
            finally
            {
                // Đóng tài liệu
                pdfDoc.Close();
            }

            // Trả file PDF về client
            byte[] bytes = memoryStream.ToArray();
            memoryStream.Close();

            return File(bytes, "application/pdf", "DanhSachKhachHang.pdf");
        }





    }
}
