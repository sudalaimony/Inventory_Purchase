using System.Data;
using InventoryPurchase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;

namespace InventoryPurchase.Controllers
{
    public class ProductController : Controller
    {
        private readonly string connectionString;

        public ProductController(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public IActionResult Index()
        {
            List<Product> list = new List<Product>();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_Product_GetAll", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@OnlyActive", false);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    Product p = new Product();
                    p.ProductId = Convert.ToInt32(dr["ProductId"]);
                    p.ProductCode = dr["ProductCode"].ToString()!;
                    p.ProductName = dr["ProductName"].ToString()!;
                    p.CategoryName = dr["CategoryName"].ToString();
                    p.TaxName = dr["TaxName"] == DBNull.Value ? null : dr["TaxName"].ToString();
                    p.CostPrice = Convert.ToDecimal(dr["CostPrice"]);
                    p.SellingPrice = Convert.ToDecimal(dr["SellingPrice"]);
                    p.HasExpiry = Convert.ToBoolean(dr["HasExpiry"]);
                    p.IsActive = Convert.ToBoolean(dr["IsActive"]);
                    list.Add(p);
                }
            }
            return View(list);
        }

        public IActionResult Create()
        {
            LoadDropdowns();
            return View("Form", new Product());
        }

        public IActionResult Edit(int id)
        {
            Product? product = null;

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_Product_GetById", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ProductId", id);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    product = new Product();
                    product.ProductId = Convert.ToInt32(dr["ProductId"]);
                    product.ProductCode = dr["ProductCode"].ToString()!;
                    product.ProductName = dr["ProductName"].ToString()!;
                    product.CategoryId = Convert.ToInt32(dr["CategoryId"]);
                    product.TaxId = dr["TaxId"] == DBNull.Value ? null : Convert.ToInt32(dr["TaxId"]);
                    product.CostPrice = Convert.ToDecimal(dr["CostPrice"]);
                    product.SellingPrice = Convert.ToDecimal(dr["SellingPrice"]);
                    product.HasExpiry = Convert.ToBoolean(dr["HasExpiry"]);
                    product.IsActive = Convert.ToBoolean(dr["IsActive"]);
                }
            }

            if (product == null)
                return NotFound();

            LoadDropdowns();
            return View("Form", product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Save(Product product)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        SqlCommand cmd = new SqlCommand("sp_Product_Save", con);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ProductId", product.ProductId);
                        cmd.Parameters.AddWithValue("@ProductCode", product.ProductCode);
                        cmd.Parameters.AddWithValue("@ProductName", product.ProductName);
                        cmd.Parameters.AddWithValue("@CategoryId", product.CategoryId);
                        cmd.Parameters.AddWithValue("@TaxId", (object?)product.TaxId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CostPrice", product.CostPrice);
                        cmd.Parameters.AddWithValue("@SellingPrice", product.SellingPrice);
                        cmd.Parameters.AddWithValue("@HasExpiry", product.HasExpiry);
                        cmd.Parameters.AddWithValue("@IsActive", product.IsActive);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }

                    TempData["Message"] = "Product saved";
                    return RedirectToAction("Index");
                }
                catch (SqlException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            LoadDropdowns();
            return View("Form", product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlCommand cmd = new SqlCommand("sp_Product_Delete", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ProductId", id);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
                TempData["Message"] = "Product deleted";
            }
            catch (SqlException ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction("Index");
        }

        // category and tax dropdowns
        private void LoadDropdowns()
        {
            List<Category> categories = new List<Category>();
            List<Tax> taxes = new List<Tax>();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                SqlCommand cmd = new SqlCommand("sp_Category_GetAll", con);
                cmd.CommandType = CommandType.StoredProcedure;
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Category c = new Category();
                        c.CategoryId = Convert.ToInt32(dr["CategoryId"]);
                        c.CategoryName = dr["CategoryName"].ToString()!;
                        categories.Add(c);
                    }
                }

                SqlCommand cmd2 = new SqlCommand("sp_Tax_GetAll", con);
                cmd2.CommandType = CommandType.StoredProcedure;
                cmd2.Parameters.AddWithValue("@OnlyActive", true);
                using (SqlDataReader dr = cmd2.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Tax t = new Tax();
                        t.TaxId = Convert.ToInt32(dr["TaxId"]);
                        t.TaxName = dr["TaxName"].ToString()!;
                        taxes.Add(t);
                    }
                }
            }

            ViewBag.Categories = new SelectList(categories, "CategoryId", "CategoryName");
            ViewBag.Taxes = new SelectList(taxes, "TaxId", "TaxName");
        }
    }
}
