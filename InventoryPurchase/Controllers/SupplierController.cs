using System.Data;
using InventoryPurchase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace InventoryPurchase.Controllers
{
    public class SupplierController : Controller
    {
        private readonly string connectionString;

        public SupplierController(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public IActionResult Index()
        {
            List<Supplier> list = new List<Supplier>();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_Supplier_GetAll", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@OnlyActive", false);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    list.Add(ReadSupplier(dr));
                }
            }
            return View(list);
        }

        public IActionResult Create()
        {
            return View("Form", new Supplier());
        }

        public IActionResult Edit(int id)
        {
            Supplier? supplier = null;

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand("sp_Supplier_GetById", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@SupplierId", id);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    supplier = ReadSupplier(dr);
                }
            }

            if (supplier == null)
                return NotFound();

            return View("Form", supplier);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Save(Supplier supplier)
        {
            if (!ModelState.IsValid)
                return View("Form", supplier);

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlCommand cmd = new SqlCommand("sp_Supplier_Save", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@SupplierId", supplier.SupplierId);
                    cmd.Parameters.AddWithValue("@SupplierName", supplier.SupplierName);
                    cmd.Parameters.AddWithValue("@Phone", (object?)supplier.Phone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Address", (object?)supplier.Address ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsActive", supplier.IsActive);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View("Form", supplier);
            }

            TempData["Message"] = "Supplier saved";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlCommand cmd = new SqlCommand("sp_Supplier_Delete", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@SupplierId", id);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
                TempData["Message"] = "Supplier deleted";
            }
            catch (SqlException ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction("Index");
        }

        private Supplier ReadSupplier(SqlDataReader dr)
        {
            Supplier s = new Supplier();
            s.SupplierId = Convert.ToInt32(dr["SupplierId"]);
            s.SupplierName = dr["SupplierName"].ToString()!;
            s.Phone = dr["Phone"].ToString();
            s.Address = dr["Address"].ToString();
            s.IsActive = Convert.ToBoolean(dr["IsActive"]);
            return s;
        }
    }
}
