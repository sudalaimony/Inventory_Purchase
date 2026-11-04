using System.Data;
using InventoryPurchase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace InventoryPurchase.Controllers
{
    public class TaxController : Controller
    {
        private readonly string connectionString;

        public TaxController(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public IActionResult Index()
        {
            List<Tax> list = new List<Tax>();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_Tax_GetAll", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@OnlyActive", false);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    list.Add(ReadTax(dr));
                }
            }
            return View(list);
        }

        public IActionResult Create()
        {
            return View("Form", new Tax());
        }

        public IActionResult Edit(int id)
        {
            Tax? tax = null;

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_Tax_GetById", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@TaxId", id);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    tax = ReadTax(dr);
                }
            }

            if (tax == null)
                return NotFound();

            return View("Form", tax);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Save(Tax tax)
        {
            if (!ModelState.IsValid)
                return View("Form", tax);

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlCommand cmd = new SqlCommand("sp_Tax_Save", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@TaxId", tax.TaxId);
                    cmd.Parameters.AddWithValue("@TaxName", tax.TaxName);
                    cmd.Parameters.AddWithValue("@TaxType", tax.TaxType);
                    cmd.Parameters.AddWithValue("@TaxRate", tax.TaxRate);
                    cmd.Parameters.AddWithValue("@AdditionalRate", tax.AdditionalRate);
                    cmd.Parameters.AddWithValue("@IsActive", tax.IsActive);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View("Form", tax);
            }

            TempData["Message"] = "Tax saved";
            return RedirectToAction("Index");
        }

        // procedure deletes the tax, or makes it inactive if it is already used
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            string result;

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_Tax_Delete", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@TaxId", id);

                con.Open();
                result = Convert.ToString(cmd.ExecuteScalar()) ?? "";
            }

            if (result == "Tax deleted")
                TempData["Message"] = result;
            else
                TempData["Error"] = result;

            return RedirectToAction("Index");
        }

        private Tax ReadTax(SqlDataReader dr)
        {
            Tax t = new Tax();
            t.TaxId = Convert.ToInt32(dr["TaxId"]);
            t.TaxName = dr["TaxName"].ToString()!;
            t.TaxType = dr["TaxType"].ToString()!;
            t.TaxRate = Convert.ToDecimal(dr["TaxRate"]);
            t.AdditionalRate = Convert.ToDecimal(dr["AdditionalRate"]);
            t.IsActive = Convert.ToBoolean(dr["IsActive"]);
            return t;
        }
    }
}
