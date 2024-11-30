﻿using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication1.Models;
//using System;
//using System.Collections.Generic;
//using System.Drawing.Printing;
//using System.Drawing;
//using System.IO;
//using System.Linq;
//using System.Web;
//using System.Web.Mvc;
//using System.Xml.Linq;
//using WebApplication1.Models;
//using iTextSharp.text;
//using iTextSharp.text.pdf;
//using System.Data.Entity;



namespace WebApplication1.Controllers
{
    public class warehouseController : Controller
    {
        private WatchStoreEntities9 db = new WatchStoreEntities9();

        // Action để xuất PDF
        public ActionResult ExportToPDF(int? supplierId, string searchTerm = "")
        {
          


            //var products = query.OrderBy(p => p.ProductID).ToList();
            var products = db.Products.OrderBy(p => p.ProductID).ToList(); // Không dùng lọc


            // Tạo tài liệu PDF
            Document pdfDoc = new Document(PageSize.A4, 10f, 10f, 20f, 20f);
            MemoryStream memoryStream = new MemoryStream();

            PdfWriter writer = PdfWriter.GetInstance(pdfDoc, memoryStream);
            pdfDoc.Open();

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
            var supplier = supplierId.HasValue ? db.Suppliers.Find(supplierId.Value) : null;
            string supplierName = supplier != null ? supplier.SupplierName : "DANH SACH SAN PHAM TRONG KHO";
            pdfDoc.Add(new Paragraph($" {supplierName}", font));
            //var supplier = supplierId.HasValue ? db.Suppliers.Find(supplierId.Value) : null;
            //string supplierName = supplier != null ? supplier.SupplierName : "Tất cả";
            //pdfDoc.Add(new Paragraph($"Nha cung cap: {supplierName}", font));
            //pdfDoc.Add(new Paragraph($"Tim kiem: {searchTerm}", font));
            pdfDoc.Add(new Paragraph(" "));
            pdfDoc.Add(new Paragraph(" "));

            // Bảng dữ liệu
            PdfPTable table = new PdfPTable(4); // 4 cột
            table.WidthPercentage = 100;

            // Thêm header
            table.AddCell(new PdfPCell(new Phrase("Ma SP", font)));
            table.AddCell(new PdfPCell(new Phrase("Tên SP", font)));
            table.AddCell(new PdfPCell(new Phrase("Gia", font)));
            table.AddCell(new PdfPCell(new Phrase("So luong", font)));

            foreach (var product in products)
            {
                // Convert Price to string with proper formatting (e.g., currency format)
                table.AddCell(new PdfPCell(new Phrase(product.ProductID.ToString(), font)));
                table.AddCell(new PdfPCell(new Phrase(product.ProductName, font)));
                table.AddCell(new PdfPCell(new Phrase(product.Price.ToString("C0"), font)));  // Format as currency
                table.AddCell(new PdfPCell(new Phrase(product.StockQuantity.ToString(), font)));  // Convert int to string
            }


            pdfDoc.Add(table);

            // Đóng tài liệu
            pdfDoc.Close();

            // Trả file PDF về client
            byte[] bytes = memoryStream.ToArray();
            memoryStream.Close();

            return File(bytes, "WebApplication1/pdf", "DanhSachSanPham.pdf");
        }

        // GET: warehouse
        public ActionResult Index(int? supplierId, string searchTerm = "", int page = 1, int entriesPerPage = 10)
        {
            // Lấy danh sách nhà cung cấp để hiển thị trong dropdown list
            var suppliers = db.Suppliers.Select(s => new SelectListItem
            {
                Value = s.SupplierID.ToString(),
                Text = s.SupplierName,
                Selected = supplierId.HasValue && s.SupplierID == supplierId.Value
            }).ToList();
            ViewBag.Suppliers = suppliers;

            // Truy vấn sản phẩm
            IQueryable<Product> query = db.Products.Include(p => p.Supplier);

            if (supplierId.HasValue)
            {
                query = query.Where(p => p.SupplierID == supplierId.Value);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p => p.ProductName.Contains(searchTerm) ||
                                         p.Description.Contains(searchTerm));
            }

            // Tổng số sản phẩm (sau khi lọc)
            int totalEntries = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalEntries / entriesPerPage);

            if (entriesPerPage <= 0) entriesPerPage = 10; // Đảm bảo entriesPerPage hợp lệ

            // Phân trang
            var paginatedProducts = query
                .OrderBy(p => p.ProductID)
                .Skip((page - 1) * entriesPerPage)
                .Take(entriesPerPage)
                .ToList();

            // Gán thông tin phân trang vào ViewBag
            ViewBag.TotalEntries = totalEntries;
            ViewBag.Page = page;
            ViewBag.NoOfPages = totalPages;
            ViewBag.SearchTerm = searchTerm; // Lưu từ khóa tìm kiếm để hiển thị lại

            // Truyền danh sách sản phẩm vào view
            return View(paginatedProducts);
        }
    }
}
