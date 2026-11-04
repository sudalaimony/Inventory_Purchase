using System.Data;
using InventoryPurchase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace InventoryPurchase.Controllers
{
    public class CategoryController : Controller
    {
        private readonly string connectionString;

        public CategoryController(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public IActionResult Index()
        {
            ViewBag.List = GetCategories();
            return View(new Category());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Category category)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        SqlCommand cmd = new SqlCommand("sp_Category_Insert", con);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@CategoryName", category.CategoryName);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }

                    TempData["Message"] = "Category added";
                    return RedirectToAction("Index");
                }
                catch (SqlException ex)
                {
                    ModelState.AddModelError("CategoryName", ex.Message);
                }
            }

            ViewBag.List = GetCategories();
            return View("Index", category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlCommand cmd = new SqlCommand("sp_Category_Delete", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@CategoryId", id);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
                TempData["Message"] = "Category deleted";
            }
            catch (SqlException ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction("Index");
        }

        private List<Category> GetCategories()
        {
            List<Category> list = new List<Category>();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_Category_GetAll", con);
                cmd.CommandType = CommandType.StoredProcedure;

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    Category c = new Category();
                    c.CategoryId = Convert.ToInt32(dr["CategoryId"]);
                    c.CategoryName = dr["CategoryName"].ToString()!;
                    list.Add(c);
                }
            }
            return list;
        }
    }
}
