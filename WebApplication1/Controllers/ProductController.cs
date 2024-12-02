using System;
using System.Collections.Generic;
using System.Data.Common.CommandTrees.ExpressionBuilder;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using System.Web.DynamicData;
using System.Web.Mvc;
using WebApplication1.Models;
using System.Linq;

namespace admin4.Controllers
{
    public class ProductImageViewModel
    {
        public string Hinh { get; set; }
    }

    public class ProductController : Controller
    {
        // GET: Products




        public ActionResult Index(
        string search = "",
        string sortColumn = "Id",
        string iconClass = "fa-sort-asc",
        int page = 1,
        int entriesPerPage = 10,
        int oldEntriesPerPage = 10,
        string status = "Category")
        {
            WatchStoreEntities9 db = new WatchStoreEntities9();

            // Lấy danh sách sản phẩm ban đầu
            var products = db.Products.AsQueryable();

            // Áp dụng bộ lọc danh mục nếu có
            if (!string.IsNullOrEmpty(status) && status != "Category")
            {
                products = products.Where(p => p.Category.CategoryName == status);
            }

            // Áp dụng tìm kiếm nếu có
            if (!string.IsNullOrEmpty(search))
            {
                products = products.Where(p => p.ProductName.Contains(search));
            }

            // Áp dụng sắp xếp
            switch (sortColumn)
            {
                case "Id":
                    products = iconClass == "fa-sort-asc"
                        ? products.OrderBy(p => p.ProductID)
                        : products.OrderByDescending(p => p.ProductID);
                    break;
                case "ProductName":
                    products = iconClass == "fa-sort-asc"
                        ? products.OrderBy(p => p.ProductName)
                        : products.OrderByDescending(p => p.ProductName);
                    break;
                case "UnitPrice":
                    products = iconClass == "fa-sort-asc"
                        ? products.OrderBy(p => p.Price)
                        : products.OrderByDescending(p => p.Price);
                    break;
            }

            // Nếu số lượng bản ghi hiển thị thay đổi, tính toán lại trang hiện tại
            if (oldEntriesPerPage != entriesPerPage)
            {
                // Tính chỉ số của bản ghi đầu tiên trên trang hiện tại
                int firstItemIndex = (page - 1) * oldEntriesPerPage;

                // Tính lại số trang dựa trên `entriesPerPage` mới
                page = (int)Math.Ceiling((double)(firstItemIndex + 1) / entriesPerPage);
            }

            // Tổng số bản ghi
            int totalEntries = products.Count();

            // Tổng số trang
            int totalPages = (int)Math.Ceiling((double)totalEntries / entriesPerPage);

            // Đảm bảo `page` nằm trong giới hạn hợp lệ
            page = Math.Max(1, Math.Min(page, totalPages));

            // Tính số bản ghi bỏ qua
            int skipRecords = (page - 1) * entriesPerPage;

            // Lấy danh sách sản phẩm phân trang
            var paginatedProducts = products
                .Skip(skipRecords)
                .Take(entriesPerPage)
                .ToList();

            // Truyền dữ liệu đến View
            ViewBag.Search = search;
            ViewBag.SortColumn = sortColumn;
            ViewBag.IconClass = iconClass;
            ViewBag.EntriesPerPage = entriesPerPage;
            ViewBag.OldEntriesPerPage = oldEntriesPerPage;
            ViewBag.TotalEntries = totalEntries;
            ViewBag.Page = page;
            ViewBag.NoOfPages = totalPages;
            ViewBag.Category = status;



            return View(paginatedProducts);
        }







        public ActionResult Detail(int? id)
        {
            WatchStoreEntities9 db = new WatchStoreEntities9();

            // Lấy thông tin sản phẩm
            Product pro = db.Products.FirstOrDefault(row => row.ProductID == id);
            if (pro == null)
            {
                // Nếu không tìm thấy sản phẩm
                ViewBag.AverageRating = 0;
                ViewBag.TotalQuantitySold = 0;
                ViewBag.StockQuantity = 0;
                ViewBag.feedback = new Feedback { ProductID = id, Rating = 0 };
                ViewBag.ProductImage = new List<ProductImageViewModel>();
                return View(); // Trả về View rỗng
            }

            // Lấy thông tin phản hồi
            Feedback f = db.Feedbacks.FirstOrDefault(row => row.ProductID == id) ?? new Feedback
            {
                ProductID = id.Value,
                Rating = 0
            };

            if (f.Rating == null) f.Rating = 0;
            ViewBag.feedback = f;

            // Lấy chi tiết sản phẩm
            Detail d = db.Details.FirstOrDefault(row => row.ProductID == id);
            ViewBag.detail = d;

            // Truy vấn tổng số lượng đã bán
            var productSales = db.OrderItems
                .Join(db.Orders, oi => oi.OrderID, o => o.OrderID, (oi, o) => new { oi, o })
                .Where(temp => temp.oi.ProductID == id && temp.o.Status == "Approved")
                .GroupBy(temp => temp.oi.ProductID)
                .Select(g => new
                {
                    TotalQuantitySold = g.Sum(x => x.oi.Quantity)
                })
                .FirstOrDefault();

            ViewBag.TotalQuantitySold = productSales?.TotalQuantitySold ?? 0;

            // Lấy số lượng tồn kho
            ViewBag.StockQuantity = pro.StockQuantity ?? 0;

            // Lấy hình ảnh sản phẩm
            var productImages = db.Images_Default
                .Where(img => img.ProductID == id)
                .Select(img => new ProductImageViewModel { Hinh = img.URL_Images_Default })
                .ToList();

            ViewBag.ProductImage = productImages;

            return View(pro);
        }


        public ActionResult Create()
        {
            using (WatchStoreEntities9 db = new WatchStoreEntities9())
            {
                // Retrieve the list of suppliers from the database
                var suppliers = db.Suppliers.ToList();
                ViewBag.Suppliers = suppliers;
                // Retrieve the list of suppliers from the database
                var brand = db.Brands.ToList();
                ViewBag.Brands = brand;
                // Retrieve the list of suppliers from the database
                var category = db.Categories.ToList();
                ViewBag.Categorie = category;
            }

            return View();
        }


       
        [HttpPost]
        public ActionResult Create(Product product, HttpPostedFileBase ImageUrl, HttpPostedFileBase Image1, HttpPostedFileBase Image2, HttpPostedFileBase Image3, HttpPostedFileBase Image4, HttpPostedFileBase Image5, HttpPostedFileBase ImageDetail , string URLVideo)
        {
            using (WatchStoreEntities9 db = new WatchStoreEntities9())
            {
                // Kiểm tra nếu SupplierID hợp lệ
                var supplierExists = db.Suppliers.Any(s => s.SupplierID == product.SupplierID);
                if (!supplierExists)
                {
                    ModelState.AddModelError("", "The selected supplier does not exist.");
                    return View(product);
                }

                // Kiểm tra nếu Brand hợp lệ
                var brandExists = db.Brands.Any(b => b.BrandID == product.BrandID);
                if (!brandExists)
                {
                    ModelState.AddModelError("", "The selected Brand does not exist.");
                    return View(product);
                }

                // Kiểm tra nếu Category hợp lệ
                var categoryExists = db.Categories.Any(c => c.CategoryID == product.CategoryID);
                if (!categoryExists)
                {
                    ModelState.AddModelError("", "The selected Category does not exist.");
                    return View(product);
                }
                //var productDetail = db.Products.Any(b => b.BrandID == product.BrandID);
                //if (!brandExists)
                //{
                //    ModelState.AddModelError("", "The selected Brand does not exist.");
                //    return View(product);
                //}
               
                // Kiểm tra nếu sản phẩm tồn tại
                var productToUpdate = db.Products.FirstOrDefault(p => p.ProductID == product.ProductID);
                if (productToUpdate == null)
                {
                    ModelState.AddModelError("", "The selected product does not exist.");
                    return View(product);
                }

                //// Cập nhật thông tin mô tả cho sản phẩm nếu sản phẩm hợp lệ
                //productToUpdate.Description = product.Description;
                //db.SaveChanges();
                ////return RedirectToAction("Index");


                // Handle the image file if it was uploaded
                if(ImageUrl != null && ImageUrl.ContentLength > 0)
                {
                    // Create directory path for the brand and product
                    var brand = db.Brands.FirstOrDefault(b => b.BrandID == product.BrandID)?.BrandName;
                    var productName = product.ProductName;
                    var directoryPath = Path.Combine(Server.MapPath("~/Content/img/"), brand, productName);

                    // Ensure directory exists
                    if(!Directory.Exists(directoryPath))
                    {
                        // Tạo thư mục mới với tên bao gồm productName + "Default"
                        string newDirectoryPathdefault = Path.Combine(directoryPath, productName + " Default");
                        string newDirectoryPathRelity = Path.Combine(directoryPath, productName + " Reality");
                        string newDirectoryPathFeedback = Path.Combine(directoryPath, productName + " Feedback");

                        Directory.CreateDirectory(newDirectoryPathdefault);
                        Directory.CreateDirectory(newDirectoryPathRelity);
                        Directory.CreateDirectory(newDirectoryPathFeedback);



                        // Generate the image file name and path
                        string fileName = Path.GetFileName(ImageUrl.FileName);
                        //string fileNameReality = Path.GetFileName(ImageDetail.FileName);
                        string filePath = Path.Combine(directoryPath, fileName);
                        ImageUrl.SaveAs(filePath);
                        product.ImageUrl = fileName;
                        // xg url


                        string fileNameReality = Path.GetFileName(ImageDetail.FileName);
                        string filePathReality = Path.Combine(newDirectoryPathRelity, fileName);
                        ImageDetail.SaveAs(filePath);

                        var realityImage = new Images_Reality
                        {
                            ProductID = product.ProductID,
                            URL_Images_Reality =(fileNameReality)
                        };
                        ImageDetail.SaveAs(filePathReality);

                        db.Images_Reality.Add(realityImage);
                        // xong Relity
                        SaveProductImageToDatabase(db, product, Image1, 1, newDirectoryPathdefault);
                        SaveProductImageToDatabase(db, product, Image2, 2, newDirectoryPathdefault);
                        SaveProductImageToDatabase(db, product, Image3, 3, newDirectoryPathdefault);
                        SaveProductImageToDatabase(db, product, Image4, 4, newDirectoryPathdefault);
                        SaveProductImageToDatabase(db, product, Image5, 5, newDirectoryPathdefault);
                        // xg default
                        // Save URL Video to the Video table
                        // Lưu URL video vào bảng Video
                        if (!string.IsNullOrEmpty(URLVideo))
                        {
                            var video = new Video
                            {
                                ProductID = product.ProductID,
                                URLVideo = URLVideo
                            };

                            db.Videos.Add(video);
                            //db.SaveChanges();
                       
                        }
                        product.AverageRating = 0;
                        product.Description = "asd";
                        product.Check_Remove = 0;
                        product.Discount = 0;
                    }
                }


               

                // Lưu sản phẩm vào cơ sở dữ liệu
                db.Products.Add(product);
                db.SaveChanges();

                return RedirectToAction("Index");
            }
        }

        // Hàm hỗ trợ để lưu ảnh vào Images_Default
        private void SaveProductImageToDatabase(WatchStoreEntities9 db, Product product, HttpPostedFileBase image, int imageNumber, string newDirectoryPathdefault)
        {
            if (image != null && image.ContentLength > 0)
            {
                string fileName = Path.GetFileName(image.FileName);
                string filePath = Path.Combine(newDirectoryPathdefault, (fileName));
                image.SaveAs(filePath);

                var defaultImage = new Images_Default
                {
                    ProductID = product.ProductID,
                    URL_Images_Default = (fileName)
                };
                db.Images_Default.Add(defaultImage);
            }

        }




        public ActionResult Edit(int id)
        {
            using (WatchStoreEntities9 db = new WatchStoreEntities9())
            {
                var product = db.Products.Find(id);
                if (product == null)
                {
                    return HttpNotFound();
                }

                // Populate the dropdowns
                ViewBag.Brands = db.Brands.ToList();
                ViewBag.Categorie = db.Categories.ToList();


                return View(product);
            }
        }


        [HttpPost]
        public ActionResult Edit(Product pro, HttpPostedFileBase ImageUrl)
        {
            using (WatchStoreEntities9 db = new WatchStoreEntities9())
            {
                // Find the existing product in the database
                var product = db.Products.Find(pro.ProductID);
                if (product == null)
                {
                    return HttpNotFound();
                }

                // Check if the SupplierId is valid
                var supplierExists = db.Suppliers.Any(s => s.SupplierID == pro.SupplierID);
                if (!supplierExists)
                {
                    ModelState.AddModelError("", "The selected supplier does not exist.");
                    return View(pro);
                }

                // Check if the BrandId is valid
                var brandExists = db.Brands.Any(b => b.BrandID == pro.BrandID);
                if (!brandExists)
                {
                    ModelState.AddModelError("", "The selected brand does not exist.");
                    return View(pro);
                }

                // Check if the CategoryId is valid
                var categoryExists = db.Categories.Any(c => c.CategoryID == pro.CategoryID);
                if (!categoryExists)
                {
                    ModelState.AddModelError("", "The selected category does not exist.");
                    return View(pro);
                }
                //// Check if the StockQuantity is valid
                //var StockQuantity = db.Products.Any(s => s.StockQuantity == pro.StockQuantity);
                //if (!StockQuantity)
                //{
                //    ModelState.AddModelError("", "The selected supplier does not exist.");
                //    return View(pro);
                //}

                // Handle the image file if it was uploaded
                if (ImageUrl != null && ImageUrl.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(ImageUrl.FileName);
                    string filePath = Path.Combine(Server.MapPath("~/Content/imgdata/"), fileName);
                    ImageUrl.SaveAs(filePath);
                    product.ImageUrl = fileName; // Save relative path in the database
                }

                // Update product details
                product.ProductName = pro.ProductName;
                product.Price = pro.Price;
                product.BrandID = pro.BrandID; // Update BrandID instead of Brand
                product.CategoryID = pro.CategoryID;
                product.SupplierID = pro.SupplierID;
                product.StockQuantity = pro.StockQuantity;

                db.SaveChanges();

                return RedirectToAction("Index");
            }
        }

        public ActionResult Delete(int id)
        {

            WatchStoreEntities9 db = new WatchStoreEntities9();
            Product product = db.Products.Where(row => row.ProductID == id).FirstOrDefault();

            return View(product);
        }
        [HttpPost]
        public ActionResult Delete(int id, Product pro)
        {
            using (WatchStoreEntities9 db = new WatchStoreEntities9())
            {
                // Find the product to delete
                Product product = db.Products.Where(p => p.ProductID == id).FirstOrDefault();

                if (product == null)
                {
                    return HttpNotFound();
                }

                // Find and delete related OrderItem
                //var orderItems = db.OrderItem.Where(oi => oi.ProductID == id).ToList();
                //foreach (var orderItem in orderItems)
                //{
                //    db.OrderItem.Remove(orderItem);
                //}

                //// Now delete the product
                //db.Products.Remove(product);
                product.Check_Remove = 0;
                db.SaveChanges();

                return RedirectToAction("Index");
            }
        }

    }
}