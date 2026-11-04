using System.Data;
using InventoryPurchase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace InventoryPurchase.Controllers
{
    public class HomeController : Controller
    {
        private readonly string connectionString;

        public HomeController(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public IActionResult Index()
        {
            Dashboard d = new Dashboard();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_Dashboard", con);
                cmd.CommandType = CommandType.StoredProcedure;

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    d.Products = Convert.ToInt32(dr["Products"]);
                    d.Suppliers = Convert.ToInt32(dr["Suppliers"]);
                    d.Drafts = Convert.ToInt32(dr["Drafts"]);
                    d.Posted = Convert.ToInt32(dr["Posted"]);
                    d.ExpiredLots = Convert.ToInt32(dr["ExpiredLots"]);
                }
            }
            return View(d);
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
